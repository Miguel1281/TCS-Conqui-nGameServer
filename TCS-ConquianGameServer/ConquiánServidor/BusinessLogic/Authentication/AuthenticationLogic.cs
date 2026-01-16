using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.BusinessLogic.Validation;
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.Enums;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.ConquiánDB.Abstractions;
using ConquiánServidor.Utilities;
using ConquiánServidor.Utilities.Email;
using ConquiánServidor.Utilities.Email.Templates;
using NLog;
using System;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic.Authentication
{
    public class AuthenticationLogic : IAuthenticationLogic
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private const int VERIFICATION_CODE_EXPIRY_MINUTES = 10;
        private const int INITIAL_PLAYER_LEVEL = 1;
        private const int INITIAL_PLAYER_POINTS = 0;

        private readonly IPlayerRepository playerRepository;
        private readonly IEmailService emailService;
        private readonly IPresenceManager presenceManager;

        public AuthenticationLogic(IPlayerRepository playerRepository, IEmailService emailService, IPresenceManager presenceManager)
        {
            this.playerRepository = playerRepository;
            this.emailService = emailService;
            this.presenceManager = presenceManager;
        }

        public async Task<PlayerDto> AuthenticatePlayerAsync(string playerEmail, string playerPassword)
        {
            Logger.Info("Authentication attempt started.");

            var playerFromDb = await playerRepository.GetPlayerByEmailAsync(playerEmail);

            if (playerFromDb == null || !PasswordHasher.verifyPassword(playerPassword, playerFromDb.password))
            {
                throw new BusinessLogicException(ServiceErrorType.InvalidPassword);
            }

            if (this.presenceManager.IsPlayerOnline(playerFromDb.idPlayer))
            {
                Logger.Warn($"Authentication failed: Player ID {playerFromDb.idPlayer} is already online.");
                throw new BusinessLogicException(ServiceErrorType.SessionActive);
            }

            await this.presenceManager.NotifyStatusChange(playerFromDb.idPlayer, (int)PlayerStatus.Online);

            Logger.Info($"Authentication successful for Player ID: {playerFromDb.idPlayer}");

            return new PlayerDto
            {
                idPlayer = playerFromDb.idPlayer,
                nickname = playerFromDb.nickname,
                pathPhoto = playerFromDb.pathPhoto,
                Status = PlayerStatus.Online
            };
        }

        public async Task SignOutPlayerAsync(int idPlayer)
        {
            this.presenceManager.DisconnectUser(idPlayer);
            await Task.CompletedTask;
            Logger.Info($"Sign out notification sent for Player ID: {idPlayer}");
        }

        public async Task RegisterPlayerAsync(PlayerDto finalPlayerData)
        {
            if (!string.IsNullOrEmpty(SignUpServerValidator.ValidateName(finalPlayerData.name)) ||
                !string.IsNullOrEmpty(SignUpServerValidator.ValidateLastName(finalPlayerData.lastName)) ||
                !string.IsNullOrEmpty(SignUpServerValidator.ValidateNickname(finalPlayerData.nickname)))
            {
                throw new BusinessLogicException(ServiceErrorType.InvalidNameFormat);
            }

            if (!string.IsNullOrEmpty(SignUpServerValidator.ValidatePassword(finalPlayerData.password)))
            {
                throw new BusinessLogicException(ServiceErrorType.InvalidPasswordFormat);
            }

            bool nicknameExists = await playerRepository.DoesNicknameExistAsync(finalPlayerData.nickname);
            if (nicknameExists)
            {
                throw new BusinessLogicException(ServiceErrorType.DuplicateRecord);
            }

            var playerToUpdate = await playerRepository.GetPlayerByEmailAsync(finalPlayerData.email);
            if (playerToUpdate == null)
            {
                throw new BusinessLogicException(ServiceErrorType.UserNotFound);
            }

            playerToUpdate.password = PasswordHasher.hashPassword(finalPlayerData.password);
            playerToUpdate.nickname = finalPlayerData.nickname;
            playerToUpdate.name = finalPlayerData.name;
            playerToUpdate.lastName = finalPlayerData.lastName;
            playerToUpdate.pathPhoto = finalPlayerData.pathPhoto;
            playerToUpdate.verificationCode = null;
            playerToUpdate.codeExpiryDate = null;
            playerToUpdate.idLevel = INITIAL_PLAYER_LEVEL;
            playerToUpdate.currentPoints = INITIAL_PLAYER_POINTS;

            await playerRepository.SaveChangesAsync();

            Logger.Info($"Registration successful for Player ID: {playerToUpdate.idPlayer}");
        }

        public async Task<string> GenerateAndStoreRecoveryTokenAsync(string email)
        {
            var player = await playerRepository.GetPlayerByEmailAsync(email);

            if (player == null || string.IsNullOrEmpty(player.password))
            {
                throw new BusinessLogicException(ServiceErrorType.UserNotFound);
            }

            string recoveryCode = emailService.GenerateVerificationCode();
            player.verificationCode = recoveryCode;
            player.codeExpiryDate = DateTime.UtcNow.AddMinutes(VERIFICATION_CODE_EXPIRY_MINUTES);
            await playerRepository.SaveChangesAsync();

            Logger.Info($"Recovery token generated for Player ID: {player.idPlayer}");
            return recoveryCode;
        }

        public async Task<string> SendVerificationCodeAsync(string email)
        {
            string emailError = SignUpServerValidator.ValidateEmail(email);
            if (!string.IsNullOrEmpty(emailError))
            {
                throw new BusinessLogicException(ServiceErrorType.InvalidEmailFormat);
            }

            var existingPlayer = await playerRepository.GetPlayerForVerificationAsync(email);
            if (existingPlayer != null)
            {
                throw new BusinessLogicException(ServiceErrorType.RegisteredMail);
            }

            string verificationCode = emailService.GenerateVerificationCode();
            var playerToVerify = await playerRepository.GetPlayerByEmailAsync(email);

            if (playerToVerify == null)
            {
                playerToVerify = new Player();
                playerToVerify.idLevel = INITIAL_PLAYER_LEVEL;
                playerRepository.AddPlayer(playerToVerify);
            }

            playerToVerify.email = email;
            playerToVerify.verificationCode = verificationCode;
            playerToVerify.codeExpiryDate = DateTime.UtcNow.AddMinutes(VERIFICATION_CODE_EXPIRY_MINUTES);
            await playerRepository.SaveChangesAsync();

            var emailTemplate = new VerificationEmailTemplate(verificationCode);
            await emailService.SendEmailAsync(email, emailTemplate);

            Logger.Info($"Verification code sent. Player ID (if available): {playerToVerify.idPlayer}");
            return verificationCode;
        }

        public async Task VerifyCodeAsync(string email, string code)
        {
            var player = await playerRepository.GetPlayerByEmailAsync(email);

            if (player == null)
            {
                throw new BusinessLogicException(ServiceErrorType.UserNotFound);
            }

            if (player.codeExpiryDate.HasValue && DateTime.UtcNow > player.codeExpiryDate.Value)
            {
                Logger.Warn($"Verification code expired for Player ID {player.idPlayer}");
                throw new BusinessLogicException(ServiceErrorType.VerificationCodeExpired);
            }

            if (player.verificationCode != code)
            {
                throw new BusinessLogicException(ServiceErrorType.InvalidVerificationCode);
            }

            Logger.Info($"Verification code verified for Player ID: {player.idPlayer}");
        }

        public async Task HandlePasswordRecoveryRequestAsync(string email)
        {
            string recoveryCode = await GenerateAndStoreRecoveryTokenAsync(email);

            var emailTemplate = new RecoveryEmailTemplate(recoveryCode);
            await emailService.SendEmailAsync(email, emailTemplate);

        }

        public async Task HandleTokenValidationAsync(string email, string token)
        {
            await VerifyCodeAsync(email, token);
        }

        public async Task HandlePasswordResetAsync(string email, string token, string newPassword)
        {
            string passwordError = SignUpServerValidator.ValidatePassword(newPassword);
            if (!string.IsNullOrEmpty(passwordError))
            {
                throw new BusinessLogicException(ServiceErrorType.InvalidPasswordFormat);
            }

            await HandleTokenValidationAsync(email, token);

            var player = await playerRepository.GetPlayerByEmailAsync(email);
            if (player == null)
            {
                throw new BusinessLogicException(ServiceErrorType.UserNotFound);
            }

            player.password = PasswordHasher.hashPassword(newPassword);
            player.verificationCode = null;
            player.codeExpiryDate = null;

            await playerRepository.SaveChangesAsync();

            Logger.Info($"Password reset successful for Player ID: {player.idPlayer}");
        }

        public async Task DeleteTemporaryPlayerAsync(string email)
        {
            var player = await playerRepository.GetPlayerByEmailAsync(email);

            if (player != null && string.IsNullOrEmpty(player.name))
            {
                await playerRepository.DeletePlayerAsync(player);
                Logger.Info($"Temporary player deleted. Player ID: {player.idPlayer}");
            }
            else
            {
                Logger.Info("Temporary player deletion: User not found or invalid.");
            }
        }
    }
}
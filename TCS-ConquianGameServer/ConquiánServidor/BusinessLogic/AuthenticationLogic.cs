using ConquiánServidor.BusinessLogic.Validation;
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.Utilities;
using ConquiánServidor.Utilities.Email;
using ConquiánServidor.Utilities.Email.Templates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using ConquiánServidor.Properties.Langs;

namespace ConquiánServidor.BusinessLogic
{
    public class AuthenticationLogic
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IPlayerRepository playerRepository;
        private readonly IEmailService emailService;
        public AuthenticationLogic(IPlayerRepository playerRepository, IEmailService emailService)
        {
            this.playerRepository = playerRepository;
            this.emailService = emailService;
        }

        public async Task<PlayerDto> AuthenticatePlayerAsync(string playerEmail, string playerPassword)
        {
            Logger.Info("Authentication attempt started.");

            var playerFromDb = await playerRepository.GetPlayerByEmailAsync(playerEmail);

            if (playerFromDb == null || !PasswordHasher.verifyPassword(playerPassword, playerFromDb.password))
            {
                Logger.Warn("Authentication failed: Invalid credentials.");
                throw new UnauthorizedAccessException(Lang.ErrorInvalidCredentials);
            }

            playerFromDb.IdStatus = 1;
            await playerRepository.SaveChangesAsync();
            await PresenceManager.Instance.NotifyStatusChange(playerFromDb.idPlayer, 1);

            Logger.Info($"Authentication successful for Player ID: {playerFromDb.idPlayer}");

            return new PlayerDto
            {
                idPlayer = playerFromDb.idPlayer,
                nickname = playerFromDb.nickname,
                pathPhoto = playerFromDb.pathPhoto,
            };
        }

        public async Task SignOutPlayerAsync(int idPlayer)
        {
            Logger.Info($"Sign out attempt for Player ID: {idPlayer}");
            var playerFromDb = await playerRepository.GetPlayerByIdAsync(idPlayer);
            if (playerFromDb != null)
            {
                playerFromDb.IdStatus = 2;
                await playerRepository.SaveChangesAsync();
                await PresenceManager.Instance.NotifyStatusChange(idPlayer, 2);
                Logger.Info($"Sign out successful for Player ID: {idPlayer}");
            }
        }

        public async Task RegisterPlayerAsync(PlayerDto finalPlayerData)
        {
            List<string> validationErrors = new List<string>
            {
                SignUpServerValidator.ValidateName(finalPlayerData.name),
                SignUpServerValidator.ValidateLastName(finalPlayerData.lastName),
                SignUpServerValidator.ValidateNickname(finalPlayerData.nickname),
                SignUpServerValidator.ValidatePassword(finalPlayerData.password)
            };

            var errors = validationErrors.Where(e => !string.IsNullOrEmpty(e)).ToList();

            if (errors.Any())
            {
                string errorMsg = string.Join("; ", errors);
                Logger.Warn($"Registration validation failed. Errors: {errorMsg}");
                throw new ArgumentException(errorMsg);
            }

            bool nicknameExists = await playerRepository.DoesNicknameExistAsync(finalPlayerData.nickname);
            if (nicknameExists)
            {
                Logger.Warn("Registration failed: Nickname already exists.");
                throw new InvalidOperationException(Lang.ErrorNicknameExists);
            }

            var playerToUpdate = await playerRepository.GetPlayerByEmailAsync(finalPlayerData.email);
            if (playerToUpdate == null)
            {
                throw new InvalidOperationException("El flujo de registro es incorrecto. Usuario no encontrado.");
            }

            playerToUpdate.password = PasswordHasher.hashPassword(finalPlayerData.password);
            playerToUpdate.nickname = finalPlayerData.nickname;
            playerToUpdate.name = finalPlayerData.name;
            playerToUpdate.lastName = finalPlayerData.lastName;
            playerToUpdate.pathPhoto = finalPlayerData.pathPhoto;
            playerToUpdate.verificationCode = null;
            playerToUpdate.codeExpiryDate = null;
            playerToUpdate.level = "1";
            playerToUpdate.currentPoints = "0";

            await playerRepository.SaveChangesAsync();

            Logger.Info($"Registration successful for Player ID: {playerToUpdate.idPlayer}");
        }

        public async Task<string> GenerateAndStoreRecoveryTokenAsync(string email)
        {
            var player = await playerRepository.GetPlayerByEmailAsync(email);

            if (player == null || string.IsNullOrEmpty(player.password))
            {
                Logger.Warn("Recovery token requested for non-existent user.");
                throw new KeyNotFoundException(Lang.ErrorUserNotFound);
            }

            string recoveryCode = emailService.GenerateVerificationCode();
            player.verificationCode = recoveryCode;
            player.codeExpiryDate = DateTime.UtcNow.AddMinutes(10);

            await playerRepository.SaveChangesAsync();

            Logger.Info($"Recovery token generated for Player ID: {player.idPlayer}");
            return recoveryCode;
        }

     public async Task<string> SendVerificationCodeAsync(string email)
        {
            string emailError = SignUpServerValidator.ValidateEmail(email);
            if (!string.IsNullOrEmpty(emailError))
            {
                Logger.Warn("Verification code request with invalid email format.");
                throw new ArgumentException(emailError);
            }

            var existingPlayer = await playerRepository.GetPlayerForVerificationAsync(email);
            if (existingPlayer != null)
            {
                Logger.Warn($"Verification attempted for existing account. Player ID: {existingPlayer.idPlayer}");
                throw new InvalidOperationException("ERROR_EMAIL_EXISTS");
            }

            string verificationCode = emailService.GenerateVerificationCode();
            var playerToVerify = await playerRepository.GetPlayerByEmailAsync(email);

            if (playerToVerify == null)
            {
                playerToVerify = new Player();
                playerRepository.AddPlayer(playerToVerify);
            }

            playerToVerify.email = email;
            playerToVerify.verificationCode = verificationCode;
            playerToVerify.codeExpiryDate = DateTime.UtcNow.AddMinutes(10);

            await playerRepository.SaveChangesAsync();

            var emailTemplate = new VerificationEmailTemplate(verificationCode);
            await emailService.SendEmailAsync(email, emailTemplate);

            Logger.Info($"Verification code sent. Player ID (if available): {playerToVerify.idPlayer}");
            return verificationCode;
        }

        public async Task VerifyCodeAsync(string email, string code)
        {
            var player = await playerRepository.GetPlayerByEmailAsync(email);

            bool isValid = player != null &&
                           player.verificationCode == code &&
                           DateTime.UtcNow < player.codeExpiryDate;

            if (!isValid)
            {
                Logger.Warn("Verification code check failed.");
                throw new ArgumentException(Lang.ErrorVerificationCodeIncorrect);
            }

            Logger.Info($"Verification code verified for Player ID: {player.idPlayer}");
        }

        public async Task HandlePasswordRecoveryRequestAsync(string email)
        {
            string recoveryCode = await GenerateAndStoreRecoveryTokenAsync(email);

            var emailTemplate = new RecoveryEmailTemplate(recoveryCode);
            await emailService.SendEmailAsync(email, emailTemplate);

            Logger.Info("Recovery email sent.");
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
                Logger.Warn("Password reset failed: Invalid password format.");
                throw new ArgumentException(passwordError);
            }

            await HandleTokenValidationAsync(email, token);

            var player = await playerRepository.GetPlayerByEmailAsync(email);
            if (player == null)
            {
                throw new KeyNotFoundException(Lang.ErrorUserNotFound);
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
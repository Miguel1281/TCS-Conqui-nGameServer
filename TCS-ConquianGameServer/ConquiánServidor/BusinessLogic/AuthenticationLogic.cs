using ConquiánServidor.BusinessLogic.Validation;
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.Utilities;
using ConquiánServidor.Utilities.Email;
using ConquiánServidor.Utilities.Email.Templates;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Mail;
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
            Logger.Info(string.Format(Lang.LogAuthLogicAuthAttempt, playerEmail));

            var playerFromDb = await playerRepository.GetPlayerByEmailAsync(playerEmail);

            if (playerFromDb == null || !PasswordHasher.verifyPassword(playerPassword, playerFromDb.password))
            {
                Logger.Warn(string.Format(Lang.LogAuthLogicAuthFailed, playerEmail));
                throw new UnauthorizedAccessException(Lang.ErrorInvalidCredentials);
            }

            playerFromDb.IdStatus = 1;
            await playerRepository.SaveChangesAsync();
            await PresenceManager.Instance.NotifyStatusChange(playerFromDb.idPlayer, 1);

            Logger.Info(string.Format(Lang.LogAuthLogicAuthSuccess, playerEmail, playerFromDb.idPlayer));

            return new PlayerDto
            {
                idPlayer = playerFromDb.idPlayer,
                nickname = playerFromDb.nickname,
                pathPhoto = playerFromDb.pathPhoto,
            };
        }

        public async Task SignOutPlayerAsync(int idPlayer)
        {
            Logger.Info(string.Format(Lang.LogAuthLogicSignOutAttempt, idPlayer)); 
            var playerFromDb = await playerRepository.GetPlayerByIdAsync(idPlayer);
            if (playerFromDb != null)
            {
                playerFromDb.IdStatus = 2;
                await playerRepository.SaveChangesAsync();
                await PresenceManager.Instance.NotifyStatusChange(idPlayer, 2);
                Logger.Info(string.Format(Lang.LogAuthLogicSignOutSuccess, idPlayer)); 
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
                Logger.Warn(string.Format(Lang.LogAuthLogicRegisterValidationFailed, finalPlayerData.nickname, errorMsg));
                throw new ArgumentException(errorMsg);
            }

            bool nicknameExists = await playerRepository.DoesNicknameExistAsync(finalPlayerData.nickname);
            if (nicknameExists)
            {
                Logger.Warn(string.Format(Lang.LogAuthLogicRegisterNicknameExists, finalPlayerData.email, finalPlayerData.nickname));
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

            await playerRepository.SaveChangesAsync();

            Logger.Info(string.Format(Lang.LogAuthLogicRegisterSuccess, finalPlayerData.nickname, finalPlayerData.email));
        }

        public async Task<string> GenerateAndStoreRecoveryTokenAsync(string email)
        {
            var player = await playerRepository.GetPlayerByEmailAsync(email);

            if (player == null || string.IsNullOrEmpty(player.password))
            {
                Logger.Warn(string.Format(Lang.LogAuthLogicGenTokenNotFound, email));
                throw new KeyNotFoundException(Lang.ErrorUserNotFound);
            }

            string recoveryCode = emailService.GenerateVerificationCode();
            player.verificationCode = recoveryCode;
            player.codeExpiryDate = DateTime.UtcNow.AddMinutes(10);

            await playerRepository.SaveChangesAsync();

            Logger.Info(string.Format(Lang.LogAuthLogicGenTokenSuccess, email));
            return recoveryCode;
        }

        public async Task<string> SendVerificationCodeAsync(string email)
        {
            string emailError = SignUpServerValidator.ValidateEmail(email);
            if (!string.IsNullOrEmpty(emailError))
            {
                Logger.Warn(string.Format(Lang.LogAuthLogicSendCodeInvalidEmail, email));
                throw new ArgumentException(emailError);
            }

            var existingPlayer = await playerRepository.GetPlayerForVerificationAsync(email);
            if (existingPlayer != null)
            {
                Logger.Warn(string.Format(Lang.LogAuthLogicSendCodeEmailExists, email));
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

            Logger.Info(string.Format(Lang.LogAuthLogicSendCodeSuccess, email));
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
                Logger.Warn(string.Format(Lang.LogAuthLogicVerifyCodeFailed, email));
                throw new ArgumentException(Lang.ErrorVerificationCodeIncorrect);
            }

            Logger.Info(string.Format(Lang.LogAuthLogicVerifyCodeSuccess, email));
        }

        public async Task HandlePasswordRecoveryRequestAsync(string email)
        {
            string recoveryCode = await GenerateAndStoreRecoveryTokenAsync(email);

            var emailTemplate = new RecoveryEmailTemplate(recoveryCode);
            await emailService.SendEmailAsync(email, emailTemplate);

            Logger.Info(string.Format(Lang.LogAuthLogicPassRecoverySuccess, email));
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
                Logger.Warn(string.Format(Lang.LogAuthLogicPassResetInvalidPassword, email));
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

            Logger.Info(string.Format(Lang.LogAuthLogicPassResetSuccess, email));
        }

        public async Task DeleteTemporaryPlayerAsync(string email)
        {
            var player = await playerRepository.GetPlayerByEmailAsync(email);

            if (player != null && string.IsNullOrEmpty(player.name))
            {
                await playerRepository.DeletePlayerAsync(player);
                Logger.Info(string.Format(Lang.LogAuthLogicDeleteTempSuccess, email));
            }
            else
            {
                Logger.Info(string.Format(Lang.LogAuthLogicDeleteTempNotFound, email));
            }
        }
    }
}
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
            PlayerDto playerDto = new PlayerDto();
            var playerFromDb = await playerRepository.GetPlayerByEmailAsync(playerEmail);

            if (playerFromDb != null && PasswordHasher.verifyPassword(playerPassword, playerFromDb.password))
            {
                playerFromDb.IdStatus = 1;
                await playerRepository.SaveChangesAsync();
                await PresenceManager.Instance.NotifyStatusChange(playerFromDb.idPlayer, 1);
                playerDto = new PlayerDto
                {
                    idPlayer = playerFromDb.idPlayer,
                    nickname = playerFromDb.nickname,
                    pathPhoto = playerFromDb.pathPhoto,
                };
                Logger.Info(string.Format(Lang.LogAuthLogicAuthSuccess, playerEmail, playerFromDb.idPlayer)); 
            }
            else
            {
                Logger.Warn(string.Format(Lang.LogAuthLogicAuthFailed, playerEmail)); 
            }
            return playerDto;
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

        public async Task<bool> RegisterPlayerAsync(PlayerDto finalPlayerData)
        {
            bool success = false;
            List<string> validationErrors = new List<string>();
            validationErrors.Add(SignUpServerValidator.ValidateName(finalPlayerData.name));
            validationErrors.Add(SignUpServerValidator.ValidateLastName(finalPlayerData.lastName));
            validationErrors.Add(SignUpServerValidator.ValidateNickname(finalPlayerData.nickname));
            validationErrors.Add(SignUpServerValidator.ValidatePassword(finalPlayerData.password));

            bool hasErrors = validationErrors.Any(error => !string.IsNullOrEmpty(error));

            if (hasErrors)
            {
                string errors = string.Join("; ", validationErrors.Where(e => !string.IsNullOrEmpty(e)));
                Logger.Warn(string.Format(Lang.LogAuthLogicRegisterValidationFailed, finalPlayerData.nickname, errors)); 
                return success;
            }

            try
            {
                var nicknameExists = await playerRepository.DoesNicknameExistAsync(finalPlayerData.nickname);
                if (!nicknameExists)
                {
                    var playerToUpdate = await playerRepository.GetPlayerByEmailAsync(finalPlayerData.email);

                    if (playerToUpdate != null)
                    {
                        playerToUpdate.password = PasswordHasher.hashPassword(finalPlayerData.password);
                        playerToUpdate.nickname = finalPlayerData.nickname;
                        playerToUpdate.name = finalPlayerData.name;
                        playerToUpdate.lastName = finalPlayerData.lastName;
                        playerToUpdate.pathPhoto = finalPlayerData.pathPhoto;
                        playerToUpdate.verificationCode = null;
                        playerToUpdate.codeExpiryDate = null;

                        await playerRepository.SaveChangesAsync();
                        success = true;
                        Logger.Info(string.Format(Lang.LogAuthLogicRegisterSuccess, finalPlayerData.nickname, finalPlayerData.email)); 
                    }
                }
                else
                {
                    Logger.Warn(string.Format(Lang.LogAuthLogicRegisterNicknameExists, finalPlayerData.email, finalPlayerData.nickname)); 
                }
            }
            catch (DbUpdateException ex)
            {
                Logger.Error(ex, string.Format(Lang.GlobalDbUpdateError, ex.InnerException?.Message));
                success = false;
            }
            catch (DbEntityValidationException ex)
            {
                var errorMessages = ex.EntityValidationErrors.SelectMany(e => e.ValidationErrors).Select(v => v.ErrorMessage);
                Logger.Error(ex, string.Format(Lang.GlobalDbEntityValidationError, string.Join("; ", errorMessages)));
                success = false;
            }
            catch (SqlException ex)
            {
                Logger.Error(ex, string.Format(Lang.GlobalSqlError, ex.Message));
                success = false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, string.Format(Lang.LogAuthLogicRegisterPlayerUnexpectedError, ex.Message));
                success = false;
            }
            return success;
        }
        public async Task<string> GenerateAndStoreRecoveryTokenAsync(string email)
        {
            try
            {
                var player = await playerRepository.GetPlayerByEmailAsync(email);

                if (player != null && !string.IsNullOrEmpty(player.password))
                {
                    string recoveryCode = emailService.GenerateVerificationCode();
          
                    player.verificationCode = recoveryCode;
                    player.codeExpiryDate = DateTime.UtcNow.AddMinutes(10);

                    await playerRepository.SaveChangesAsync();

                    Logger.Info(string.Format(Lang.LogAuthLogicGenTokenSuccess, email)); 
                    return recoveryCode;
                }
                else
                {
                    Logger.Warn(string.Format(Lang.LogAuthLogicGenTokenNotFound, email)); 
                }
            }
            catch (DbUpdateException ex)
            {
                Logger.Error(ex, string.Format(Lang.LogAuthLogicGenTokenDbUpdateError, ex.InnerException?.Message));
            }
            catch (SqlException ex)
            {
                Logger.Error(ex, string.Format(Lang.LogAuthLogicGenTokenSqlError, ex.Message));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, string.Format(Lang.LogAuthLogicGenTokenUnexpectedError, ex.Message));
            }
            return null;
        }
        public async Task<string> SendVerificationCodeAsync(string email)
        {
            string result = string.Empty;

            string emailError = SignUpServerValidator.ValidateEmail(email);
            if (!string.IsNullOrEmpty(emailError))
            {
                Logger.Warn(string.Format(Lang.LogAuthLogicSendCodeInvalidEmail, email)); 
                return result;
            }

            var existingPlayer = await playerRepository.GetPlayerForVerificationAsync(email);
            if (existingPlayer != null)
            {
                Logger.Warn(string.Format(Lang.LogAuthLogicSendCodeEmailExists, email)); 
                result = "ERROR_EMAIL_EXISTS";
            }
            else
            {
                try
                {
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
                    result = verificationCode;
                }
                catch (DbUpdateException ex)
                {
                    Logger.Error(ex, string.Format(Lang.LogAuthLogicSendCodeDbUpdateError, ex.InnerException?.Message));
                }
                catch (DbEntityValidationException ex)
                {
                    var errorMessages = ex.EntityValidationErrors.SelectMany(e => e.ValidationErrors).Select(v => v.ErrorMessage);
                    Logger.Error(ex, string.Format(Lang.LogAuthLogicSendCodeEfValidationError, string.Join("; ", errorMessages)));
                }
                catch (SqlException ex)
                {
                    Logger.Error(ex, string.Format(Lang.LogAuthLogicSendCodeSqlError, ex.Message));
                }
                catch (SmtpException ex)
                {
                    Logger.Error(ex, string.Format(Lang.GlobalSmtpError, ex.Message));
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, string.Format(Lang.LogAuthLogicSendCodeUnexpectedError, ex.Message));
                }
            }
            return result;
        }

        public async Task<bool> VerifyCodeAsync(string email, string code)
        {
            bool isValid = false;
            try
            {
                var player = await playerRepository.GetPlayerByEmailAsync(email);
                if (player != null && player.verificationCode == code && DateTime.UtcNow < player.codeExpiryDate)
                {
                    isValid = true;
                    Logger.Info(string.Format(Lang.LogAuthLogicVerifyCodeSuccess, email)); 
                }
                else
                {
                    Logger.Warn(string.Format(Lang.LogAuthLogicVerifyCodeFailed, email)); 
                }
            }
            catch (SqlException ex)
            {
                Logger.Error(ex, string.Format(Lang.LogAuthLogicVerifyCodeSqlError, ex.Message));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, string.Format(Lang.LogAuthLogicVerifyCodeUnexpectedError, ex.Message));
            }
            return isValid;
        }

        public async Task<bool> HandlePasswordRecoveryRequestAsync(string email)
        {
            bool success = false;
            try
            {
                string recoveryCode = await GenerateAndStoreRecoveryTokenAsync(email);

                if (!string.IsNullOrEmpty(recoveryCode))
                {
                    var emailTemplate = new RecoveryEmailTemplate(recoveryCode);
                    await emailService.SendEmailAsync(email, emailTemplate);
                    success = true;
                    Logger.Info(string.Format(Lang.LogAuthLogicPassRecoverySuccess, email)); 
                }
                else
                {
                    Logger.Warn(string.Format(Lang.LogAuthLogicPassRecoveryTokenFailed, email)); 
                }
            }
            catch (SmtpException ex)
            {
                Logger.Error(ex, string.Format(Lang.LogAuthLogicPassRecoverySmtpError, ex.Message));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, string.Format(Lang.LogAuthLogicPassRecoveryUnexpectedError, ex.Message));
            }
            return success;
        }

        public async Task<bool> HandleTokenValidationAsync(string email, string token)
        {
            return await VerifyCodeAsync(email, token);
        }

        public async Task<bool> HandlePasswordResetAsync(string email, string token, string newPassword)
        {
            bool success = false;
            try
            {
                string passwordError = SignUpServerValidator.ValidatePassword(newPassword);
                if (!string.IsNullOrEmpty(passwordError))
                {
                    Logger.Warn(string.Format(Lang.LogAuthLogicPassResetInvalidPassword, email)); 
                    return success;
                }

                if (await HandleTokenValidationAsync(email, token))
                {
                    var player = await playerRepository.GetPlayerByEmailAsync(email);
                    if (player != null)
                    {
                        player.password = PasswordHasher.hashPassword(newPassword);
                        player.verificationCode = null;
                        player.codeExpiryDate = null;

                        await playerRepository.SaveChangesAsync();
                        success = true;
                        Logger.Info(string.Format(Lang.LogAuthLogicPassResetSuccess, email)); 
                    }
                    else
                    {
                        Logger.Warn(string.Format(Lang.LogAuthLogicPassResetPlayerNotFound, email)); 
                    }
                }
                else
                {
                    Logger.Warn(string.Format(Lang.LogAuthLogicPassResetTokenFailed, email)); 
                }
            }
            catch (DbUpdateException ex)
            {
                Logger.Error(ex, string.Format(Lang.LogAuthLogicPassResetDbUpdateError, ex.InnerException?.Message));
            }
            catch (SqlException ex)
            {
                Logger.Error(ex, string.Format(Lang.LogAuthLogicPassResetSqlError, ex.Message));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, string.Format(Lang.LogAuthLogicPassResetUnexpectedError, ex.Message));
            }
            return success;
        }

        public async Task<bool> DeleteTemporaryPlayerAsync(string email)
        {
            bool success = false;
            try
            {
                var player = await playerRepository.GetPlayerByEmailAsync(email);

                if (player != null && string.IsNullOrEmpty(player.name))
                {
                    success = await playerRepository.DeletePlayerAsync(player);
                    Logger.Info(string.Format(Lang.LogAuthLogicDeleteTempSuccess, email)); 
                }
                else
                {
                    Logger.Info(string.Format(Lang.LogAuthLogicDeleteTempNotFound, email)); 
                }
            }
            catch (DbUpdateException ex)
            {
                Logger.Error(ex, string.Format(Lang.LogAuthLogicDeleteTempDbUpdateError, ex.InnerException?.Message));
            }
            catch (SqlException ex)
            {
                Logger.Error(ex, string.Format(Lang.LogAuthLogicDeleteTempSqlError, ex.Message));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, string.Format(Lang.LogAuthLogicDeleteTempUnexpectedError, ex.Message));
            }

            return success;
        }
    }
}
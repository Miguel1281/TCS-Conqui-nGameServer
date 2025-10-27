using ConquiánServidor.BusinessLogic.Validation;
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.Utilities;
using ConquiánServidor.Utilities.Email;
using ConquiánServidor.Utilities.Email.Templates;
using ConquiánServidor.BusinessLogic;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic
{
    public class AuthenticationLogic
    {
        private readonly IPlayerRepository playerRepository;
        private readonly IEmailService emailService;
        public AuthenticationLogic(IPlayerRepository playerRepository, IEmailService emailService)
        {
            this.playerRepository = playerRepository;
            this.emailService = emailService;
        }

        public async Task<PlayerDto> AuthenticatePlayerAsync(string playerEmail, string playerPassword)
        {
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
            }
            return playerDto; 
        }

        public async Task SignOutPlayerAsync(int idPlayer)
        {
            var playerFromDb = await playerRepository.GetPlayerByIdAsync(idPlayer);
            if (playerFromDb != null)
            {
                playerFromDb.IdStatus = 2;
                await playerRepository.SaveChangesAsync();
                await PresenceManager.Instance.NotifyStatusChange(idPlayer, 2);
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
            bool hasErrors = false;

            foreach (string error in validationErrors)
            {
                if (!string.IsNullOrEmpty(error))
                {
                    hasErrors = true;
                    break;
                }
            }

            if (hasErrors)
            {
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
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine("Error de SQL: " + ex.Message);
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
                    player.codeExpiryDate = DateTime.UtcNow.AddMinutes(1); 

                    await playerRepository.SaveChangesAsync();

                    return recoveryCode; 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error en GenerateAndStoreRecoveryTokenAsync: " + ex.Message);
            }
            return null; 
        }
        public async Task<string> SendVerificationCodeAsync(string email)
        {
            string result = string.Empty;

            string emailError = SignUpServerValidator.ValidateEmail(email);
            if (!string.IsNullOrEmpty(emailError))
            {
                return result; 
            }

            var existingPlayer = await playerRepository.GetPlayerForVerificationAsync(email);
            if (existingPlayer != null)
            {
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
                    playerToVerify.codeExpiryDate = DateTime.UtcNow.AddMinutes(1);

                    await playerRepository.SaveChangesAsync();

                    var emailTemplate = new VerificationEmailTemplate(verificationCode);
                    await emailService.SendEmailAsync(email, emailTemplate);

                    result = verificationCode;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al enviar correo: " + ex.Message);
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
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al verificar el código: " + ex.Message);
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
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error en solicitud de recuperación: " + ex.Message);
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
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al reiniciar contraseña: " + ex.Message);
            }
            return success;
        }
    }
}
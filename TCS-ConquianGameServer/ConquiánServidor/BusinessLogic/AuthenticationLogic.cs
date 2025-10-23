using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.Utilities;
using ConquiánServidor.Utilities.Email;
using ConquiánServidor.Utilities.Email.Templates;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic
{
    public class AuthenticationLogic
    {
        private readonly IPlayerRepository playerRepository;
        private readonly EmailService emailService = new EmailService();

        public AuthenticationLogic(IPlayerRepository playerRepository)
        {
            this.playerRepository = playerRepository;
        }

        public async Task<PlayerDto> AuthenticatePlayerAsync(string playerEmail, string playerPassword)
        {
            var playerFromDb = await playerRepository.GetPlayerByEmailAsync(playerEmail);

            if (playerFromDb != null && PasswordHasher.verifyPassword(playerPassword, playerFromDb.password))
            {
                playerFromDb.IdStatus = 1;
                await playerRepository.SaveChangesAsync();
                return new PlayerDto
                {
                    idPlayer = playerFromDb.idPlayer,
                    nickname = playerFromDb.nickname,
                    pathPhoto = playerFromDb.pathPhoto,
                };
            }
            return null;
        }

        public async Task SignOutPlayerAsync(int idPlayer)
        {
            var playerFromDb = await playerRepository.GetPlayerByIdAsync(idPlayer);
            if (playerFromDb != null)
            {
                playerFromDb.IdStatus = 2;
                await playerRepository.SaveChangesAsync();
            }
        }

        public async Task<bool> RegisterPlayerAsync(PlayerDto finalPlayerData)
        {
            try
            {
                var nicknameExists = await playerRepository.DoesNicknameExistAsync(finalPlayerData.nickname);
                if (nicknameExists)
                {
                    return false;
                }

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
                    return true;
                }
                return false;
            }
            catch (SqlException ex)
            {
                Console.WriteLine("Error de SQL: " + ex.Message);
                return false;
            }
        }

        public async Task<string> SendVerificationCodeAsync(string email)
        {
            var existingPlayer = await playerRepository.GetPlayerForVerificationAsync(email);
            if (existingPlayer != null)
            {
                return "ERROR_EMAIL_EXISTS";
            }

            string verificationCode = emailService.GenerateVerificationCode();
            try
            {
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

                return verificationCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al enviar correo: " + ex.Message);
                return string.Empty;
            }
        }

        public async Task<bool> VerifyCodeAsync(string email, string code)
        {
            try
            {
                var player = await playerRepository.GetPlayerByEmailAsync(email);
                if (player == null)
                {
                    return false;
                }
                if (player.verificationCode == code && DateTime.UtcNow < player.codeExpiryDate)
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al verificar el código: " + ex.Message);
                return false;
            }
        }

        public async Task<bool> HandlePasswordRecoveryRequestAsync(string email)
        {
            try
            {
                var player = await playerRepository.GetPlayerByEmailAsync(email);

                if (player == null || string.IsNullOrEmpty(player.password))
                {
                    return false;
                }

                string recoveryCode = emailService.GenerateVerificationCode();
                player.verificationCode = recoveryCode;
                player.codeExpiryDate = DateTime.UtcNow.AddMinutes(15);

                await playerRepository.SaveChangesAsync();

                var emailTemplate = new RecoveryEmailTemplate(recoveryCode);
                await emailService.SendEmailAsync(player.email, emailTemplate);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error en solicitud de recuperación: " + ex.Message);
                return false;
            }
        }

        public async Task<bool> HandleTokenValidationAsync(string email, string token)
        {
            return await VerifyCodeAsync(email, token);
        }

        public async Task<bool> HandlePasswordResetAsync(string email, string token, string newPassword)
        {
            try
            {
                if (!await HandleTokenValidationAsync(email, token))
                {
                    return false;
                }

                var player = await playerRepository.GetPlayerByEmailAsync(email);
                if (player == null)
                {
                    return false;
                }

                player.password = PasswordHasher.hashPassword(newPassword);

                player.verificationCode = null;
                player.codeExpiryDate = null;

                await playerRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al reiniciar contraseña: " + ex.Message);
                return false;
            }
        }
    }
}
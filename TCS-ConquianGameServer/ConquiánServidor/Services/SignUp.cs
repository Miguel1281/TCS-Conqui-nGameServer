using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Utilities;
using ConquiánServidor.Utilities.Email;
using ConquiánServidor.Utilities.Email.Templates;
using System;
using System.Data.Entity;
using System.Data.Entity.Validation; 
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using ConquiánServidor.Contracts.ServiceContracts;

namespace ConquiánServidor.Services
{
    public class SignUp : ISignUp
    {
        private readonly EmailService emailService = new EmailService();

        public async Task<bool> RegisterPlayerAsync(PlayerDto finalPlayerData)
        {
            try
            {
                using (var context = new ConquiánDBEntities())
                {
                    var nicknameExists = await context.Player.AnyAsync(p => p.nickname == finalPlayerData.nickname);
                    if (nicknameExists)
                    {
                        return false;
                    }

                    var playerToUpdate = await context.Player.FirstOrDefaultAsync(p => p.email == finalPlayerData.email);

                    if (playerToUpdate != null)
                    {
                        playerToUpdate.password = PasswordHasher.hashPassword(finalPlayerData.password);
                        playerToUpdate.nickname = finalPlayerData.nickname;
                        playerToUpdate.name = finalPlayerData.name;
                        playerToUpdate.lastName = finalPlayerData.lastName;
                        playerToUpdate.pathPhoto = finalPlayerData.pathPhoto;
                        playerToUpdate.verificationCode = null;
                        playerToUpdate.codeExpiryDate = null;

                        context.SaveChanges();
                        return true;
                    }
                    return false; 
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine("Error de SQL: " + ex.Message);
                return false;
            }
        }

        public async Task<string> SendVerificationCodeAsync(string email)
        {
            using (var context = new ConquiánDBEntities())
            {
                var existingPlayer = await context.Player.FirstOrDefaultAsync(p => p.email == email && p.password != null);
                if (existingPlayer != null)
                {
                    return "ERROR_EMAIL_EXISTS";
                }
            }

            string verificationCode = emailService.GenerateVerificationCode();
            try
            {
                using (var context = new ConquiánDBEntities())
                {
                    var playerToVerify = await context.Player.FirstOrDefaultAsync(p => p.email == email);

                    if (playerToVerify == null)
                    {
                        playerToVerify = new Player();
                        context.Player.Add(playerToVerify);
                    }

                    playerToVerify.email = email;
                    playerToVerify.verificationCode = verificationCode;
                    playerToVerify.codeExpiryDate = DateTime.UtcNow.AddMinutes(1);

                    await context.SaveChangesAsync();

                    var emailTemplate = new VerificationEmailTemplate(verificationCode);
                    await emailService.SendEmailAsync(email, emailTemplate);

                    return verificationCode;
                }
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
                using (var context = new ConquiánDBEntities())
                {
                    var player = await context.Player.FirstOrDefaultAsync(p => p.email == email);

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
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al verificar el código: " + ex.Message);
                return false;
            }
        }
    }
}
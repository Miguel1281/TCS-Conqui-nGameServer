using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts;
using ConquiánServidor.Utilities.Email;
using ConquiánServidor.Utilities.Email.Templates;
using System;
using System.Data.Entity.Validation; // Necesario para un logging de errores más detallado
using System.Data.SqlClient;
using System.Linq;

namespace ConquiánServidor.Services
{
    public class SignUp : ISignUp
    {
        private readonly EmailService emailService = new EmailService();

        public bool RegisterPlayer(Player finalPlayerData)
        {
            try
            {
                using (var context = new ConquiánDBEntities())
                {
                    var playerToUpdate = context.Player.FirstOrDefault(p => p.email == finalPlayerData.email);

                    if (playerToUpdate != null)
                    {
                        playerToUpdate.password = finalPlayerData.password;
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

        public string SendVerificationCode(string email)
        {
            using (var context = new ConquiánDBEntities())
            {
                var existingPlayer = context.Player.FirstOrDefault(p => p.email == email && p.password != null);
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
                    var playerToVerify = context.Player.FirstOrDefault(p => p.email == email);

                    if (playerToVerify == null)
                    {
                        playerToVerify = new Player();
                        context.Player.Add(playerToVerify);
                    }

                    playerToVerify.email = email;
                    playerToVerify.verificationCode = verificationCode;
                    playerToVerify.codeExpiryDate = DateTime.UtcNow.AddMinutes(1); 

                    context.SaveChanges();

                    var emailTemplate = new VerificationEmailTemplate(verificationCode);
                    emailService.SendEmail(email, emailTemplate);

                    return verificationCode;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al enviar correo: " + ex.Message);
                return string.Empty;
            }
        }
        public bool VerifyCode(string email, string code)
        {
            try
            {
                using (var context = new ConquiánDBEntities())
                {
                    var player = context.Player.FirstOrDefault(p => p.email == email);

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
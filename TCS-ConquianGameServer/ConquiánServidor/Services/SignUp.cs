using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts;
using ConquiánServidor.Utilities.Email;
using ConquiánServidor.Utilities.Email.Templates;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;


namespace ConquiánServidor.Services
{
    public class SignUp:ISignUp
    {
        private readonly EmailService emailService = new EmailService();

        public bool RegisterPlayer(Player newPlayer)
        {
            try
            {
                using (var context = new ConquiánDBEntities())
                {
                    context.Player.Add(newPlayer);
                    context.SaveChanges();
                    return true;
                }
            }
            catch (SqlException ex)
            {
                return false;
            }
        }
        public string SendVerificationCode(string email)
        {
            string verificationCode = emailService.GenerateVerificationCode();
            try
            {
                var emailTemplate = new VerificationEmailTemplate(verificationCode);

                emailService.SendEmail(email, emailTemplate);

                return verificationCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al enviar correo: " + ex.Message);
                return string.Empty;
            }
        }
    }
}

using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;


namespace ConquiánServidor.Services
{
    public class SignUp:ISignUp
    {
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
            catch (Exception ex)
            {
                return false;
            }
        }
        public string SendVerificationCode(string email)
        {
            string verificationCode = GenerateVerificationCode();
            try
            {
                SendEmail(email, verificationCode);
                return verificationCode;
            }
            catch (Exception ex)
            {

                Console.WriteLine("Error al enviar correo: " + ex.Message);
                return string.Empty;
            }
        }

        private string GenerateVerificationCode()
        {
            Random random = new Random();
            const string chars = "0123456789";
            return new string(Enumerable.Repeat(chars, 6)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private void SendEmail(string toEmail, string code)
        {
            string fromMail = "proyectoconquian@gmail.com";
            string fromPassword = "maey tztt bnka oses";

            MailMessage message = new MailMessage();
            message.From = new MailAddress(fromMail);
            message.Subject = "Tu código de acceso";
            message.To.Add(new MailAddress(toEmail));
            message.Body = $"<html><body>Tu código para continuar con el registro es: <strong>{code}</strong></body></html>";
            message.IsBodyHtml = true;

            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(fromMail, fromPassword),
                EnableSsl = true,
            };

            smtpClient.Send(message);
        }

    }
}

using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ConquiánServidor.Utilities.Email
{
    public class EmailService : IEmailService
    {
        private static readonly RandomNumberGenerator randomGenerator = RandomNumberGenerator.Create();
        public string GenerateVerificationCode()
        {
            const string CHAR = "0123456789";
            var data = new byte[6];
            randomGenerator.GetBytes(data);
            return new string(data.Select(b => CHAR[b % CHAR.Length]).ToArray());
        } 
        public async Task SendEmailAsync(string toEmail, IEmailTemplate template)
        {
            string fromMail = ConfigurationManager.AppSettings["EmailUser"];
            string fromPassword = ConfigurationManager.AppSettings["EmailPassword"];

            MailMessage message = new MailMessage();
            message.From = new MailAddress(fromMail);
            message.To.Add(new MailAddress(toEmail));

            message.Subject = template.Subject;
            message.Body = template.HtmlBody;
            message.IsBodyHtml = true;

            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(fromMail, fromPassword),
                EnableSsl = true,
            };

            await smtpClient.SendMailAsync(message);
        }
    }
}

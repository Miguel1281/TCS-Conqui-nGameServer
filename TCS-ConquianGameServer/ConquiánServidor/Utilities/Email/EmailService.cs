using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Configuration;

namespace ConquiánServidor.Utilities.Email
{
    public class EmailService : IEmailService
    {
        public string GenerateVerificationCode()
        {
            Random random = new Random();
            const string chars = "0123456789";
            return new string(Enumerable.Repeat(chars, 6)
              .Select(s => s[random.Next(s.Length)]).ToArray());
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

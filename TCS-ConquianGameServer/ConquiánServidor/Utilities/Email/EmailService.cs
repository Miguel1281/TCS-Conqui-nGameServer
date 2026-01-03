using NLog;
using System;
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
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
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
            Logger.Info($"Initiating email transmission. Subject: {template.Subject}");

            string fromMail = Environment.GetEnvironmentVariable("CONQUIAN_EMAIL_USER");
            string fromPassword = Environment.GetEnvironmentVariable("CONQUIAN_EMAIL_PASSWORD");

            if (string.IsNullOrEmpty(fromMail) || string.IsNullOrEmpty(fromPassword))
            {
                throw new ConfigurationErrorsException("Email credentials are missing");
            }

            try
            {
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

                Logger.Info("Email transmitted successfully via SMTP.");
            }
            catch (SmtpException smtpEx)
            {
                Logger.Error(smtpEx, "SMTP Protocol Error: Failed to send email.");
                throw new InvalidOperationException("Failed to transmit email via SMTP provider.", smtpEx);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "General Error: Failed to send email.");
                throw new InvalidOperationException("An unexpected error occurred while attempting to send email.", ex);
            }
        }
    }
}

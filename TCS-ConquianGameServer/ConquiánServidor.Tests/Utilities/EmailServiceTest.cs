using ConquiánServidor.Utilities.Email;
using Moq;
using System;
using System.Configuration;
using System.Threading.Tasks;
using Xunit;

namespace ConquiánServidor.Tests.Utilities
{
    public class EmailServiceTest
    {
        [Fact]
        public void GenerateVerificationCode_Invoke_ReturnsStringWithLengthOfSix()
        {
            var emailService = new EmailService();

            string result = emailService.GenerateVerificationCode();

            Assert.Equal(6, result.Length);
        }

        [Fact]
        public void GenerateVerificationCode_Invoke_ReturnsStringWithOnlyDigits()
        {
            var emailService = new EmailService();

            string result = emailService.GenerateVerificationCode();

            Assert.Matches("^[0-9]+$", result);
        }

        [Fact]
        public void GenerateVerificationCode_MultipleInvokes_ReturnsDifferentCodes()
        {
            var emailService = new EmailService();

            string code1 = emailService.GenerateVerificationCode();
            string code2 = emailService.GenerateVerificationCode();

            Assert.NotEqual(code1, code2);
        }

        [Fact]
        public async Task SendEmailAsync_MissingEnvironmentVariables_ThrowsConfigurationErrorsException()
        {
            var emailService = new EmailService();
            var mockTemplate = new Mock<IEmailTemplate>();
            mockTemplate.Setup(t => t.Subject).Returns("Test Subject");
            Environment.SetEnvironmentVariable("CONQUIAN_EMAIL_USER", null);
            Environment.SetEnvironmentVariable("CONQUIAN_EMAIL_PASSWORD", null);

            await Assert.ThrowsAsync<ConfigurationErrorsException>(() =>
                emailService.SendEmailAsync("test@example.com", mockTemplate.Object));
        }

        [Fact]
        public async Task SendEmailAsync_InvalidEmailFormat_ThrowsInvalidOperationException()
        {
            var emailService = new EmailService();
            var mockTemplate = new Mock<IEmailTemplate>();
            mockTemplate.Setup(t => t.Subject).Returns("Test Subject");
            Environment.SetEnvironmentVariable("CONQUIAN_EMAIL_USER", "user@test.com");
            Environment.SetEnvironmentVariable("CONQUIAN_EMAIL_PASSWORD", "password");
            string invalidEmail = "correo-invalido";

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                emailService.SendEmailAsync(invalidEmail, mockTemplate.Object));

            Environment.SetEnvironmentVariable("CONQUIAN_EMAIL_USER", null);
            Environment.SetEnvironmentVariable("CONQUIAN_EMAIL_PASSWORD", null);
        }

        [Fact]
        public async Task SendEmailAsync_SmtpConnectionFails_ThrowsInvalidOperationException()
        {
            var emailService = new EmailService();
            var mockTemplate = new Mock<IEmailTemplate>();
            mockTemplate.Setup(t => t.Subject).Returns("Test Subject");
            mockTemplate.Setup(t => t.HtmlBody).Returns("<p>Body</p>");
            Environment.SetEnvironmentVariable("CONQUIAN_EMAIL_USER", "fakeuser@gmail.com");
            Environment.SetEnvironmentVariable("CONQUIAN_EMAIL_PASSWORD", "fakepassword");

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                emailService.SendEmailAsync("test@example.com", mockTemplate.Object));

            Assert.Equal("Failed to transmit email via SMTP provider.", exception.Message);

            Environment.SetEnvironmentVariable("CONQUIAN_EMAIL_USER", null);
            Environment.SetEnvironmentVariable("CONQUIAN_EMAIL_PASSWORD", null);
        }
    }
}

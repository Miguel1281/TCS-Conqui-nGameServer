using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using ConquiánServidor.Utilities.Email;

namespace ConquiánServidor.Tests.Utilities
{
    public class EmailServiceTest
    {
        private class StubEmailTemplate : IEmailTemplate
        {
            public string Subject => "Test Subject";
            public string HtmlBody => "<p>Test Body</p>";
        }

        [Fact]
        public void GenerateVerificationCode_Execution_ReturnsStringWithLengthSix()
        {
            var emailService = new EmailService();
            string code = emailService.GenerateVerificationCode();
            Assert.Equal(6, code.Length);
        }

        [Fact]
        public void GenerateVerificationCode_Execution_ReturnsStringWithOnlyDigits()
        {
            var emailService = new EmailService();
            string code = emailService.GenerateVerificationCode();
            Assert.True(code.All(char.IsDigit));
        }

        [Fact]
        public void GenerateVerificationCode_MultipleExecutions_ReturnsDifferentCodes()
        {
            var emailService = new EmailService();
            string code1 = emailService.GenerateVerificationCode();
            string code2 = emailService.GenerateVerificationCode();
            Assert.NotEqual(code1, code2);
        }

        [Fact]
        public async Task SendEmailAsync_MissingEnvironmentVariables_ThrowsConfigurationErrorsException()
        {
            Environment.SetEnvironmentVariable("CONQUIAN_EMAIL_USER", null);
            Environment.SetEnvironmentVariable("CONQUIAN_EMAIL_PASSWORD", null);
            var emailService = new EmailService();
            var template = new StubEmailTemplate();

            await Assert.ThrowsAsync<ConfigurationErrorsException>(() =>
                emailService.SendEmailAsync("test@example.com", template));
        }

        [Fact]
        public async Task SendEmailAsync_MissingPasswordEnvironmentVariable_ThrowsConfigurationErrorsException()
        {
            Environment.SetEnvironmentVariable("CONQUIAN_EMAIL_USER", "user@test.com");
            Environment.SetEnvironmentVariable("CONQUIAN_EMAIL_PASSWORD", null);
            var emailService = new EmailService();
            var template = new StubEmailTemplate();

            try
            {
                await Assert.ThrowsAsync<ConfigurationErrorsException>(() =>
                    emailService.SendEmailAsync("test@example.com", template));
            }
            finally
            {
                Environment.SetEnvironmentVariable("CONQUIAN_EMAIL_USER", null);
            }
        }

        [Fact]
        public async Task SendEmailAsync_InvalidCredentials_ThrowsInvalidOperationException()
        {
            Environment.SetEnvironmentVariable("CONQUIAN_EMAIL_USER", "dummyuser@test.com");
            Environment.SetEnvironmentVariable("CONQUIAN_EMAIL_PASSWORD", "dummypassword");
            var emailService = new EmailService();
            var template = new StubEmailTemplate();

            try
            {
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    emailService.SendEmailAsync("destiny@example.com", template));

                Assert.Contains("Failed to transmit email via SMTP provider", exception.Message);
            }
            finally
            {
                Environment.SetEnvironmentVariable("CONQUIAN_EMAIL_USER", null);
                Environment.SetEnvironmentVariable("CONQUIAN_EMAIL_PASSWORD", null);
            }
        }
    }
}
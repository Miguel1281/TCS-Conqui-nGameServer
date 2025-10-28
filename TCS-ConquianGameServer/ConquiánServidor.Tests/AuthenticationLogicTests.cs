using ConquiánServidor.BusinessLogic;
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.Utilities.Email;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ConquiánServidor.Tests
{
    public class AuthenticationLogicTests
    {
        private readonly Mock<IPlayerRepository> mockPlayerRepository;
        private readonly AuthenticationLogic authLogic;
        private readonly Mock<IEmailService> mockEmailService;

        public AuthenticationLogicTests()
        {
            mockPlayerRepository = new Mock<IPlayerRepository>();
            mockEmailService = new Mock<IEmailService>();
            authLogic = new AuthenticationLogic(mockPlayerRepository.Object, mockEmailService.Object);
        }

        [Fact]
        public async Task SendVerificationCodeAsync_ShouldReturnEmptyString_WhenEmailFormatIsInvalid()
        {
            string invalidEmail = "correo-invalido.com";

            string result = await authLogic.SendVerificationCodeAsync(invalidEmail);

            Assert.Equal(string.Empty, result);
            mockPlayerRepository.Verify(r => r.GetPlayerForVerificationAsync(It.IsAny<string>()), Times.Never());
        }

        [Fact]
        public async Task SendVerificationCodeAsync_ShouldReturnError_WhenEmailAlreadyExists()
        {
            string existingEmail = "yaexiste@dominio.com";

            mockPlayerRepository.Setup(r => r.GetPlayerForVerificationAsync(existingEmail))
                                .ReturnsAsync(new Player());

            string result = await authLogic.SendVerificationCodeAsync(existingEmail);

            Assert.Equal("ERROR_EMAIL_EXISTS", result);
        }

        [Fact]
        public async Task SendVerificationCodeAsync_ShouldReturnCode_WhenEmailIsNew()
        {
            string newEmail = "nuevo@dominio.com";
            string testCode = "123456";

            mockPlayerRepository.Setup(r => r.GetPlayerForVerificationAsync(newEmail))
                                .ReturnsAsync((Player)null);

            mockPlayerRepository.Setup(r => r.GetPlayerByEmailAsync(newEmail))
                                .ReturnsAsync((Player)null);

            mockEmailService.Setup(s => s.GenerateVerificationCode()).Returns(testCode);

            mockEmailService.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<IEmailTemplate>()))
                            .Returns(Task.CompletedTask);

            string result = await authLogic.SendVerificationCodeAsync(newEmail);

            Assert.Equal(testCode, result); 

            Assert.NotEqual(string.Empty, result);
            Assert.NotEqual("ERROR_EMAIL_EXISTS", result);

            mockPlayerRepository.Verify(r => r.AddPlayer(It.IsAny<Player>()), Times.Once());
            mockPlayerRepository.Verify(r => r.SaveChangesAsync(), Times.Once());

            mockEmailService.Verify(s => s.SendEmailAsync(newEmail, It.IsAny<IEmailTemplate>()), Times.Once());
        }


        [Fact]
        public async Task VerifyCodeAsync_ShouldReturnTrue_WhenCodeIsValidAndNotExpired()
        {
            string email = "test@dominio.com";
            string code = "123456";
            var player = new Player
            {
                email = email,
                verificationCode = code,
                codeExpiryDate = DateTime.UtcNow.AddMinutes(5)
            };

            mockPlayerRepository.Setup(r => r.GetPlayerByEmailAsync(email)).ReturnsAsync(player);

            bool result = await authLogic.VerifyCodeAsync(email, code);

            Assert.True(result);
        }

        [Theory]
        [InlineData("wrong-code", "123456")]
        [InlineData("123456", "wrong-code")]
        public async Task VerifyCodeAsync_ShouldReturnFalse_WhenCodeIsInvalid(string playerCode, string providedCode)
        {
            string email = "test@dominio.com";
            var player = new Player
            {
                email = email,
                verificationCode = playerCode,
                codeExpiryDate = DateTime.UtcNow.AddMinutes(5)
            };

            mockPlayerRepository.Setup(r => r.GetPlayerByEmailAsync(email)).ReturnsAsync(player);

            bool result = await authLogic.VerifyCodeAsync(email, providedCode);

            Assert.False(result);
        }

        [Fact]
        public async Task VerifyCodeAsync_ShouldReturnFalse_WhenCodeIsExpired()
        {
            string email = "test@dominio.com";
            string code = "123456";
            var player = new Player
            {
                email = email,
                verificationCode = code,
                codeExpiryDate = DateTime.UtcNow.AddMinutes(-5)
            };

            mockPlayerRepository.Setup(r => r.GetPlayerByEmailAsync(email)).ReturnsAsync(player);

            bool result = await authLogic.VerifyCodeAsync(email, code);

            Assert.False(result);
        }

        [Fact]
        public async Task RegisterPlayerAsync_ShouldReturnFalse_WhenValidationFails()
        {
            var invalidDto = new PlayerDto
            {
                name = "NombreValido",
                lastName = "ApellidoValido",
                nickname = "NickValido",
                email = "correo@valido.com",
                password = "corta"
            };

            bool result = await authLogic.RegisterPlayerAsync(invalidDto);

            Assert.False(result);
            mockPlayerRepository.Verify(r => r.DoesNicknameExistAsync(It.IsAny<string>()), Times.Never());
            mockPlayerRepository.Verify(r => r.SaveChangesAsync(), Times.Never());
        }

        [Fact]
        public async Task RegisterPlayerAsync_ShouldReturnFalse_WhenNicknameAlreadyExists()
        {
            var validDto = new PlayerDto
            {
                name = "NombreValido",
                lastName = "ApellidoValido",
                nickname = "NickExistente",
                email = "correo@valido.com",
                password = "P@sswordValido1"
            };

            mockPlayerRepository.Setup(r => r.DoesNicknameExistAsync("NickExistente")).ReturnsAsync(true);

            bool result = await authLogic.RegisterPlayerAsync(validDto);

            Assert.False(result);
            mockPlayerRepository.Verify(r => r.DoesNicknameExistAsync("NickExistente"), Times.Once());
            mockPlayerRepository.Verify(r => r.SaveChangesAsync(), Times.Never());
        }

        [Fact]
        public async Task RegisterPlayerAsync_ShouldReturnTrue_WhenDataIsValidAndNew()
        {
            var validDto = new PlayerDto
            {
                name = "NombreValido",
                lastName = "ApellidoValido",
                nickname = "NickNuevo",
                email = "correo@valido.com",
                password = "P@sswordValido1"
            };

            mockPlayerRepository.Setup(r => r.DoesNicknameExistAsync("NickNuevo")).ReturnsAsync(false);
            mockPlayerRepository.Setup(r => r.GetPlayerByEmailAsync("correo@valido.com"))
                                .ReturnsAsync(new Player());

            bool result = await authLogic.RegisterPlayerAsync(validDto);

            Assert.True(result);
            mockPlayerRepository.Verify(r => r.SaveChangesAsync(), Times.Once());
        }
    }
}

using ConquiánServidor.BusinessLogic;
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.DataAccess.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ConquiánServidor.Tests
{
    public class AuthenticationLogicTests
    {
        private readonly Mock<IPlayerRepository> mockPlayerRepository;
        private readonly AuthenticationLogic authLogic;

        public AuthenticationLogicTests()
        {
            // Arrange global
            mockPlayerRepository = new Mock<IPlayerRepository>();
            authLogic = new AuthenticationLogic(mockPlayerRepository.Object);
        }

        [Fact]
        public async Task SendVerificationCodeAsync_ShouldReturnEmptyString_WhenEmailFormatIsInvalid()
        {
            // Arrange
            string invalidEmail = "correo-invalido.com";

            // Act
            string result = await authLogic.SendVerificationCodeAsync(invalidEmail);

            // Assert
            Assert.Equal(string.Empty, result);
            mockPlayerRepository.Verify(r => r.GetPlayerForVerificationAsync(It.IsAny<string>()), Times.Never());
        }

        [Fact]
        public async Task SendVerificationCodeAsync_ShouldReturnError_WhenEmailAlreadyExists()
        {
            // Arrange
            string existingEmail = "yaexiste@dominio.com";

            mockPlayerRepository.Setup(r => r.GetPlayerForVerificationAsync(existingEmail))
                                .ReturnsAsync(new Player()); //

            // Act
            string result = await authLogic.SendVerificationCodeAsync(existingEmail);

            // Assert
            Assert.Equal("ERROR_EMAIL_EXISTS", result);
        }

        [Fact]
        public async Task SendVerificationCodeAsync_ShouldReturnCode_WhenEmailIsNew()
        {
            // Arrange
            string newEmail = "nuevo@dominio.com";

            mockPlayerRepository.Setup(r => r.GetPlayerForVerificationAsync(newEmail))
                                .ReturnsAsync((Player)null);

            mockPlayerRepository.Setup(r => r.GetPlayerByEmailAsync(newEmail))
                                .ReturnsAsync((Player)null);

            // Act
            string result = await authLogic.SendVerificationCodeAsync(newEmail);

            // Assert
            Assert.NotEqual(string.Empty, result);
            Assert.NotEqual("ERROR_EMAIL_EXISTS", result);
            Assert.True(result.Length > 0);

            mockPlayerRepository.Verify(r => r.AddPlayer(It.IsAny<Player>()), Times.Once());
            mockPlayerRepository.Verify(r => r.SaveChangesAsync(), Times.Once());
        }


        [Fact]
        public async Task VerifyCodeAsync_ShouldReturnTrue_WhenCodeIsValidAndNotExpired()
        {
            // Arrange
            string email = "test@dominio.com";
            string code = "123456";
            var player = new Player //
            {
                email = email,
                verificationCode = code,
                codeExpiryDate = DateTime.UtcNow.AddMinutes(5)
            };

            mockPlayerRepository.Setup(r => r.GetPlayerByEmailAsync(email)).ReturnsAsync(player);

            // Act
            bool result = await authLogic.VerifyCodeAsync(email, code);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("wrong-code", "123456")]
        [InlineData("123456", "wrong-code")]
        public async Task VerifyCodeAsync_ShouldReturnFalse_WhenCodeIsInvalid(string playerCode, string providedCode)
        {
            // Arrange
            string email = "test@dominio.com";
            var player = new Player //
            {
                email = email,
                verificationCode = playerCode,
                codeExpiryDate = DateTime.UtcNow.AddMinutes(5)
            };

            mockPlayerRepository.Setup(r => r.GetPlayerByEmailAsync(email)).ReturnsAsync(player);

            // Act
            bool result = await authLogic.VerifyCodeAsync(email, providedCode);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task VerifyCodeAsync_ShouldReturnFalse_WhenCodeIsExpired()
        {
            // Arrange
            string email = "test@dominio.com";
            string code = "123456";
            var player = new Player //
            {
                email = email,
                verificationCode = code,
                codeExpiryDate = DateTime.UtcNow.AddMinutes(-5)
            };

            mockPlayerRepository.Setup(r => r.GetPlayerByEmailAsync(email)).ReturnsAsync(player);

            // Act
            bool result = await authLogic.VerifyCodeAsync(email, code);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task RegisterPlayerAsync_ShouldReturnFalse_WhenValidationFails()
        {
            // Arrange
            var invalidDto = new PlayerDto
            {
                name = "NombreValido",
                lastName = "ApellidoValido",
                nickname = "NickValido",
                email = "correo@valido.com",
                password = "corta"
            };

            // Act
            bool result = await authLogic.RegisterPlayerAsync(invalidDto);

            // Assert
            Assert.False(result);
            mockPlayerRepository.Verify(r => r.DoesNicknameExistAsync(It.IsAny<string>()), Times.Never());
            mockPlayerRepository.Verify(r => r.SaveChangesAsync(), Times.Never());
        }

        [Fact]
        public async Task RegisterPlayerAsync_ShouldReturnFalse_WhenNicknameAlreadyExists()
        {
            // Arrange
            var validDto = new PlayerDto //
            {
                name = "NombreValido",
                lastName = "ApellidoValido",
                nickname = "NickExistente",
                email = "correo@valido.com",
                password = "P@sswordValido1"
            };

            mockPlayerRepository.Setup(r => r.DoesNicknameExistAsync("NickExistente")).ReturnsAsync(true);

            // Act
            bool result = await authLogic.RegisterPlayerAsync(validDto);

            // Assert
            Assert.False(result);
            mockPlayerRepository.Verify(r => r.DoesNicknameExistAsync("NickExistente"), Times.Once());
            mockPlayerRepository.Verify(r => r.SaveChangesAsync(), Times.Never());
        }

        [Fact]
        public async Task RegisterPlayerAsync_ShouldReturnTrue_WhenDataIsValidAndNew()
        {
            // Arrange
            var validDto = new PlayerDto //
            {
                name = "NombreValido",
                lastName = "ApellidoValido",
                nickname = "NickNuevo",
                email = "correo@valido.com",
                password = "P@sswordValido1"
            };

            mockPlayerRepository.Setup(r => r.DoesNicknameExistAsync("NickNuevo")).ReturnsAsync(false);
            mockPlayerRepository.Setup(r => r.GetPlayerByEmailAsync("correo@valido.com"))
                                .ReturnsAsync(new Player()); //

            // Act
            bool result = await authLogic.RegisterPlayerAsync(validDto);

            // Assert
            Assert.True(result);
            mockPlayerRepository.Verify(r => r.SaveChangesAsync(), Times.Once());
        }
    }
}

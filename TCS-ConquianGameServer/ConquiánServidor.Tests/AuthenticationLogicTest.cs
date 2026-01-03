using Autofac;
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
    public class AuthenticationLogicTest
    {
        private readonly Mock<IPlayerRepository> mockPlayerRepository;
        private readonly AuthenticationLogic authLogic;
        private readonly Mock<IEmailService> mockEmailService;
        private readonly Mock<PresenceManager> mockPresenceManager;
        public AuthenticationLogicTest()
        {
            mockPlayerRepository = new Mock<IPlayerRepository>();
            mockEmailService = new Mock<IEmailService>();

            var mockScope = new Mock<ILifetimeScope>();

            mockPresenceManager = new Mock<PresenceManager>(mockScope.Object);

            authLogic = new AuthenticationLogic(
                mockPlayerRepository.Object,
                mockEmailService.Object,
                mockPresenceManager.Object
            );
        }
        [Fact]
        public async Task SendVerificationCodeAsync_ShouldThrowArgumentException_WhenEmailFormatIsInvalid()
        {
            string invalidEmail = "correo-invalido.com";

            await Assert.ThrowsAsync<ArgumentException>(() => authLogic.SendVerificationCodeAsync(invalidEmail));

            mockPlayerRepository.Verify(r => r.GetPlayerForVerificationAsync(It.IsAny<string>()), Times.Never());
        }

        [Fact]
        public async Task SendVerificationCodeAsync_ShouldThrowInvalidOperationException_WhenEmailAlreadyExists()
        {
            string existingEmail = "yaexiste@dominio.com";

            mockPlayerRepository.Setup(r => r.GetPlayerForVerificationAsync(existingEmail))
                                .ReturnsAsync(new Player());

            await Assert.ThrowsAsync<InvalidOperationException>(() => authLogic.SendVerificationCodeAsync(existingEmail));
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

            mockPlayerRepository.Verify(r => r.AddPlayer(It.IsAny<Player>()), Times.Once());
            mockPlayerRepository.Verify(r => r.SaveChangesAsync(), Times.Once());
            mockEmailService.Verify(s => s.SendEmailAsync(newEmail, It.IsAny<IEmailTemplate>()), Times.Once());
        }

        [Fact]
        public async Task VerifyCodeAsync_ShouldComplete_WhenCodeIsValidAndNotExpired()
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

            await authLogic.VerifyCodeAsync(email, code);

        }

        [Theory]
        [InlineData("wrong-code", "123456")]
        [InlineData("123456", "wrong-code")]
        public async Task VerifyCodeAsync_ShouldThrowArgumentException_WhenCodeIsInvalid(string playerCode, string providedCode)
        {
            string email = "test@dominio.com";
            var player = new Player
            {
                email = email,
                verificationCode = playerCode,
                codeExpiryDate = DateTime.UtcNow.AddMinutes(5)
            };

            mockPlayerRepository
                .Setup(r => r.GetPlayerByEmailAsync(email))
                .ReturnsAsync(player);

            await Assert.ThrowsAsync<ArgumentException>(() => authLogic.VerifyCodeAsync(email, providedCode));
        }

        [Fact]
        public async Task VerifyCodeAsync_ShouldThrowArgumentException_WhenCodeIsExpired()
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

            await Assert.ThrowsAsync<ArgumentException>(() => authLogic.VerifyCodeAsync(email, code));
        }

        [Fact]
        public async Task RegisterPlayerAsync_ShouldThrowArgumentException_WhenValidationFails()
        {
            var invalidDto = new PlayerDto
            {
                name = "NombreValido",
                lastName = "ApellidoValido",
                nickname = "NickValido",
                email = "correo@valido.com",
                password = "corta"
            };

            await Assert.ThrowsAsync<ArgumentException>(() => authLogic.RegisterPlayerAsync(invalidDto));

            mockPlayerRepository.Verify(r => r.DoesNicknameExistAsync(It.IsAny<string>()), Times.Never());
            mockPlayerRepository.Verify(r => r.SaveChangesAsync(), Times.Never());
        }

        [Fact]
        public async Task RegisterPlayerAsync_ShouldThrowInvalidOperationException_WhenNicknameAlreadyExists()
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

            await Assert.ThrowsAsync<InvalidOperationException>(() => authLogic.RegisterPlayerAsync(validDto));

            mockPlayerRepository.Verify(r => r.DoesNicknameExistAsync("NickExistente"), Times.Once());
            mockPlayerRepository.Verify(r => r.SaveChangesAsync(), Times.Never());
        }

        [Fact]
        public async Task RegisterPlayerAsync_ShouldComplete_WhenDataIsValidAndNew()
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
                                .ReturnsAsync(new Player { email = "correo@valido.com" });

            await authLogic.RegisterPlayerAsync(validDto);

            mockPlayerRepository.Verify(r => r.SaveChangesAsync(), Times.Once());
        }

        [Fact]
        public async Task DeleteTemporaryPlayerAsync_ShouldCallDelete_WhenPlayerIsIncomplete()
        {
            string email = "incomplete@test.com";
            var incompletePlayer = new Player
            {
                email = email,
                name = null
            };

            mockPlayerRepository.Setup(r => r.GetPlayerByEmailAsync(email))
                                .ReturnsAsync(incompletePlayer);

            await authLogic.DeleteTemporaryPlayerAsync(email);

            mockPlayerRepository.Verify(r => r.GetPlayerByEmailAsync(email), Times.Once());
            mockPlayerRepository.Verify(r => r.DeletePlayerAsync(incompletePlayer), Times.Once());
        }

        [Fact]
        public async Task DeleteTemporaryPlayerAsync_ShouldNotCallDelete_WhenPlayerIsAlreadyComplete()
        {
            string email = "complete@test.com";
            var completePlayer = new Player
            {
                email = email,
                name = "UsuarioCompleto"
            };

            mockPlayerRepository.Setup(r => r.GetPlayerByEmailAsync(email))
                                .ReturnsAsync(completePlayer);

            await authLogic.DeleteTemporaryPlayerAsync(email);

            mockPlayerRepository.Verify(r => r.GetPlayerByEmailAsync(email), Times.Once());
            mockPlayerRepository.Verify(r => r.DeletePlayerAsync(It.IsAny<Player>()), Times.Never());
        }

        [Fact]
        public async Task DeleteTemporaryPlayerAsync_ShouldNotCallDelete_WhenPlayerNotFound()
        {
            string email = "notfound@test.com";

            mockPlayerRepository.Setup(r => r.GetPlayerByEmailAsync(email))
                                .ReturnsAsync((Player)null);

            await authLogic.DeleteTemporaryPlayerAsync(email);

            mockPlayerRepository.Verify(r => r.GetPlayerByEmailAsync(email), Times.Once());
            mockPlayerRepository.Verify(r => r.DeletePlayerAsync(It.IsAny<Player>()), Times.Never());
        }
    }
}
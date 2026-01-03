using ConquiánServidor.BusinessLogic;
using ConquiánServidor.Utilities.Email.Templates;
using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.Enums;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.Utilities;
using ConquiánServidor.Utilities.Email;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ConquiánServidor.Tests.BusinessLogic
{
    public class AuthenticationLogicTest
    {
        private readonly Mock<IPlayerRepository> playerRepositoryMock;
        private readonly Mock<IEmailService> emailServiceMock;
        private readonly Mock<IPresenceManager> presenceManagerMock;
        private readonly AuthenticationLogic authenticationLogic;

        public AuthenticationLogicTest()
        {
            playerRepositoryMock = new Mock<IPlayerRepository>();
            emailServiceMock = new Mock<IEmailService>();
            presenceManagerMock = new Mock<IPresenceManager>();

            authenticationLogic = new AuthenticationLogic(
                playerRepositoryMock.Object,
                emailServiceMock.Object,
                presenceManagerMock.Object
            );
        }

        [Fact]
        public async Task AuthenticatePlayerAsync_ValidCredentials_ReturnsPlayerDto()
        {
            string email = "test@test.com";
            string password = "StrongPassword123!";
            string hashedPassword = PasswordHasher.hashPassword(password);

            var player = new Player
            {
                idPlayer = 1,
                email = email,
                password = hashedPassword,
                nickname = "Tester",
                pathPhoto = "photo.png"
            };

            playerRepositoryMock.Setup(r => r.GetPlayerByEmailAsync(email)).ReturnsAsync(player);
            presenceManagerMock.Setup(p => p.IsPlayerOnline(player.idPlayer)).Returns(false);

            var result = await authenticationLogic.AuthenticatePlayerAsync(email, password);

            Assert.NotNull(result);
            Assert.Equal(player.idPlayer, result.idPlayer);
            Assert.Equal(PlayerStatus.Online, result.Status);
            presenceManagerMock.Verify(p => p.NotifyStatusChange(player.idPlayer, (int)PlayerStatus.Online), Times.Once);
        }

        [Fact]
        public async Task AuthenticatePlayerAsync_InvalidPassword_ThrowsBusinessLogicException()
        {
            string email = "test@test.com";
            string password = "CorrectPassword";
            string wrongPassword = "WrongPassword";
            string hashedPassword = PasswordHasher.hashPassword(password);

            var player = new Player { idPlayer = 1, email = email, password = hashedPassword };

            playerRepositoryMock.Setup(r => r.GetPlayerByEmailAsync(email)).ReturnsAsync(player);

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() =>
                authenticationLogic.AuthenticatePlayerAsync(email, wrongPassword));

            Assert.Equal(ServiceErrorType.InvalidPassword, exception.ErrorType);
        }

        [Fact]
        public async Task AuthenticatePlayerAsync_UserNotFound_ThrowsBusinessLogicException()
        {
            string email = "unknown@test.com";
            string password = "AnyPassword";

            playerRepositoryMock.Setup(r => r.GetPlayerByEmailAsync(email)).ReturnsAsync((Player)null);

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() =>
                authenticationLogic.AuthenticatePlayerAsync(email, password));

            Assert.Equal(ServiceErrorType.InvalidPassword, exception.ErrorType);
        }

        [Fact]
        public async Task AuthenticatePlayerAsync_UserAlreadyOnline_ThrowsBusinessLogicException()
        {
            string email = "test@test.com";
            string password = "StrongPassword123!";
            string hashedPassword = PasswordHasher.hashPassword(password);

            var player = new Player { idPlayer = 1, email = email, password = hashedPassword };

            playerRepositoryMock.Setup(r => r.GetPlayerByEmailAsync(email)).ReturnsAsync(player);
            presenceManagerMock.Setup(p => p.IsPlayerOnline(player.idPlayer)).Returns(true);

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() =>
                authenticationLogic.AuthenticatePlayerAsync(email, password));

            Assert.Equal(ServiceErrorType.SessionActive, exception.ErrorType);
        }

        [Fact]
        public async Task SignOutPlayerAsync_Invoke_NotifiesOfflineStatus()
        {
            int playerId = 1;

            await authenticationLogic.SignOutPlayerAsync(playerId);

            presenceManagerMock.Verify(p => p.NotifyStatusChange(playerId, (int)PlayerStatus.Offline), Times.Once);
        }

        [Fact]
        public async Task RegisterPlayerAsync_ValidData_UpdatesPlayer()
        {
            var dto = new PlayerDto
            {
                email = "new@test.com",
                password = "StrongPass123!",
                name = "Juan",
                lastName = "Perez",
                nickname = "JuanP",
                pathPhoto = "default.png"
            };

            var existingTempPlayer = new Player { idPlayer = 1, email = dto.email };

            playerRepositoryMock.Setup(r => r.DoesNicknameExistAsync(dto.nickname)).ReturnsAsync(false);
            playerRepositoryMock.Setup(r => r.GetPlayerByEmailAsync(dto.email)).ReturnsAsync(existingTempPlayer);

            await authenticationLogic.RegisterPlayerAsync(dto);

            playerRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
            Assert.Equal(dto.name, existingTempPlayer.name);
            Assert.NotNull(existingTempPlayer.password);
        }

        [Fact]
        public async Task RegisterPlayerAsync_InvalidName_ThrowsBusinessLogicException()
        {
            var dto = new PlayerDto { name = "Juan123", lastName = "Perez", nickname = "JuanP", password = "Pass" };

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() =>
                authenticationLogic.RegisterPlayerAsync(dto));

            Assert.Equal(ServiceErrorType.InvalidNameFormat, exception.ErrorType);
        }

        [Fact]
        public async Task RegisterPlayerAsync_WeakPassword_ThrowsBusinessLogicException()
        {
            var dto = new PlayerDto
            {
                name = "Juan",
                lastName = "Perez",
                nickname = "JuanP",
                password = "123"
            };

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() =>
                authenticationLogic.RegisterPlayerAsync(dto));

            Assert.Equal(ServiceErrorType.InvalidPasswordFormat, exception.ErrorType);
        }

        [Fact]
        public async Task RegisterPlayerAsync_NicknameExists_ThrowsBusinessLogicException()
        {
            var dto = new PlayerDto
            {
                name = "Juan",
                lastName = "Perez",
                nickname = "ExistingNick",
                password = "StrongPass123!"
            };

            playerRepositoryMock.Setup(r => r.DoesNicknameExistAsync(dto.nickname)).ReturnsAsync(true);

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() =>
                authenticationLogic.RegisterPlayerAsync(dto));

            Assert.Equal(ServiceErrorType.DuplicateRecord, exception.ErrorType);
        }

        [Fact]
        public async Task RegisterPlayerAsync_TempUserNotFound_ThrowsBusinessLogicException()
        {
            var dto = new PlayerDto
            {
                email = "ghost@test.com",
                name = "Juan",
                lastName = "Perez",
                nickname = "JuanP",
                password = "StrongPass123!"
            };

            playerRepositoryMock.Setup(r => r.DoesNicknameExistAsync(dto.nickname)).ReturnsAsync(false);
            playerRepositoryMock.Setup(r => r.GetPlayerByEmailAsync(dto.email)).ReturnsAsync((Player)null);

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() =>
                authenticationLogic.RegisterPlayerAsync(dto));

            Assert.Equal(ServiceErrorType.UserNotFound, exception.ErrorType);
        }

        [Fact]
        public async Task SendVerificationCodeAsync_NewUser_CreatesPlayerAndSendsEmail()
        {
            string email = "newuser@test.com";
            string generatedCode = "123456";

            playerRepositoryMock.Setup(r => r.GetPlayerForVerificationAsync(email)).ReturnsAsync((Player)null);
            playerRepositoryMock.Setup(r => r.GetPlayerByEmailAsync(email)).ReturnsAsync((Player)null);
            emailServiceMock.Setup(e => e.GenerateVerificationCode()).Returns(generatedCode);

            string result = await authenticationLogic.SendVerificationCodeAsync(email);

            Assert.Equal(generatedCode, result);
            playerRepositoryMock.Verify(r => r.AddPlayer(It.IsAny<Player>()), Times.Once);
            playerRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
            emailServiceMock.Verify(e => e.SendEmailAsync(email, It.IsAny<VerificationEmailTemplate>()), Times.Once);
        }

        [Fact]
        public async Task SendVerificationCodeAsync_AlreadyRegistered_ThrowsBusinessLogicException()
        {
            string email = "registered@test.com";
            var existingPlayer = new Player { idPlayer = 1 };

            playerRepositoryMock.Setup(r => r.GetPlayerForVerificationAsync(email)).ReturnsAsync(existingPlayer);

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() =>
                authenticationLogic.SendVerificationCodeAsync(email));

            Assert.Equal(ServiceErrorType.RegisteredMail, exception.ErrorType);
        }

        [Fact]
        public async Task VerifyCodeAsync_ValidCode_Success()
        {
            string email = "test@test.com";
            string code = "123456";
            var player = new Player
            {
                email = email,
                verificationCode = code,
                codeExpiryDate = DateTime.UtcNow.AddMinutes(5)
            };

            playerRepositoryMock.Setup(r => r.GetPlayerByEmailAsync(email)).ReturnsAsync(player);

            await authenticationLogic.VerifyCodeAsync(email, code);
        }

        [Fact]
        public async Task VerifyCodeAsync_ExpiredCode_ThrowsBusinessLogicException()
        {
            string email = "test@test.com";
            string code = "123456";
            var player = new Player
            {
                email = email,
                verificationCode = code,
                codeExpiryDate = DateTime.UtcNow.AddMinutes(-5)
            };

            playerRepositoryMock.Setup(r => r.GetPlayerByEmailAsync(email)).ReturnsAsync(player);

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() =>
                authenticationLogic.VerifyCodeAsync(email, code));

            Assert.Equal(ServiceErrorType.VerificationCodeExpired, exception.ErrorType);
        }

        [Fact]
        public async Task VerifyCodeAsync_IncorrectCode_ThrowsBusinessLogicException()
        {
            string email = "test@test.com";
            var player = new Player
            {
                email = email,
                verificationCode = "123456",
                codeExpiryDate = DateTime.UtcNow.AddMinutes(5)
            };

            playerRepositoryMock.Setup(r => r.GetPlayerByEmailAsync(email)).ReturnsAsync(player);

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() =>
                authenticationLogic.VerifyCodeAsync(email, "654321"));

            Assert.Equal(ServiceErrorType.InvalidVerificationCode, exception.ErrorType);
        }

        [Fact]
        public async Task HandlePasswordResetAsync_ValidInput_UpdatesPassword()
        {
            string email = "test@test.com";
            string token = "123456";
            string newPassword = "NewPassword1!";
            var player = new Player
            {
                email = email,
                verificationCode = token,
                codeExpiryDate = DateTime.UtcNow.AddMinutes(10)
            };

            playerRepositoryMock.Setup(r => r.GetPlayerByEmailAsync(email)).ReturnsAsync(player);

            await authenticationLogic.HandlePasswordResetAsync(email, token, newPassword);

            playerRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
            Assert.Null(player.verificationCode);
            Assert.NotEqual(newPassword, player.password);
        }

        [Fact]
        public async Task DeleteTemporaryPlayerAsync_PlayerNoName_DeletesPlayer()
        {
            string email = "temp@test.com";
            var player = new Player { email = email, name = null };

            playerRepositoryMock.Setup(r => r.GetPlayerByEmailAsync(email)).ReturnsAsync(player);

            await authenticationLogic.DeleteTemporaryPlayerAsync(email);

            playerRepositoryMock.Verify(r => r.DeletePlayerAsync(player), Times.Once);
        }

        [Fact]
        public async Task DeleteTemporaryPlayerAsync_PlayerWithName_DoesNotDelete()
        {
            string email = "registered@test.com";
            var player = new Player { email = email, name = "Juan" };

            playerRepositoryMock.Setup(r => r.GetPlayerByEmailAsync(email)).ReturnsAsync(player);

            await authenticationLogic.DeleteTemporaryPlayerAsync(email);

            playerRepositoryMock.Verify(r => r.DeletePlayerAsync(It.IsAny<Player>()), Times.Never);
        }
    }
}
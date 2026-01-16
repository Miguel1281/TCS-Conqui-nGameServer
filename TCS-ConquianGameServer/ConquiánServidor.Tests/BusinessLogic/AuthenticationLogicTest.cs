using ConquiánServidor.BusinessLogic.Authentication;
using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.Enums;
using ConquiánServidor.ConquiánDB.Abstractions;
using ConquiánServidor.Utilities;
using ConquiánServidor.Utilities.Email;
using ConquiánServidor.Utilities.Email.Templates;
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
        public async Task AuthenticatePlayerAsync_ValidCredentialsAndOffline_ReturnsNotNull()
        {
            string email = "test@example.com";
            string password = "Password1$";
            string hashedPassword = PasswordHasher.hashPassword(password);
            var player = new Player { idPlayer = 1, email = email, password = hashedPassword };

            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(email)).ReturnsAsync(player);
            presenceManagerMock.Setup(pm => pm.IsPlayerOnline(player.idPlayer)).Returns(false);

            var result = await authenticationLogic.AuthenticatePlayerAsync(email, password);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task AuthenticatePlayerAsync_ValidCredentialsAndOffline_ReturnsCorrectId()
        {
            string email = "test@example.com";
            string password = "Password1$";
            string hashedPassword = PasswordHasher.hashPassword(password);
            var player = new Player { idPlayer = 1, email = email, password = hashedPassword };

            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(email)).ReturnsAsync(player);
            presenceManagerMock.Setup(pm => pm.IsPlayerOnline(player.idPlayer)).Returns(false);

            var result = await authenticationLogic.AuthenticatePlayerAsync(email, password);

            Assert.Equal(player.idPlayer, result.idPlayer);
        }

        [Fact]
        public async Task AuthenticatePlayerAsync_ValidCredentialsAndOffline_ReturnsOnlineStatus()
        {
            string email = "test@example.com";
            string password = "Password1$";
            string hashedPassword = PasswordHasher.hashPassword(password);
            var player = new Player { idPlayer = 1, email = email, password = hashedPassword };

            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(email)).ReturnsAsync(player);
            presenceManagerMock.Setup(pm => pm.IsPlayerOnline(player.idPlayer)).Returns(false);

            var result = await authenticationLogic.AuthenticatePlayerAsync(email, password);

            Assert.Equal(PlayerStatus.Online, result.Status);
        }

        [Fact]
        public async Task AuthenticatePlayerAsync_ValidCredentialsAndOffline_NotifiesPresenceManager()
        {
            string email = "test@example.com";
            string password = "Password1$";
            string hashedPassword = PasswordHasher.hashPassword(password);
            var player = new Player { idPlayer = 1, email = email, password = hashedPassword };

            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(email)).ReturnsAsync(player);
            presenceManagerMock.Setup(pm => pm.IsPlayerOnline(player.idPlayer)).Returns(false);

            await authenticationLogic.AuthenticatePlayerAsync(email, password);

            presenceManagerMock.Verify(pm => pm.NotifyStatusChange(player.idPlayer, (int)PlayerStatus.Online), Times.Once);
        }

        [Fact]
        public async Task AuthenticatePlayerAsync_UserNotFound_ThrowsInvalidPasswordException()
        {
            string email = "nonexistent@example.com";
            string password = "Password1$";
            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(email)).ReturnsAsync((Player)null);

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() => authenticationLogic.AuthenticatePlayerAsync(email, password));

            Assert.Equal(ServiceErrorType.InvalidPassword, exception.ErrorType);
        }

        [Fact]
        public async Task AuthenticatePlayerAsync_WrongPassword_ThrowsInvalidPasswordException()
        {
            string email = "test@example.com";
            string correctPassword = "Password1$";
            string wrongPassword = "WrongPassword1$";
            string hashedPassword = PasswordHasher.hashPassword(correctPassword);
            var player = new Player { idPlayer = 1, email = email, password = hashedPassword };

            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(email)).ReturnsAsync(player);

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() => authenticationLogic.AuthenticatePlayerAsync(email, wrongPassword));

            Assert.Equal(ServiceErrorType.InvalidPassword, exception.ErrorType);
        }

        [Fact]
        public async Task AuthenticatePlayerAsync_UserAlreadyOnline_ThrowsSessionActiveException()
        {
            string email = "test@example.com";
            string password = "Password1$";
            string hashedPassword = PasswordHasher.hashPassword(password);
            var player = new Player { idPlayer = 1, email = email, password = hashedPassword };

            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(email)).ReturnsAsync(player);
            presenceManagerMock.Setup(pm => pm.IsPlayerOnline(player.idPlayer)).Returns(true);

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() => authenticationLogic.AuthenticatePlayerAsync(email, password));

            Assert.Equal(ServiceErrorType.SessionActive, exception.ErrorType);
        }

        [Fact]
        public async Task SignOutPlayerAsync_Execution_NotifiesPresenceManager()
        {
            int playerId = 1;

            await authenticationLogic.SignOutPlayerAsync(playerId);

            presenceManagerMock.Verify(pm => pm.DisconnectUser(playerId), Times.Once);
        }

        [Fact]
        public async Task RegisterPlayerAsync_InvalidName_ThrowsInvalidNameFormatException()
        {
            var playerDto = new PlayerDto { name = "", lastName = "ValidLast", nickname = "ValidNick", password = "Password1$" };

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() => authenticationLogic.RegisterPlayerAsync(playerDto));

            Assert.Equal(ServiceErrorType.InvalidNameFormat, exception.ErrorType);
        }

        [Fact]
        public async Task RegisterPlayerAsync_WeakPassword_ThrowsInvalidPasswordFormatException()
        {
            var playerDto = new PlayerDto { name = "ValidName", lastName = "ValidLast", nickname = "ValidNick", password = "123" };

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() => authenticationLogic.RegisterPlayerAsync(playerDto));

            Assert.Equal(ServiceErrorType.InvalidPasswordFormat, exception.ErrorType);
        }

        [Fact]
        public async Task RegisterPlayerAsync_NicknameExists_ThrowsDuplicateRecordException()
        {
            var playerDto = new PlayerDto { name = "ValidName", lastName = "ValidLast", nickname = "ExistingNick", password = "Password1$" };
            playerRepositoryMock.Setup(repo => repo.DoesNicknameExistAsync(playerDto.nickname)).ReturnsAsync(true);

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() => authenticationLogic.RegisterPlayerAsync(playerDto));

            Assert.Equal(ServiceErrorType.DuplicateRecord, exception.ErrorType);
        }

        [Fact]
        public async Task RegisterPlayerAsync_TemporaryUserNotFound_ThrowsUserNotFoundException()
        {
            var playerDto = new PlayerDto { email = "new@example.com", name = "ValidName", lastName = "ValidLast", nickname = "NewNick", password = "Password1$" };
            playerRepositoryMock.Setup(repo => repo.DoesNicknameExistAsync(playerDto.nickname)).ReturnsAsync(false);
            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(playerDto.email)).ReturnsAsync((Player)null);

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() => authenticationLogic.RegisterPlayerAsync(playerDto));

            Assert.Equal(ServiceErrorType.UserNotFound, exception.ErrorType);
        }

        [Fact]
        public async Task RegisterPlayerAsync_ValidData_UpdatesPlayerName()
        {
            var playerDto = new PlayerDto { email = "test@example.com", name = "Juan", lastName = "Perez", nickname = "JuanP", password = "Password1$", pathPhoto = "img.png" };
            var existingPlayer = new Player { idPlayer = 1, email = "test@example.com", name = "OldName" };

            playerRepositoryMock.Setup(repo => repo.DoesNicknameExistAsync(playerDto.nickname)).ReturnsAsync(false);
            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(playerDto.email)).ReturnsAsync(existingPlayer);

            await authenticationLogic.RegisterPlayerAsync(playerDto);

            Assert.Equal("Juan", existingPlayer.name);
        }

        [Fact]
        public async Task RegisterPlayerAsync_ValidData_ClearsVerificationCode()
        {
            var playerDto = new PlayerDto { email = "test@example.com", name = "Juan", lastName = "Perez", nickname = "JuanP", password = "Password1$", pathPhoto = "img.png" };
            var existingPlayer = new Player { idPlayer = 1, email = "test@example.com", verificationCode = "123" };

            playerRepositoryMock.Setup(repo => repo.DoesNicknameExistAsync(playerDto.nickname)).ReturnsAsync(false);
            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(playerDto.email)).ReturnsAsync(existingPlayer);

            await authenticationLogic.RegisterPlayerAsync(playerDto);

            Assert.Null(existingPlayer.verificationCode);
        }

        [Fact]
        public async Task RegisterPlayerAsync_ValidData_SavesChanges()
        {
            var playerDto = new PlayerDto { email = "test@example.com", name = "Juan", lastName = "Perez", nickname = "JuanP", password = "Password1$", pathPhoto = "img.png" };
            var existingPlayer = new Player { idPlayer = 1, email = "test@example.com" };

            playerRepositoryMock.Setup(repo => repo.DoesNicknameExistAsync(playerDto.nickname)).ReturnsAsync(false);
            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(playerDto.email)).ReturnsAsync(existingPlayer);

            await authenticationLogic.RegisterPlayerAsync(playerDto);

            playerRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GenerateAndStoreRecoveryTokenAsync_UserNotFound_ThrowsUserNotFoundException()
        {
            string email = "unknown@example.com";
            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(email)).ReturnsAsync((Player)null);

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() => authenticationLogic.GenerateAndStoreRecoveryTokenAsync(email));

            Assert.Equal(ServiceErrorType.UserNotFound, exception.ErrorType);
        }

        [Fact]
        public async Task GenerateAndStoreRecoveryTokenAsync_UserWithoutPassword_ThrowsUserNotFoundException()
        {
            string email = "nopass@example.com";
            var player = new Player { email = email, password = null };
            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(email)).ReturnsAsync(player);

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() => authenticationLogic.GenerateAndStoreRecoveryTokenAsync(email));

            Assert.Equal(ServiceErrorType.UserNotFound, exception.ErrorType);
        }

        [Fact]
        public async Task GenerateAndStoreRecoveryTokenAsync_ValidUser_ReturnsGeneratedCode()
        {
            string email = "valid@example.com";
            var player = new Player { email = email, password = "hashed" };
            string generatedCode = "123456";

            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(email)).ReturnsAsync(player);
            emailServiceMock.Setup(s => s.GenerateVerificationCode()).Returns(generatedCode);

            string result = await authenticationLogic.GenerateAndStoreRecoveryTokenAsync(email);

            Assert.Equal(generatedCode, result);
        }

        [Fact]
        public async Task GenerateAndStoreRecoveryTokenAsync_ValidUser_UpdatesPlayerCode()
        {
            string email = "valid@example.com";
            var player = new Player { email = email, password = "hashed" };
            string generatedCode = "123456";

            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(email)).ReturnsAsync(player);
            emailServiceMock.Setup(s => s.GenerateVerificationCode()).Returns(generatedCode);

            await authenticationLogic.GenerateAndStoreRecoveryTokenAsync(email);

            Assert.Equal(generatedCode, player.verificationCode);
        }

        [Fact]
        public async Task GenerateAndStoreRecoveryTokenAsync_ValidUser_SavesChanges()
        {
            string email = "valid@example.com";
            var player = new Player { email = email, password = "hashed" };
            string generatedCode = "123456";

            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(email)).ReturnsAsync(player);
            emailServiceMock.Setup(s => s.GenerateVerificationCode()).Returns(generatedCode);

            await authenticationLogic.GenerateAndStoreRecoveryTokenAsync(email);

            playerRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task SendVerificationCodeAsync_InvalidEmail_ThrowsInvalidEmailFormatException()
        {
            string email = "invalid-email";
            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() => authenticationLogic.SendVerificationCodeAsync(email));

            Assert.Equal(ServiceErrorType.InvalidEmailFormat, exception.ErrorType);
        }

        [Fact]
        public async Task SendVerificationCodeAsync_AlreadyRegistered_ThrowsRegisteredMailException()
        {
            string email = "test@example.com";
            var player = new Player { email = email, name = "Registered User" };
            playerRepositoryMock.Setup(repo => repo.GetPlayerForVerificationAsync(email)).ReturnsAsync(player);

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() => authenticationLogic.SendVerificationCodeAsync(email));

            Assert.Equal(ServiceErrorType.RegisteredMail, exception.ErrorType);
        }

        [Fact]
        public async Task SendVerificationCodeAsync_NewUser_ReturnsGeneratedCode()
        {
            string email = "new@example.com";
            string code = "654321";
            playerRepositoryMock.Setup(repo => repo.GetPlayerForVerificationAsync(email)).ReturnsAsync((Player)null);
            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(email)).ReturnsAsync((Player)null);
            emailServiceMock.Setup(s => s.GenerateVerificationCode()).Returns(code);

            string result = await authenticationLogic.SendVerificationCodeAsync(email);

            Assert.Equal(code, result);
        }

        [Fact]
        public async Task SendVerificationCodeAsync_NewUser_AddsPlayer()
        {
            string email = "new@example.com";
            string code = "654321";
            playerRepositoryMock.Setup(repo => repo.GetPlayerForVerificationAsync(email)).ReturnsAsync((Player)null);
            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(email)).ReturnsAsync((Player)null);
            emailServiceMock.Setup(s => s.GenerateVerificationCode()).Returns(code);

            await authenticationLogic.SendVerificationCodeAsync(email);

            playerRepositoryMock.Verify(repo => repo.AddPlayer(It.IsAny<Player>()), Times.Once);
        }

        [Fact]
        public async Task SendVerificationCodeAsync_NewUser_SendsEmail()
        {
            string email = "new@example.com";
            string code = "654321";
            playerRepositoryMock.Setup(repo => repo.GetPlayerForVerificationAsync(email)).ReturnsAsync((Player)null);
            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(email)).ReturnsAsync((Player)null);
            emailServiceMock.Setup(s => s.GenerateVerificationCode()).Returns(code);

            await authenticationLogic.SendVerificationCodeAsync(email);

            emailServiceMock.Verify(s => s.SendEmailAsync(email, It.IsAny<IEmailTemplate>()), Times.Once);
        }

        [Fact]
        public async Task VerifyCodeAsync_UserNotFound_ThrowsUserNotFoundException()
        {
            string email = "test@example.com";
            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(email)).ReturnsAsync((Player)null);

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() => authenticationLogic.VerifyCodeAsync(email, "123456"));

            Assert.Equal(ServiceErrorType.UserNotFound, exception.ErrorType);
        }

        [Fact]
        public async Task VerifyCodeAsync_CodeExpired_ThrowsVerificationCodeExpiredException()
        {
            string email = "test@example.com";
            var player = new Player { email = email, verificationCode = "123456", codeExpiryDate = DateTime.UtcNow.AddMinutes(-1) };
            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(email)).ReturnsAsync(player);

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() => authenticationLogic.VerifyCodeAsync(email, "123456"));

            Assert.Equal(ServiceErrorType.VerificationCodeExpired, exception.ErrorType);
        }

        [Fact]
        public async Task VerifyCodeAsync_CodeIncorrect_ThrowsInvalidVerificationCodeException()
        {
            string email = "test@example.com";
            var player = new Player { email = email, verificationCode = "123456", codeExpiryDate = DateTime.UtcNow.AddMinutes(10) };
            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(email)).ReturnsAsync(player);

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() => authenticationLogic.VerifyCodeAsync(email, "000000"));

            Assert.Equal(ServiceErrorType.InvalidVerificationCode, exception.ErrorType);
        }

        [Fact]
        public async Task HandlePasswordResetAsync_InvalidPassword_ThrowsInvalidPasswordFormatException()
        {
            string email = "test@example.com";
            string token = "123456";
            string newPassword = "weak";

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() => authenticationLogic.HandlePasswordResetAsync(email, token, newPassword));

            Assert.Equal(ServiceErrorType.InvalidPasswordFormat, exception.ErrorType);
        }

        [Fact]
        public async Task HandlePasswordResetAsync_ValidInput_ClearsVerificationCode()
        {
            string email = "test@example.com";
            string token = "123456";
            string newPassword = "NewPassword1$";
            var player = new Player { email = email, verificationCode = token, codeExpiryDate = DateTime.UtcNow.AddMinutes(10) };

            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(email)).ReturnsAsync(player);

            await authenticationLogic.HandlePasswordResetAsync(email, token, newPassword);

            Assert.Null(player.verificationCode);
        } 

        [Fact]
        public async Task HandlePasswordResetAsync_ValidInput_SavesChanges()
        {
            string email = "test@example.com";
            string token = "123456";
            string newPassword = "NewPassword1$";
            var player = new Player { email = email, verificationCode = token, codeExpiryDate = DateTime.UtcNow.AddMinutes(10) };

            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(email)).ReturnsAsync(player);

            await authenticationLogic.HandlePasswordResetAsync(email, token, newPassword);

            playerRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteTemporaryPlayerAsync_PlayerIsTemporary_DeletesPlayer()
        {
            string email = "temp@example.com";
            var player = new Player { email = email, name = "" };
            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(email)).ReturnsAsync(player);

            await authenticationLogic.DeleteTemporaryPlayerAsync(email);

            playerRepositoryMock.Verify(repo => repo.DeletePlayerAsync(player), Times.Once);
        }

        [Fact]
        public async Task DeleteTemporaryPlayerAsync_PlayerIsRegistered_DoesNotDelete()
        {
            string email = "registered@example.com";
            var player = new Player { email = email, name = "Juan" };
            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(email)).ReturnsAsync(player);

            await authenticationLogic.DeleteTemporaryPlayerAsync(email);

            playerRepositoryMock.Verify(repo => repo.DeletePlayerAsync(It.IsAny<Player>()), Times.Never);
        }

        [Fact]
        public async Task VerifyCodeAsync_ValidCode_CompletesSuccessfully()
        {
            string email = "test@example.com";
            var player = new Player { email = email, verificationCode = "123456", codeExpiryDate = DateTime.UtcNow.AddMinutes(10) };
            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(email)).ReturnsAsync(player);

            var exception = await Record.ExceptionAsync(() => authenticationLogic.VerifyCodeAsync(email, "123456"));

            Assert.Null(exception);
        }

        [Fact]
        public async Task HandlePasswordRecoveryRequestAsync_ValidEmail_SendsRecoveryEmail()
        {
            string email = "valid@example.com";
            var player = new Player { email = email, password = "hashed" };
            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(email)).ReturnsAsync(player);
            emailServiceMock.Setup(s => s.GenerateVerificationCode()).Returns("123456");

            await authenticationLogic.HandlePasswordRecoveryRequestAsync(email);

            emailServiceMock.Verify(s => s.SendEmailAsync(email, It.IsAny<RecoveryEmailTemplate>()), Times.Once);
        }

        [Fact]
        public async Task HandleTokenValidationAsync_ValidToken_CompletesSuccessfully()
        {
            string email = "test@example.com";
            string token = "123456";
            var player = new Player { email = email, verificationCode = token, codeExpiryDate = DateTime.UtcNow.AddMinutes(10) };
            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(email)).ReturnsAsync(player);

            var exception = await Record.ExceptionAsync(() => authenticationLogic.HandleTokenValidationAsync(email, token));

            Assert.Null(exception);
        }

        [Fact]
        public async Task SendVerificationCodeAsync_ExistingTempUser_DoesNotAddPlayer()
        {
            string email = "temp@example.com";
            var tempPlayer = new Player { idPlayer = 1, email = email };
            playerRepositoryMock.Setup(repo => repo.GetPlayerForVerificationAsync(email)).ReturnsAsync((Player)null);
            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(email)).ReturnsAsync(tempPlayer);
            emailServiceMock.Setup(s => s.GenerateVerificationCode()).Returns("123456");

            await authenticationLogic.SendVerificationCodeAsync(email);

            playerRepositoryMock.Verify(repo => repo.AddPlayer(It.IsAny<Player>()), Times.Never);
        }

        [Fact]
        public async Task DeleteTemporaryPlayerAsync_UserNotFound_DoesNotDeletePlayer()
        {
            string email = "unknown@example.com";
            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(email)).ReturnsAsync((Player)null);

            await authenticationLogic.DeleteTemporaryPlayerAsync(email);

            playerRepositoryMock.Verify(repo => repo.DeletePlayerAsync(It.IsAny<Player>()), Times.Never);
        }

        [Fact]
        public async Task RegisterPlayerAsync_ValidData_HashesPassword()
        {
            var playerDto = new PlayerDto { email = "test@example.com", name = "Juan", lastName = "Perez", nickname = "JuanP", password = "Password1$", pathPhoto = "img.png" };
            var existingPlayer = new Player { idPlayer = 1, email = "test@example.com" };

            playerRepositoryMock.Setup(repo => repo.DoesNicknameExistAsync(playerDto.nickname)).ReturnsAsync(false);
            playerRepositoryMock.Setup(repo => repo.GetPlayerByEmailAsync(playerDto.email)).ReturnsAsync(existingPlayer);

            await authenticationLogic.RegisterPlayerAsync(playerDto);

            Assert.True(PasswordHasher.verifyPassword(playerDto.password, existingPlayer.password));
        }
    }
}
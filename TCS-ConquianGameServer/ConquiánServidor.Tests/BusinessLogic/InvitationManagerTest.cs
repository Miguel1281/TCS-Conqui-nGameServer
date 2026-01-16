using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using Moq;
using System;
using System.Threading.Tasks;
using ConquiánServidor.BusinessLogic.Lobby;
using Xunit;

namespace ConquiánServidor.Tests.BusinessLogic
{
    public class InvitationManagerTest
    {
        private readonly Mock<IPresenceManager> presenceManagerMock;
        private readonly InvitationManager invitationManager;

        public InvitationManagerTest()
        {
            presenceManagerMock = new Mock<IPresenceManager>();
            invitationManager = new InvitationManager(presenceManagerMock.Object);
        }

        private InvitationSenderDto CreateSender(int id = 1, string nick = "Sender")
        {
            return new InvitationSenderDto { IdPlayer = id, Nickname = nick };
        }

        [Fact]
        public async Task SendInvitationAsync_ReceiverInGame_ThrowsBusinessLogicException()
        {
            int idReceiver = 2;
            var senderDto = CreateSender();
            presenceManagerMock.Setup(p => p.IsPlayerInGame(idReceiver)).Returns(true);

            await Assert.ThrowsAsync<BusinessLogicException>(() =>
                invitationManager.SendInvitationAsync(senderDto, idReceiver, "ABCDE"));
        }

        [Fact]
        public async Task SendInvitationAsync_ReceiverInGame_ThrowsUserInGameErrorType()
        {
            int idReceiver = 2;
            var senderDto = CreateSender();
            presenceManagerMock.Setup(p => p.IsPlayerInGame(idReceiver)).Returns(true);

            try
            {
                await invitationManager.SendInvitationAsync(senderDto, idReceiver, "ABCDE");
            }
            catch (BusinessLogicException ex)
            {
                Assert.Equal(ServiceErrorType.UserInGame, ex.ErrorType);
            }
        }

        [Fact]
        public async Task SendInvitationAsync_ReceiverNotSubscribed_ThrowsBusinessLogicException()
        {
            int idReceiver = 2;
            var senderDto = CreateSender(nick: "Nick");
            presenceManagerMock.Setup(p => p.IsPlayerInGame(idReceiver)).Returns(false);

            await Assert.ThrowsAsync<BusinessLogicException>(() =>
                invitationManager.SendInvitationAsync(senderDto, idReceiver, "CODE"));
        }

        [Fact]
        public async Task SendInvitationAsync_ReceiverNotSubscribed_ThrowsOperationFailedErrorType()
        {
            int idReceiver = 2;
            var senderDto = CreateSender(nick: "Nick");
            presenceManagerMock.Setup(p => p.IsPlayerInGame(idReceiver)).Returns(false);

            try
            {
                await invitationManager.SendInvitationAsync(senderDto, idReceiver, "CODE");
            }
            catch (BusinessLogicException ex)
            {
                Assert.Equal(ServiceErrorType.UserOffline, ex.ErrorType);
            }
        }

        [Fact]
        public async Task SendInvitationAsync_ValidSubscription_CallsOnInvitationReceived()
        {
            int idReceiver = 2;
            string roomCode = "ROOM1";
            var senderDto = CreateSender();

            var callbackMock = new Mock<IInvitationCallback>();
            presenceManagerMock.Setup(p => p.IsPlayerInGame(idReceiver)).Returns(false);
            invitationManager.Subscribe(idReceiver, callbackMock.Object);

            await invitationManager.SendInvitationAsync(senderDto, idReceiver, roomCode);

            callbackMock.Verify(c => c.OnInvitationReceived(senderDto.Nickname, roomCode), Times.Once);
        }

        [Fact]
        public async Task SendInvitationAsync_CallbackFails_ThrowsBusinessLogicException()
        {
            int idReceiver = 5;
            var senderDto = CreateSender(nick: "Nick");

            var callbackMock = new Mock<IInvitationCallback>();
            callbackMock.Setup(c => c.OnInvitationReceived(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception());

            presenceManagerMock.Setup(p => p.IsPlayerInGame(idReceiver)).Returns(false);
            invitationManager.Subscribe(idReceiver, callbackMock.Object);

            await Assert.ThrowsAsync<BusinessLogicException>(() =>
                invitationManager.SendInvitationAsync(senderDto, idReceiver, "CODE"));
        }

        [Fact]
        public async Task SendInvitationAsync_CallbackFails_RemovesSubscriber()
        {
            int idReceiver = 5;
            var senderDto = CreateSender(nick: "Nick");
            var callbackMock = new Mock<IInvitationCallback>();

            callbackMock.Setup(c => c.OnInvitationReceived(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new InvalidOperationException());

            presenceManagerMock.Setup(p => p.IsPlayerInGame(idReceiver)).Returns(false);
            invitationManager.Subscribe(idReceiver, callbackMock.Object);

            await Record.ExceptionAsync(() => invitationManager.SendInvitationAsync(senderDto, idReceiver, "CODE"));

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() =>
                invitationManager.SendInvitationAsync(senderDto, idReceiver, "CODE"));

            Assert.Equal(ServiceErrorType.UserOffline, exception.ErrorType);
        }
        
        [Fact]
        public async Task SendInvitationAsync_AfterUnsubscribe_ThrowsBusinessLogicException()
        {
            int idReceiver = 10;
            var senderDto = CreateSender(nick: "Nick");

            var callbackMock = new Mock<IInvitationCallback>();
            presenceManagerMock.Setup(p => p.IsPlayerInGame(idReceiver)).Returns(false);

            invitationManager.Subscribe(idReceiver, callbackMock.Object);
            invitationManager.Unsubscribe(idReceiver);

            await Assert.ThrowsAsync<BusinessLogicException>(() =>
                invitationManager.SendInvitationAsync(senderDto, idReceiver, "CODE"));
        }

        [Fact]
        public async Task SendInvitationAsync_AfterUnsubscribe_ThrowsOperationFailedErrorType()
        {
            int idReceiver = 10;
            var senderDto = CreateSender(nick: "Nick");

            var callbackMock = new Mock<IInvitationCallback>();
            presenceManagerMock.Setup(p => p.IsPlayerInGame(idReceiver)).Returns(false);
            invitationManager.Subscribe(idReceiver, callbackMock.Object);
            invitationManager.Unsubscribe(idReceiver);

            try
            {
                await invitationManager.SendInvitationAsync(senderDto, idReceiver, "CODE");
            }
            catch (BusinessLogicException ex)
            {
                Assert.Equal(ServiceErrorType.UserOffline, ex.ErrorType);
            }
        }


        [Fact]
        public async Task SendInvitationAsync_Resubscribe_CallsNewCallback()
        {
            int idReceiver = 3;
            var senderDto = CreateSender(nick: "Nick");

            var oldCallbackMock = new Mock<IInvitationCallback>();
            var newCallbackMock = new Mock<IInvitationCallback>();
            presenceManagerMock.Setup(p => p.IsPlayerInGame(idReceiver)).Returns(false);

            invitationManager.Subscribe(idReceiver, oldCallbackMock.Object);
            invitationManager.Subscribe(idReceiver, newCallbackMock.Object);

            await invitationManager.SendInvitationAsync(senderDto, idReceiver, "CODE");

            newCallbackMock.Verify(c => c.OnInvitationReceived(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SendInvitationAsync_Resubscribe_DoesNotCallOldCallback()
        {
            int idReceiver = 3;
            var senderDto = CreateSender(nick: "Nick");

            var oldCallbackMock = new Mock<IInvitationCallback>();
            var newCallbackMock = new Mock<IInvitationCallback>();
            presenceManagerMock.Setup(p => p.IsPlayerInGame(idReceiver)).Returns(false);

            invitationManager.Subscribe(idReceiver, oldCallbackMock.Object);
            invitationManager.Subscribe(idReceiver, newCallbackMock.Object);

            await invitationManager.SendInvitationAsync(senderDto, idReceiver, "CODE");

            oldCallbackMock.Verify(c => c.OnInvitationReceived(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task SendInvitationAsync_ReceiverInLobby_ThrowsBusinessLogicException()
        {
            int idReceiver = 2;
            var senderDto = CreateSender();
            presenceManagerMock.Setup(p => p.IsPlayerInLobby(idReceiver)).Returns(true);

            await Assert.ThrowsAsync<BusinessLogicException>(() =>
                invitationManager.SendInvitationAsync(senderDto, idReceiver, "ABCDE"));
        }

        [Fact]
        public async Task SendInvitationAsync_ReceiverInLobby_ThrowsUserInLobbyErrorType()
        {
            int idReceiver = 2;
            var senderDto = CreateSender();
            presenceManagerMock.Setup(p => p.IsPlayerInLobby(idReceiver)).Returns(true);

            try
            {
                await invitationManager.SendInvitationAsync(senderDto, idReceiver, "ABCDE");
            }
            catch (BusinessLogicException ex)
            {
                Assert.Equal(ServiceErrorType.UserInLobby, ex.ErrorType);
            }
        }
    }
}
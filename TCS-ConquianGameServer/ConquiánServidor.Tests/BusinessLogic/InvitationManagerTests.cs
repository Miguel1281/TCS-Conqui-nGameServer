using ConquiánServidor.BusinessLogic;
using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ConquiánServidor.Tests.BusinessLogic
{
    public class InvitationManagerTests
    {
        private readonly Mock<IPresenceManager> presenceManagerMock;
        private readonly InvitationManager invitationManager;

        public InvitationManagerTests()
        {
            presenceManagerMock = new Mock<IPresenceManager>();
            invitationManager = new InvitationManager(presenceManagerMock.Object);
        }

        [Fact]
        public async Task SendInvitationAsync_ReceiverInGame_ThrowsBusinessLogicException()
        {
            int idSender = 1;
            int idReceiver = 2;
            string nickname = "Sender";
            string roomCode = "ABCDE";
            presenceManagerMock.Setup(p => p.IsPlayerInGame(idReceiver)).Returns(true);

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() =>
                invitationManager.SendInvitationAsync(idSender, nickname, idReceiver, roomCode));

            Assert.Equal(ServiceErrorType.UserInGame, exception.ErrorType);
        }

        [Fact]
        public async Task SendInvitationAsync_ReceiverNotSubscribed_ThrowsBusinessLogicException()
        {
            int idSender = 1;
            int idReceiver = 2;
            presenceManagerMock.Setup(p => p.IsPlayerInGame(idReceiver)).Returns(false);

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() =>
                invitationManager.SendInvitationAsync(idSender, "Nick", idReceiver, "CODE"));

            Assert.Equal(ServiceErrorType.OperationFailed, exception.ErrorType);
        }

        [Fact]
        public async Task SendInvitationAsync_ValidSubscription_CallsOnInvitationReceived()
        {
            int idReceiver = 2;
            string nickname = "Sender";
            string roomCode = "ROOM1";
            var callbackMock = new Mock<IInvitationCallback>();
            presenceManagerMock.Setup(p => p.IsPlayerInGame(idReceiver)).Returns(false);
            invitationManager.Subscribe(idReceiver, callbackMock.Object);

            await invitationManager.SendInvitationAsync(1, nickname, idReceiver, roomCode);

            callbackMock.Verify(c => c.OnInvitationReceived(nickname, roomCode), Times.Once);
        }

        [Fact]
        public async Task SendInvitationAsync_CallbackFails_RemovesSubscriberAndThrowsException()
        {
            int idReceiver = 5;
            var callbackMock = new Mock<IInvitationCallback>();
            callbackMock.Setup(c => c.OnInvitationReceived(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception());
            presenceManagerMock.Setup(p => p.IsPlayerInGame(idReceiver)).Returns(false);
            invitationManager.Subscribe(idReceiver, callbackMock.Object);

            await Assert.ThrowsAsync<BusinessLogicException>(() =>
                invitationManager.SendInvitationAsync(1, "Nick", idReceiver, "CODE"));

            await Assert.ThrowsAsync<BusinessLogicException>(() =>
                invitationManager.SendInvitationAsync(1, "Nick", idReceiver, "CODE"));
        }

        [Fact]
        public async Task SendInvitationAsync_AfterUnsubscribe_ThrowsBusinessLogicException()
        {
            int idReceiver = 10;
            var callbackMock = new Mock<IInvitationCallback>();
            presenceManagerMock.Setup(p => p.IsPlayerInGame(idReceiver)).Returns(false);
            invitationManager.Subscribe(idReceiver, callbackMock.Object);
            invitationManager.Unsubscribe(idReceiver);

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() =>
                invitationManager.SendInvitationAsync(1, "Nick", idReceiver, "CODE"));

            Assert.Equal(ServiceErrorType.OperationFailed, exception.ErrorType);
        }

        [Fact]
        public async Task SendInvitationAsync_Resubscribe_UpdatesCallbackAndWorks()
        {
            int idReceiver = 3;
            var oldCallbackMock = new Mock<IInvitationCallback>();
            var newCallbackMock = new Mock<IInvitationCallback>();
            presenceManagerMock.Setup(p => p.IsPlayerInGame(idReceiver)).Returns(false);

            invitationManager.Subscribe(idReceiver, oldCallbackMock.Object);
            invitationManager.Subscribe(idReceiver, newCallbackMock.Object);
            await invitationManager.SendInvitationAsync(1, "Nick", idReceiver, "CODE");

            newCallbackMock.Verify(c => c.OnInvitationReceived(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            oldCallbackMock.Verify(c => c.OnInvitationReceived(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}
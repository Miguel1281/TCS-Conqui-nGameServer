using Xunit;
using Moq;
using ConquiánServidor.BusinessLogic;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.Enums;
using Autofac;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ServiceModel;

namespace ConquiánServidor.Tests.BusinessLogic
{
    public class PresenceManagerTest
    {
        private readonly IContainer container;
        private readonly Mock<IFriendshipLogic> mockFriendshipLogic;
        private readonly Mock<IPresenceCallback> mockCallback;
        private readonly PresenceManager presenceManager;

        public PresenceManagerTest()
        {
            mockFriendshipLogic = new Mock<IFriendshipLogic>();
            mockCallback = new Mock<IPresenceCallback>();

            var builder = new ContainerBuilder();
            builder.RegisterInstance(mockFriendshipLogic.Object).As<IFriendshipLogic>();
            container = builder.Build();

            presenceManager = new PresenceManager(container);
        }

        [Fact]
        public void IsPlayerOnline_UserNotSubscribed_ReturnsFalse()
        {
            var result = presenceManager.IsPlayerOnline(1);

            Assert.False(result);
        }

        [Fact]
        public void Subscribe_ValidUser_StoresCallbackAndReturnsOnline()
        {
            presenceManager.Subscribe(1, mockCallback.Object);

            var result = presenceManager.IsPlayerOnline(1);

            Assert.True(result);
        }

        [Fact]
        public void Unsubscribe_ExistingUser_RemovesUser()
        {
            presenceManager.Subscribe(1, mockCallback.Object);
            presenceManager.Unsubscribe(1);

            var result = presenceManager.IsPlayerOnline(1);

            Assert.False(result);
        }

        [Fact]
        public async Task NotifyStatusChange_FriendsOnline_NotifiesFriends()
        {
            int userChangingStatus = 1;
            int friendId = 2;
            int newStatus = (int)PlayerStatus.InGame;

            var friendsList = new List<PlayerDto> { new PlayerDto { idPlayer = friendId } };
            mockFriendshipLogic.Setup(f => f.GetFriendsAsync(userChangingStatus)).ReturnsAsync(friendsList);

            var mockFriendCallback = new Mock<IPresenceCallback>();
            var mockCommObject = mockFriendCallback.As<ICommunicationObject>();
            mockCommObject.Setup(c => c.State).Returns(CommunicationState.Opened);

            presenceManager.Subscribe(friendId, mockFriendCallback.Object);

            await presenceManager.NotifyStatusChange(userChangingStatus, newStatus);

            mockFriendCallback.Verify(c => c.OnFriendStatusChanged(userChangingStatus, newStatus), Times.Once);
        }

        [Fact]
        public async Task NotifyStatusChange_FriendsOffline_DoesNotNotify()
        {
            int userChangingStatus = 1;
            int friendId = 2;
            int newStatus = (int)PlayerStatus.InGame;

            var friendsList = new List<PlayerDto> { new PlayerDto { idPlayer = friendId } };
            mockFriendshipLogic.Setup(f => f.GetFriendsAsync(userChangingStatus)).ReturnsAsync(friendsList);

            await presenceManager.NotifyStatusChange(userChangingStatus, newStatus);

            mockFriendshipLogic.Verify(f => f.GetFriendsAsync(userChangingStatus), Times.Once);
        }

        [Fact]
        public void NotifyNewFriendRequest_TargetOnline_CallsCallback()
        {
            int targetId = 1;
            presenceManager.Subscribe(targetId, mockCallback.Object);

            presenceManager.NotifyNewFriendRequest(targetId);

            mockCallback.Verify(c => c.OnFriendRequestReceived(), Times.Once);
        }

        [Fact]
        public void NotifyNewFriendRequest_TargetOffline_DoesNotThrow()
        {
            int targetId = 999;

            var exception = Record.Exception(() => presenceManager.NotifyNewFriendRequest(targetId));

            Assert.Null(exception);
        }

        [Fact]
        public void NotifyFriendListUpdate_TargetOnline_CallsCallback()
        {
            int targetId = 1;
            presenceManager.Subscribe(targetId, mockCallback.Object);

            presenceManager.NotifyFriendListUpdate(targetId);

            mockCallback.Verify(c => c.OnFriendListUpdated(), Times.Once);
        }

        [Fact]
        public async Task IsPlayerInGame_PlayerOnlineAndInGame_ReturnsTrue()
        {
            int playerId = 1;
            presenceManager.Subscribe(playerId, mockCallback.Object);

            mockFriendshipLogic.Setup(f => f.GetFriendsAsync(playerId)).ReturnsAsync(new List<PlayerDto>());

            await presenceManager.NotifyStatusChange(playerId, (int)PlayerStatus.InGame);

            var result = presenceManager.IsPlayerInGame(playerId);

            Assert.True(result);
        }

        [Fact]
        public void IsPlayerInGame_PlayerOnlineButAvailable_ReturnsFalse()
        {
            int playerId = 1;
            presenceManager.Subscribe(playerId, mockCallback.Object);

            var result = presenceManager.IsPlayerInGame(playerId);

            Assert.False(result);
        }

        [Fact]
        public void IsPlayerInGame_PlayerOffline_ReturnsFalse()
        {
            var result = presenceManager.IsPlayerInGame(999);

            Assert.False(result);
        }
    }
}
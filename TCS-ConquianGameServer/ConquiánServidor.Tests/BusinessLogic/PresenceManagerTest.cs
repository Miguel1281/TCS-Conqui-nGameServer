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
        private readonly Mock<ILobbyLogic> mockLobbyLogic;
        private readonly Mock<ILobbySessionManager> mockLobbySessionManager;
        private readonly Mock<IGameSessionManager> mockGameSessionManager;

        public PresenceManagerTest()
        {
            mockFriendshipLogic = new Mock<IFriendshipLogic>();
            mockCallback = new Mock<IPresenceCallback>();
            mockLobbyLogic = new Mock<ILobbyLogic>();
            mockLobbySessionManager = new Mock<ILobbySessionManager>();
            mockGameSessionManager = new Mock<IGameSessionManager>();

            var builder = new ContainerBuilder();
            builder.RegisterInstance(mockFriendshipLogic.Object).As<IFriendshipLogic>();
            builder.RegisterInstance(mockLobbyLogic.Object).As<ILobbyLogic>();
            builder.RegisterInstance(mockLobbySessionManager.Object).As<ILobbySessionManager>();
            builder.RegisterInstance(mockGameSessionManager.Object).As<IGameSessionManager>();

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

            var mockCommObject = mockCallback.As<ICommunicationObject>();
            mockCommObject.Setup(c => c.State).Returns(CommunicationState.Opened);

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

            var mockCommObject = mockCallback.As<ICommunicationObject>();
            mockCommObject.Setup(c => c.State).Returns(CommunicationState.Opened);

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

        [Fact]
        public async Task IsPlayerInLobby_PlayerInLobbyStatus_ReturnsTrue()
        {
            int playerId = 1;
            mockFriendshipLogic.Setup(f => f.GetFriendsAsync(playerId)).ReturnsAsync(new List<PlayerDto>());

            await presenceManager.NotifyStatusChange(playerId, (int)PlayerStatus.InLobby);

            var result = presenceManager.IsPlayerInLobby(playerId);

            Assert.True(result);
        }

        [Fact]
        public async Task IsPlayerInLobby_PlayerOnline_ReturnsFalse()
        {
            int playerId = 1;
            mockFriendshipLogic.Setup(f => f.GetFriendsAsync(playerId)).ReturnsAsync(new List<PlayerDto>());

            await presenceManager.NotifyStatusChange(playerId, (int)PlayerStatus.Online);

            var result = presenceManager.IsPlayerInLobby(playerId);

            Assert.False(result);
        }

        [Fact]
        public void DisconnectUser_UserInLobby_CallsLeaveLobby()
        {
            int playerId = 1;
            string roomCode = "CODE1";
            mockLobbySessionManager.Setup(m => m.GetLobbyCodeForPlayer(playerId)).Returns(roomCode);
            mockLobbyLogic.Setup(l => l.LeaveLobbyAsync(roomCode, playerId)).ReturnsAsync(true);
            mockFriendshipLogic.Setup(f => f.GetFriendsAsync(playerId)).ReturnsAsync(new List<PlayerDto>());

            presenceManager.DisconnectUser(playerId);

            mockLobbyLogic.Verify(l => l.LeaveLobbyAsync(roomCode, playerId), Times.Once);
        }

        [Fact]
        public void DisconnectUser_UserInGame_CallsCheckAndClearActiveSessions()
        {
            int playerId = 1;
            mockGameSessionManager.Setup(m => m.CheckAndClearActiveSessions(playerId));
            mockFriendshipLogic.Setup(f => f.GetFriendsAsync(playerId)).ReturnsAsync(new List<PlayerDto>());

            presenceManager.DisconnectUser(playerId);

            mockGameSessionManager.Verify(m => m.CheckAndClearActiveSessions(playerId), Times.Once);
        }

        [Fact]
        public void DisconnectUser_Execution_MarksUserOffline()
        {
            int playerId = 1;
            presenceManager.Subscribe(playerId, mockCallback.Object);
            mockFriendshipLogic.Setup(f => f.GetFriendsAsync(playerId)).ReturnsAsync(new List<PlayerDto>());

            presenceManager.DisconnectUser(playerId);

            Assert.False(presenceManager.IsPlayerOnline(playerId));
        }

        [Fact]
        public async Task NotifyStatusChange_CommunicationException_DoesNotThrow()
        {
            int userChangingStatus = 1;
            int friendId = 2;
            var friendsList = new List<PlayerDto> { new PlayerDto { idPlayer = friendId } };
            mockFriendshipLogic.Setup(f => f.GetFriendsAsync(userChangingStatus)).ReturnsAsync(friendsList);

            var mockFriendCallback = new Mock<IPresenceCallback>();
            var mockCommObject = mockFriendCallback.As<ICommunicationObject>();
            mockCommObject.Setup(c => c.State).Returns(CommunicationState.Opened);

            mockFriendCallback.Setup(c => c.OnFriendStatusChanged(It.IsAny<int>(), It.IsAny<int>()))
                .Throws(new CommunicationException());

            presenceManager.Subscribe(friendId, mockFriendCallback.Object);

            var exception = await Record.ExceptionAsync(() => presenceManager.NotifyStatusChange(userChangingStatus, (int)PlayerStatus.Online));

            Assert.Null(exception);
        }
    }
}
using ConquiánServidor.BusinessLogic;
using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.Enums;
using ConquiánServidor.DataAccess.Abstractions;
using DbEntity = ConquiánServidor.ConquiánDB;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ConquiánServidor.Tests.BusinessLogic
{
    public class FriendshipLogicTests
    {
        private readonly Mock<IFriendshipRepository> friendshipRepositoryMock;
        private readonly Mock<IPlayerRepository> playerRepositoryMock;
        private readonly Mock<IPresenceManager> presenceManagerMock;
        private readonly FriendshipLogic friendshipLogic;

        public FriendshipLogicTests()
        {
            friendshipRepositoryMock = new Mock<IFriendshipRepository>();
            playerRepositoryMock = new Mock<IPlayerRepository>();
            presenceManagerMock = new Mock<IPresenceManager>();
            friendshipLogic = new FriendshipLogic(
                friendshipRepositoryMock.Object,
                playerRepositoryMock.Object,
                presenceManagerMock.Object
            );
        }

        [Fact]
        public async Task GetFriendsAsync_MultipleFriends_ReturnsMappedDtosWithPresenceStatus()
        {
            int idPlayer = 1;
            var friends = new List<DbEntity.Player>
            {
                new DbEntity.Player { idPlayer = 2, nickname = "Friend1" },
                new DbEntity.Player { idPlayer = 3, nickname = "Friend2" }
            };
            friendshipRepositoryMock.Setup(r => r.GetFriendsAsync(idPlayer))
                .ReturnsAsync(friends);
            presenceManagerMock.Setup(p => p.IsPlayerOnline(2)).Returns(true);
            presenceManagerMock.Setup(p => p.IsPlayerOnline(3)).Returns(false);

            var result = await friendshipLogic.GetFriendsAsync(idPlayer);

            Assert.Equal(2, result.Count);
            Assert.Equal(PlayerStatus.Online, result.First(f => f.idPlayer == 2).Status);
            Assert.Equal(PlayerStatus.Offline, result.First(f => f.idPlayer == 3).Status);
        }

        [Fact]
        public async Task GetPlayerByNicknameAsync_PlayerIsRequester_ThrowsBusinessLogicException()
        {
            string nickname = "Self";
            int idPlayer = 1;
            playerRepositoryMock.Setup(r => r.GetPlayerByNicknameAsync(nickname))
                .ReturnsAsync(new DbEntity.Player { idPlayer = idPlayer, nickname = nickname });

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() =>
                friendshipLogic.GetPlayerByNicknameAsync(nickname, idPlayer));

            Assert.Equal(ServiceErrorType.UserNotFound, exception.ErrorType);
        }

        [Fact]
        public async Task SendFriendRequestAsync_ExistingRelationship_ThrowsBusinessLogicException()
        {
            int idPlayer = 1;
            int idFriend = 2;
            friendshipRepositoryMock.Setup(r => r.GetExistingRelationshipAsync(idPlayer, idFriend))
                .ReturnsAsync(new DbEntity.Friendship());

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() =>
                friendshipLogic.SendFriendRequestAsync(idPlayer, idFriend));

            Assert.Equal(ServiceErrorType.ExistingRequest, exception.ErrorType);
        }

        [Fact]
        public async Task SendFriendRequestAsync_ValidRequest_CallsAddAndNotify()
        {
            int idPlayer = 1;
            int idFriend = 2;
            friendshipRepositoryMock.Setup(r => r.GetExistingRelationshipAsync(idPlayer, idFriend))
                .ReturnsAsync((DbEntity.Friendship)null);

            await friendshipLogic.SendFriendRequestAsync(idPlayer, idFriend);

            friendshipRepositoryMock.Verify(r => r.AddFriendship(It.IsAny<DbEntity.Friendship>()), Times.Once);
            friendshipRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
            presenceManagerMock.Verify(p => p.NotifyNewFriendRequest(idFriend), Times.Once);
        }

        [Fact]
        public async Task UpdateFriendRequestStatusAsync_RequestNotFound_ReturnsWithoutAction()
        {
            int idFriendship = 1;
            friendshipRepositoryMock.Setup(r => r.GetPendingRequestByIdAsync(idFriendship))
                .ReturnsAsync((DbEntity.Friendship)null);

            await friendshipLogic.UpdateFriendRequestStatusAsync(idFriendship, (int)FriendshipStatus.Accepted);

            friendshipRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateFriendRequestStatusAsync_AcceptedStatus_UpdatesRelationshipAndNotifies()
        {
            int idFriendship = 1;
            var request = new DbEntity.Friendship { idFriendship = idFriendship, idOrigen = 1, idDestino = 2 };
            friendshipRepositoryMock.Setup(r => r.GetPendingRequestByIdAsync(idFriendship))
                .ReturnsAsync(request);
            friendshipRepositoryMock.Setup(r => r.GetAcceptedFriendshipAsync(1, 2))
                .ReturnsAsync((DbEntity.Friendship)null);

            await friendshipLogic.UpdateFriendRequestStatusAsync(idFriendship, (int)FriendshipStatus.Accepted);

            Assert.Equal((int)FriendshipStatus.Accepted, request.idStatus);
            friendshipRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
            presenceManagerMock.Verify(p => p.NotifyFriendListUpdate(It.IsAny<int>()), Times.Exactly(2));
        }

        [Fact]
        public async Task UpdateFriendRequestStatusAsync_DeclinedStatus_RemovesRelationship()
        {
            int idFriendship = 1;
            var request = new DbEntity.Friendship { idFriendship = idFriendship, idOrigen = 1, idDestino = 2 };
            friendshipRepositoryMock.Setup(r => r.GetPendingRequestByIdAsync(idFriendship))
                .ReturnsAsync(request);

            await friendshipLogic.UpdateFriendRequestStatusAsync(idFriendship, (int)FriendshipStatus.Pending);

            friendshipRepositoryMock.Verify(r => r.RemoveFriendship(request), Times.Once);
            friendshipRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteFriendAsync_FriendshipExists_RemovesAndNotifies()
        {
            int idPlayer = 1;
            int idFriend = 2;
            var friendship = new DbEntity.Friendship();
            friendshipRepositoryMock.Setup(r => r.GetAcceptedFriendshipAsync(idPlayer, idFriend))
                .ReturnsAsync(friendship);

            await friendshipLogic.DeleteFriendAsync(idPlayer, idFriend);

            friendshipRepositoryMock.Verify(r => r.RemoveFriendship(friendship), Times.Once);
            friendshipRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
            presenceManagerMock.Verify(p => p.NotifyFriendListUpdate(idPlayer), Times.Once);
            presenceManagerMock.Verify(p => p.NotifyFriendListUpdate(idFriend), Times.Once);
        }

        [Fact]
        public async Task DeleteFriendAsync_FriendshipDoesNotExist_ReturnsSuccessWithoutAction()
        {
            friendshipRepositoryMock.Setup(r => r.GetAcceptedFriendshipAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((DbEntity.Friendship)null);

            await friendshipLogic.DeleteFriendAsync(1, 2);

            friendshipRepositoryMock.Verify(r => r.RemoveFriendship(It.IsAny<DbEntity.Friendship>()), Times.Never);
            friendshipRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }
    }
}
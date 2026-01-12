using ConquiánServidor.BusinessLogic;
using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.ConquiánDB.Abstractions;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.Enums;
using ConquiánServidor.DataAccess.Abstractions;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace ConquiánServidor.Tests.BusinessLogic
{
    public class FriendshipLogicTest
    {
        private readonly Mock<IFriendshipRepository> friendshipRepositoryMock;
        private readonly Mock<IPlayerRepository> playerRepositoryMock;
        private readonly Mock<IPresenceManager> presenceManagerMock;
        private readonly FriendshipLogic friendshipLogic;

        public FriendshipLogicTest()
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
        public async Task GetFriendsAsync_FriendsFound_ReturnsCorrectCount()
        {
            int playerId = 1;
            var friendsList = new List<Player>
            {
                new Player { idPlayer = 2, nickname = "Friend1" },
                new Player { idPlayer = 3, nickname = "Friend2" }
            };

            friendshipRepositoryMock.Setup(r => r.GetFriendsAsync(playerId)).ReturnsAsync(friendsList);

            var result = await friendshipLogic.GetFriendsAsync(playerId);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetFriendsAsync_FriendsFound_MapsStatusCorrectly()
        {
            int playerId = 1;
            var friendsList = new List<Player>
            {
                new Player { idPlayer = 2, nickname = "Friend1" }
            };

            friendshipRepositoryMock.Setup(r => r.GetFriendsAsync(playerId)).ReturnsAsync(friendsList);
            presenceManagerMock.Setup(pm => pm.IsPlayerInGame(2)).Returns(true);

            var result = await friendshipLogic.GetFriendsAsync(playerId);

            Assert.Equal(PlayerStatus.InGame, result[0].Status);
        }

        [Fact]
        public async Task GetFriendsAsync_NoFriends_ReturnsEmptyList()
        {
            int playerId = 1;
            friendshipRepositoryMock.Setup(r => r.GetFriendsAsync(playerId)).ReturnsAsync(new List<Player>());

            var result = await friendshipLogic.GetFriendsAsync(playerId);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetFriendRequestsAsync_RequestsFound_ReturnsCorrectCount()
        {
            int playerId = 1;
            var requests = new List<Friendship>
            {
                new Friendship { idFriendship = 10, Player1 = new Player { nickname = "Requester1" } }
            };

            friendshipRepositoryMock.Setup(r => r.GetFriendRequestsAsync(playerId)).ReturnsAsync(requests);

            var result = await friendshipLogic.GetFriendRequestsAsync(playerId);

            Assert.Single(result);
        }

        [Fact]
        public async Task GetFriendRequestsAsync_RequestsFound_MapsIdCorrectly()
        {
            int playerId = 1;
            var requests = new List<Friendship>
            {
                new Friendship { idFriendship = 10, Player1 = new Player { nickname = "Requester1" } }
            };

            friendshipRepositoryMock.Setup(r => r.GetFriendRequestsAsync(playerId)).ReturnsAsync(requests);

            var result = await friendshipLogic.GetFriendRequestsAsync(playerId);

            Assert.Equal(10, result[0].IdFriendship);
        }

        [Fact]
        public async Task GetPlayerByNicknameAsync_PlayerFound_ReturnsNotNull()
        {
            string nickname = "Target";
            int requesterId = 1;
            var foundPlayer = new Player { idPlayer = 2, nickname = nickname };

            playerRepositoryMock.Setup(r => r.GetPlayerByNicknameAsync(nickname)).ReturnsAsync(foundPlayer);

            var result = await friendshipLogic.GetPlayerByNicknameAsync(nickname, requesterId);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetPlayerByNicknameAsync_PlayerFound_ReturnsCorrectId()
        {
            string nickname = "Target";
            int requesterId = 1;
            var foundPlayer = new Player { idPlayer = 2, nickname = nickname };

            playerRepositoryMock.Setup(r => r.GetPlayerByNicknameAsync(nickname)).ReturnsAsync(foundPlayer);

            var result = await friendshipLogic.GetPlayerByNicknameAsync(nickname, requesterId);

            Assert.Equal(foundPlayer.idPlayer, result.idPlayer);
        }

        [Fact]
        public async Task GetPlayerByNicknameAsync_PlayerNotFound_ThrowsUserNotFoundException()
        {
            string nickname = "NonExistent";
            int requesterId = 1;
            playerRepositoryMock.Setup(r => r.GetPlayerByNicknameAsync(nickname)).ReturnsAsync((Player)null);

            await Assert.ThrowsAsync<BusinessLogicException>(() => friendshipLogic.GetPlayerByNicknameAsync(nickname, requesterId));
        }

        [Fact]
        public async Task GetPlayerByNicknameAsync_PlayerNotFound_ThrowsUserNotFoundExceptionType()
        {
            string nickname = "NonExistent";
            int requesterId = 1;
            playerRepositoryMock.Setup(r => r.GetPlayerByNicknameAsync(nickname)).ReturnsAsync((Player)null);

            try
            {
                await friendshipLogic.GetPlayerByNicknameAsync(nickname, requesterId);
            }
            catch (BusinessLogicException ex)
            {
                Assert.Equal(ServiceErrorType.UserNotFound, ex.ErrorType);
            }
        }

        [Fact]
        public async Task GetPlayerByNicknameAsync_PlayerIsSelf_ThrowsUserNotFoundException()
        {
            string nickname = "MyNick";
            int requesterId = 1;
            var selfPlayer = new Player { idPlayer = requesterId, nickname = nickname };
            playerRepositoryMock.Setup(r => r.GetPlayerByNicknameAsync(nickname)).ReturnsAsync(selfPlayer);

            await Assert.ThrowsAsync<BusinessLogicException>(() => friendshipLogic.GetPlayerByNicknameAsync(nickname, requesterId));
        }

        [Fact]
        public async Task SendFriendRequestAsync_NoExistingRelation_AddsRequest()
        {
            int playerId = 1;
            int friendId = 2;
            friendshipRepositoryMock.Setup(r => r.GetExistingRelationshipAsync(playerId, friendId)).ReturnsAsync((Friendship)null);

            await friendshipLogic.SendFriendRequestAsync(playerId, friendId);

            friendshipRepositoryMock.Verify(r => r.AddFriendship(It.IsAny<Friendship>()), Times.Once);
        }

        [Fact]
        public async Task SendFriendRequestAsync_NoExistingRelation_SavesChanges()
        {
            int playerId = 1;
            int friendId = 2;
            friendshipRepositoryMock.Setup(r => r.GetExistingRelationshipAsync(playerId, friendId)).ReturnsAsync((Friendship)null);

            await friendshipLogic.SendFriendRequestAsync(playerId, friendId);

            friendshipRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task SendFriendRequestAsync_NoExistingRelation_NotifiesUser()
        {
            int playerId = 1;
            int friendId = 2;
            friendshipRepositoryMock.Setup(r => r.GetExistingRelationshipAsync(playerId, friendId)).ReturnsAsync((Friendship)null);

            await friendshipLogic.SendFriendRequestAsync(playerId, friendId);

            presenceManagerMock.Verify(pm => pm.NotifyNewFriendRequest(friendId), Times.Once);
        }

        [Fact]
        public async Task SendFriendRequestAsync_ExistingRelation_ThrowsExistingRequestException()
        {
            int playerId = 1;
            int friendId = 2;
            friendshipRepositoryMock.Setup(r => r.GetExistingRelationshipAsync(playerId, friendId)).ReturnsAsync(new Friendship());

            await Assert.ThrowsAsync<BusinessLogicException>(() => friendshipLogic.SendFriendRequestAsync(playerId, friendId));
        }

        [Fact]
        public async Task SendFriendRequestAsync_ExistingRelation_ThrowsExistingRequestExceptionType()
        {
            int playerId = 1;
            int friendId = 2;
            friendshipRepositoryMock.Setup(r => r.GetExistingRelationshipAsync(playerId, friendId)).ReturnsAsync(new Friendship());

            try
            {
                await friendshipLogic.SendFriendRequestAsync(playerId, friendId);
            }
            catch (BusinessLogicException ex)
            {
                Assert.Equal(ServiceErrorType.ExistingRequest, ex.ErrorType);
            }
        }

        [Fact]
        public async Task UpdateFriendRequestStatusAsync_RequestNotFound_DoesNotSaveChanges()
        {
            int friendshipId = 99;
            friendshipRepositoryMock.Setup(r => r.GetPendingRequestByIdAsync(friendshipId)).ReturnsAsync((Friendship)null);

            await friendshipLogic.UpdateFriendRequestStatusAsync(friendshipId, (int)FriendshipStatus.Accepted);

            friendshipRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateFriendRequestStatusAsync_AcceptRequest_UpdatesStatus()
        {
            int friendshipId = 10;
            var request = new Friendship { idFriendship = friendshipId, idStatus = (int)FriendshipStatus.Pending };
            friendshipRepositoryMock.Setup(r => r.GetPendingRequestByIdAsync(friendshipId)).ReturnsAsync(request);

            await friendshipLogic.UpdateFriendRequestStatusAsync(friendshipId, (int)FriendshipStatus.Accepted);

            Assert.Equal((int)FriendshipStatus.Accepted, request.idStatus);
        }

        [Fact]
        public async Task UpdateFriendRequestStatusAsync_AcceptRequest_SavesChanges()
        {
            int friendshipId = 10;
            var request = new Friendship { idFriendship = friendshipId, idStatus = (int)FriendshipStatus.Pending };
            friendshipRepositoryMock.Setup(r => r.GetPendingRequestByIdAsync(friendshipId)).ReturnsAsync(request);

            await friendshipLogic.UpdateFriendRequestStatusAsync(friendshipId, (int)FriendshipStatus.Accepted);

            friendshipRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateFriendRequestStatusAsync_AcceptRequest_NotifiesSender()
        {
            int friendshipId = 10;
            int senderId = 1;
            var request = new Friendship { idFriendship = friendshipId, idOrigen = senderId, idStatus = (int)FriendshipStatus.Pending };
            friendshipRepositoryMock.Setup(r => r.GetPendingRequestByIdAsync(friendshipId)).ReturnsAsync(request);

            await friendshipLogic.UpdateFriendRequestStatusAsync(friendshipId, (int)FriendshipStatus.Accepted);

            presenceManagerMock.Verify(pm => pm.NotifyFriendListUpdate(senderId), Times.Once);
        }

        [Fact]
        public async Task UpdateFriendRequestStatusAsync_AcceptDuplicateRequest_RemovesCurrentRequest()
        {
            int friendshipId = 10;
            int senderId = 1;
            int receiverId = 2;
            var request = new Friendship { idFriendship = friendshipId, idOrigen = senderId, idDestino = receiverId };
            var existingFriendship = new Friendship();

            friendshipRepositoryMock.Setup(r => r.GetPendingRequestByIdAsync(friendshipId)).ReturnsAsync(request);
            friendshipRepositoryMock.Setup(r => r.GetAcceptedFriendshipAsync(senderId, receiverId)).ReturnsAsync(existingFriendship);

            await friendshipLogic.UpdateFriendRequestStatusAsync(friendshipId, (int)FriendshipStatus.Accepted);

            friendshipRepositoryMock.Verify(r => r.RemoveFriendship(request), Times.Once);
        }

        [Fact]
        public async Task UpdateFriendRequestStatusAsync_RejectRequest_RemovesRequest()
        {
            int friendshipId = 10;
            var request = new Friendship { idFriendship = friendshipId };
            friendshipRepositoryMock.Setup(r => r.GetPendingRequestByIdAsync(friendshipId)).ReturnsAsync(request);

            await friendshipLogic.UpdateFriendRequestStatusAsync(friendshipId, (int)FriendshipStatus.Pending);

            friendshipRepositoryMock.Verify(r => r.RemoveFriendship(request), Times.Once);
        }

        [Fact]
        public async Task DeleteFriendAsync_FriendshipExists_RemovesFriendship()
        {
            int playerId = 1;
            int friendId = 2;
            var friendship = new Friendship();
            friendshipRepositoryMock.Setup(r => r.GetAcceptedFriendshipAsync(playerId, friendId)).ReturnsAsync(friendship);

            await friendshipLogic.DeleteFriendAsync(playerId, friendId);

            friendshipRepositoryMock.Verify(r => r.RemoveFriendship(friendship), Times.Once);
        }

        [Fact]
        public async Task DeleteFriendAsync_FriendshipExists_SavesChanges()
        {
            int playerId = 1;
            int friendId = 2;
            var friendship = new Friendship();
            friendshipRepositoryMock.Setup(r => r.GetAcceptedFriendshipAsync(playerId, friendId)).ReturnsAsync(friendship);

            await friendshipLogic.DeleteFriendAsync(playerId, friendId);

            friendshipRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteFriendAsync_FriendshipExists_NotifiesUser()
        {
            int playerId = 1;
            int friendId = 2;
            var friendship = new Friendship();
            friendshipRepositoryMock.Setup(r => r.GetAcceptedFriendshipAsync(playerId, friendId)).ReturnsAsync(friendship);

            await friendshipLogic.DeleteFriendAsync(playerId, friendId);

            presenceManagerMock.Verify(pm => pm.NotifyFriendListUpdate(playerId), Times.Once);
        }

        [Fact]
        public async Task DeleteFriendAsync_FriendshipNotFound_DoesNotRemove()
        {
            int playerId = 1;
            int friendId = 2;
            friendshipRepositoryMock.Setup(r => r.GetAcceptedFriendshipAsync(playerId, friendId)).ReturnsAsync((Friendship)null);

            await friendshipLogic.DeleteFriendAsync(playerId, friendId);

            friendshipRepositoryMock.Verify(r => r.RemoveFriendship(It.IsAny<Friendship>()), Times.Never);
        }
    }
}
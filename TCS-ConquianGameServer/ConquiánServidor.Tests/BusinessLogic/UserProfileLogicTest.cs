using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.Enums;
using ConquiánServidor.ConquiánDB.Abstractions;
using DbEntity = ConquiánServidor.ConquiánDB;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using ConquiánServidor.BusinessLogic.UserProfile;
using Xunit;

namespace ConquiánServidor.Tests.BusinessLogic
{
    public class UserProfileLogicTests
    {
        private readonly Mock<IPlayerRepository> playerRepositoryMock;
        private readonly Mock<ISocialRepository> socialRepositoryMock;
        private readonly Mock<IPresenceManager> presenceManagerMock;
        private readonly UserProfileLogic userProfileLogic;

        public UserProfileLogicTests()
        {
            playerRepositoryMock = new Mock<IPlayerRepository>();
            socialRepositoryMock = new Mock<ISocialRepository>();
            presenceManagerMock = new Mock<IPresenceManager>();
            userProfileLogic = new UserProfileLogic(
                playerRepositoryMock.Object,
                socialRepositoryMock.Object,
                presenceManagerMock.Object
            );
        }


        [Fact]
        public async Task GetPlayerByIdAsync_PlayerDoesNotExist_ThrowsBusinessLogicException()
        {
            int idPlayer = 1;
            playerRepositoryMock.Setup(r => r.GetPlayerByIdAsync(idPlayer))
                .ReturnsAsync((DbEntity.Player)null);

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() =>
                userProfileLogic.GetPlayerByIdAsync(idPlayer));

            Assert.Equal(ServiceErrorType.UserNotFound, exception.ErrorType);
        }

        [Fact]
        public async Task GetPlayerByIdAsync_PlayerExistsOnline_StatusIsOnline()
        {
            int idPlayer = 2;
            var dbPlayer = new DbEntity.Player
            {
                idPlayer = idPlayer,
                nickname = "Tester",
                LevelRules = new DbEntity.LevelRules { RankName = "Legend" }
            };
            playerRepositoryMock.Setup(r => r.GetPlayerByIdAsync(idPlayer))
                .ReturnsAsync(dbPlayer);
            playerRepositoryMock.Setup(r => r.GetNextLevelThresholdAsync(It.IsAny<int>()))
                .ReturnsAsync(100);
            presenceManagerMock.Setup(p => p.IsPlayerOnline(idPlayer))
                .Returns(true);

            var result = await userProfileLogic.GetPlayerByIdAsync(idPlayer);

            Assert.Equal(PlayerStatus.Online, result.Status);
        }

        [Fact]
        public async Task GetPlayerByIdAsync_PlayerExistsOffline_StatusIsOffline()
        {
            int idPlayer = 3;
            var dbPlayer = new DbEntity.Player
            {
                idPlayer = idPlayer,
                nickname = "TesterOffline",
                LevelRules = new DbEntity.LevelRules { RankName = "Bronze" }
            };
            playerRepositoryMock.Setup(r => r.GetPlayerByIdAsync(idPlayer))
                .ReturnsAsync(dbPlayer);
            playerRepositoryMock.Setup(r => r.GetNextLevelThresholdAsync(It.IsAny<int>()))
                .ReturnsAsync(200);
            presenceManagerMock.Setup(p => p.IsPlayerOnline(idPlayer))
                .Returns(false);

            var result = await userProfileLogic.GetPlayerByIdAsync(idPlayer);

            Assert.Equal(PlayerStatus.Offline, result.Status);
        }


        [Fact]
        public async Task GetPlayerSocialsAsync_PlayerNotFound_ThrowsBusinessLogicException()
        {
            int idPlayer = 99;
            playerRepositoryMock.Setup(r => r.GetPlayerByIdAsync(idPlayer))
                .ReturnsAsync((DbEntity.Player)null);

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() =>
                userProfileLogic.GetPlayerSocialsAsync(idPlayer));

            Assert.Equal(ServiceErrorType.UserNotFound, exception.ErrorType);
        }

        [Fact]
        public async Task GetPlayerSocialsAsync_PlayerFound_ReturnsCount()
        {
            int idPlayer = 5;
            var dbPlayer = new DbEntity.Player { idPlayer = idPlayer };
            var socials = new List<DbEntity.Social>
            {
                new DbEntity.Social { userLink = "http://twitter.com/user1", idSocialType = 1, idPlayer = idPlayer },
                new DbEntity.Social { userLink = "http://facebook.com/user1", idSocialType = 2, idPlayer = idPlayer }
            };

            playerRepositoryMock.Setup(r => r.GetPlayerByIdAsync(idPlayer))
                .ReturnsAsync(dbPlayer);
            socialRepositoryMock.Setup(r => r.GetSocialsByPlayerIdAsync(idPlayer))
                .ReturnsAsync(socials);

            var result = await userProfileLogic.GetPlayerSocialsAsync(idPlayer);

            Assert.Equal(2, result.Count);
        }


        [Fact]
        public async Task UpdatePlayerAsync_NullDto_ThrowsBusinessLogicException()
        {
            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() =>
                userProfileLogic.UpdatePlayerAsync(null));

            Assert.Equal(ServiceErrorType.ValidationFailed, exception.ErrorType);
        }

        [Fact]
        public async Task UpdatePlayerAsync_PlayerToUpdateNotFound_ThrowsBusinessLogicException()
        {
            var playerDto = new PlayerDto { idPlayer = 1 };
            playerRepositoryMock.Setup(r => r.GetPlayerByIdAsync(playerDto.idPlayer))
                .ReturnsAsync((DbEntity.Player)null);

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() =>
                userProfileLogic.UpdatePlayerAsync(playerDto));

            Assert.Equal(ServiceErrorType.UserNotFound, exception.ErrorType);
        }

        [Fact]
        public async Task UpdatePlayerAsync_ValidData_SaveChangesCalledOnce()
        {
            var playerDto = new PlayerDto { idPlayer = 1, name = "NewName", nickname = "NewNick" };
            var dbPlayer = new DbEntity.Player { idPlayer = 1 };
            playerRepositoryMock.Setup(r => r.GetPlayerByIdAsync(playerDto.idPlayer))
                .ReturnsAsync(dbPlayer);

            await userProfileLogic.UpdatePlayerAsync(playerDto);

            playerRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }


        [Fact]
        public async Task UpdatePlayerSocialsAsync_PlayerDoesNotExist_ThrowsBusinessLogicException()
        {
            int idPlayer = 1;
            socialRepositoryMock.Setup(r => r.DoesPlayerExistAsync(idPlayer))
                .ReturnsAsync(false);

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() =>
                userProfileLogic.UpdatePlayerSocialsAsync(idPlayer, new List<SocialDto>()));

            Assert.Equal(ServiceErrorType.UserNotFound, exception.ErrorType);
        }

        [Fact]
        public async Task UpdatePlayerSocialsAsync_ValidList_SaveChangesCalledOnce()
        {
            int idPlayer = 1;
            var socials = new List<SocialDto> { new SocialDto { IdSocialType = 1, UserLink = "link" } };
            var existingSocials = new List<DbEntity.Social>();
            socialRepositoryMock.Setup(r => r.DoesPlayerExistAsync(idPlayer))
                .ReturnsAsync(true);
            socialRepositoryMock.Setup(r => r.GetSocialsByPlayerIdAsync(idPlayer))
                .ReturnsAsync(existingSocials);

            await userProfileLogic.UpdatePlayerSocialsAsync(idPlayer, socials);

            socialRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }


        [Fact]
        public async Task UpdateProfilePictureAsync_PlayerNotFound_ThrowsBusinessLogicException()
        {
            int idPlayer = 10;
            playerRepositoryMock.Setup(r => r.GetPlayerByIdAsync(idPlayer))
                .ReturnsAsync((DbEntity.Player)null);

            var ex = await Assert.ThrowsAsync<BusinessLogicException>(() =>
                userProfileLogic.UpdateProfilePictureAsync(idPlayer, "C:\\images\\pic.png"));

            Assert.Equal(ServiceErrorType.UserNotFound, ex.ErrorType);
        }


        [Fact]
        public async Task UpdateProfilePictureAsync_ValidPath_SaveChangesCalledOnce()
        {
            int idPlayer = 12;
            var dbPlayer = new DbEntity.Player { idPlayer = idPlayer, pathPhoto = "oldpath.png" };
            playerRepositoryMock.Setup(r => r.GetPlayerByIdAsync(idPlayer))
                .ReturnsAsync(dbPlayer);

            await userProfileLogic.UpdateProfilePictureAsync(idPlayer, "newpath.png");

            playerRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

       

        [Fact]
        public async Task GetPlayerGameHistoryAsync_PlayerHasGames_ReturnsSingleItem()
        {
            int idPlayer = 1;
            var games = new List<DbEntity.Game>
            {
                new DbEntity.Game
                {
                    gameTime = 120,
                    Gamemode = new DbEntity.Gamemode { gamemode1 = "Ranked" },
                    GamePlayer = new List<DbEntity.GamePlayer>
                    {
                        new DbEntity.GamePlayer { idPlayer = idPlayer, isWinner = true, score = 50, Player = new DbEntity.Player { nickname = "Me" } },
                        new DbEntity.GamePlayer { idPlayer = 2, isWinner = false, score = 10, Player = new DbEntity.Player { nickname = "Rival" } }
                    }
                }
            };

            playerRepositoryMock.Setup(r => r.GetPlayerGamesAsync(idPlayer))
                .ReturnsAsync(games);

            var result = await userProfileLogic.GetPlayerGameHistoryAsync(idPlayer);

            Assert.Single(result);
        }

        [Fact]
        public async Task GetPlayerGameHistoryAsync_PlayerHasNoGames_ReturnsEmptyList()
        {
            int idPlayer = 3;
            playerRepositoryMock.Setup(r => r.GetPlayerGamesAsync(idPlayer))
                .ReturnsAsync(new List<DbEntity.Game>());

            var result = await userProfileLogic.GetPlayerGameHistoryAsync(idPlayer);

            Assert.Empty(result);
        }
    }
}
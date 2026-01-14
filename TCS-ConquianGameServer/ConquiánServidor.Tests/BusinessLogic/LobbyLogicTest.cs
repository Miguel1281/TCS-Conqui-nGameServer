using Xunit;
using Moq;
using ConquiánServidor.BusinessLogic;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.Enums;
using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.ConquiánDB;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using static ConquiánServidor.BusinessLogic.GuestInvitationManager;

namespace ConquiánServidor.Tests.BusinessLogic
{
    public class LobbyLogicTest
    {
        private readonly Mock<ILobbyRepository> lobbyRepositoryMock;
        private readonly Mock<IPlayerRepository> playerRepositoryMock;
        private readonly Mock<ILobbySessionManager> sessionManagerMock;
        private readonly Mock<IGameSessionManager> gameSessionManagerMock;
        private readonly Mock<IGuestInvitationManager> guestInvitationManagerMock;
        private readonly LobbyLogic lobbyLogic;

        public LobbyLogicTest()
        {
            lobbyRepositoryMock = new Mock<ILobbyRepository>();
            playerRepositoryMock = new Mock<IPlayerRepository>();
            sessionManagerMock = new Mock<ILobbySessionManager>();
            gameSessionManagerMock = new Mock<IGameSessionManager>();
            guestInvitationManagerMock = new Mock<IGuestInvitationManager>();

            lobbyLogic = new LobbyLogic(
                lobbyRepositoryMock.Object,
                playerRepositoryMock.Object,
                sessionManagerMock.Object,
                gameSessionManagerMock.Object,
                guestInvitationManagerMock.Object
            );
        }

        [Fact]
        public async Task GetLobbyStateAsync_SessionNotFound_ThrowsBusinessLogicException()
        {
            sessionManagerMock.Setup(m => m.GetLobbySession(It.IsAny<string>()))
                .Returns((LobbySession)null);

            await Assert.ThrowsAsync<BusinessLogicException>(() => lobbyLogic.GetLobbyStateAsync("CODE1"));
        }

        [Fact]
        public async Task GetLobbyStateAsync_DbRecordNotFound_ThrowsBusinessLogicException()
        {
            var session = new LobbySession
            {
                RoomCode = "CODE1",
                Players = new List<PlayerDto> { new PlayerDto() }
            };

            sessionManagerMock.Setup(m => m.GetLobbySession(It.IsAny<string>()))
                .Returns(session);
            lobbyRepositoryMock.Setup(r => r.GetLobbyByRoomCodeAsync(It.IsAny<string>()))
                .ReturnsAsync((Lobby)null);

            await Assert.ThrowsAsync<BusinessLogicException>(() => lobbyLogic.GetLobbyStateAsync("CODE1"));
        }

        [Fact]
        public async Task GetLobbyStateAsync_Success_ReturnsCorrectRoomCode()
        {
            var session = new LobbySession
            {
                RoomCode = "CODE1",
                Players = new List<PlayerDto> { new PlayerDto() },
                IdGamemode = 1
            };

            var lobby = new Lobby { roomCode = "CODE1", idHostPlayer = 1, StatusLobby = new StatusLobby { statusName = "Waiting" } };

            sessionManagerMock.Setup(m => m.GetLobbySession("CODE1")).Returns(session);
            lobbyRepositoryMock.Setup(r => r.GetLobbyByRoomCodeAsync("CODE1")).ReturnsAsync(lobby);

            var result = await lobbyLogic.GetLobbyStateAsync("CODE1");

            Assert.Equal("CODE1", result.RoomCode);
        }

        [Fact]
        public async Task CreateLobbyAsync_HostNotFound_ThrowsBusinessLogicException()
        {
            playerRepositoryMock.Setup(r => r.GetPlayerByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Player)null);

            await Assert.ThrowsAsync<BusinessLogicException>(() => lobbyLogic.CreateLobbyAsync(1));
        }

        [Fact]
        public async Task CreateLobbyAsync_Success_ReturnsRoomCodeOfLengthFive()
        {
            var player = new Player { idPlayer = 1, nickname = "Host" };
            playerRepositoryMock.Setup(r => r.GetPlayerByIdAsync(1)).ReturnsAsync(player);
            lobbyRepositoryMock.Setup(r => r.DoesRoomCodeExistAsync(It.IsAny<string>())).ReturnsAsync(false);

            var result = await lobbyLogic.CreateLobbyAsync(1);

            Assert.Equal(5, result.Length);
        }

        [Fact]
        public async Task CreateLobbyAsync_Success_AddsLobbyToRepository()
        {
            var player = new Player { idPlayer = 1, nickname = "Host" };
            playerRepositoryMock.Setup(r => r.GetPlayerByIdAsync(1)).ReturnsAsync(player);
            lobbyRepositoryMock.Setup(r => r.DoesRoomCodeExistAsync(It.IsAny<string>())).ReturnsAsync(false);

            await lobbyLogic.CreateLobbyAsync(1);

            lobbyRepositoryMock.Verify(r => r.AddLobby(It.IsAny<Lobby>()), Times.Once);
        }

        [Fact]
        public async Task JoinLobbyAsync_LobbyNotFound_ThrowsBusinessLogicException()
        {
            lobbyRepositoryMock.Setup(r => r.GetLobbyByRoomCodeAsync("CODE1")).ReturnsAsync((Lobby)null);

            await Assert.ThrowsAsync<BusinessLogicException>(() => lobbyLogic.JoinLobbyAsync("CODE1", 2));
        }

        [Fact]
        public async Task JoinLobbyAsync_LobbyNotWaiting_ThrowsBusinessLogicException()
        {
            var lobby = new Lobby { idStatusLobby = (int)LobbyStatus.InGame };
            lobbyRepositoryMock.Setup(r => r.GetLobbyByRoomCodeAsync("CODE1")).ReturnsAsync(lobby);

            await Assert.ThrowsAsync<BusinessLogicException>(() => lobbyLogic.JoinLobbyAsync("CODE1", 2));
        }

        [Fact]
        public async Task JoinLobbyAsync_PlayerNotFound_ThrowsBusinessLogicException()
        {
            var lobby = new Lobby { idStatusLobby = (int)LobbyStatus.Waiting };
            lobbyRepositoryMock.Setup(r => r.GetLobbyByRoomCodeAsync("CODE1")).ReturnsAsync(lobby);
            playerRepositoryMock.Setup(r => r.GetPlayerByIdAsync(2)).ReturnsAsync((Player)null);

            await Assert.ThrowsAsync<BusinessLogicException>(() => lobbyLogic.JoinLobbyAsync("CODE1", 2));
        }

        [Fact]
        public async Task JoinLobbyAsync_PlayerBanned_ThrowsBusinessLogicException()
        {
            var lobby = new Lobby { idStatusLobby = (int)LobbyStatus.Waiting };
            var player = new Player { idPlayer = 2 };
            lobbyRepositoryMock.Setup(r => r.GetLobbyByRoomCodeAsync("CODE1")).ReturnsAsync(lobby);
            playerRepositoryMock.Setup(r => r.GetPlayerByIdAsync(2)).ReturnsAsync(player);

            sessionManagerMock.Setup(m => m.AddPlayerToLobby(It.IsAny<string>(), It.IsAny<PlayerDto>()))
                .Throws(new InvalidOperationException("Banned"));

            await Assert.ThrowsAsync<BusinessLogicException>(() => lobbyLogic.JoinLobbyAsync("CODE1", 2));
        }

        [Fact]
        public async Task JoinLobbyAsync_SessionFull_ThrowsBusinessLogicException()
        {
            var lobby = new Lobby { idStatusLobby = (int)LobbyStatus.Waiting };
            var player = new Player { idPlayer = 2 };
            lobbyRepositoryMock.Setup(r => r.GetLobbyByRoomCodeAsync("CODE1")).ReturnsAsync(lobby);
            playerRepositoryMock.Setup(r => r.GetPlayerByIdAsync(2)).ReturnsAsync(player);
            sessionManagerMock.Setup(m => m.AddPlayerToLobby(It.IsAny<string>(), It.IsAny<PlayerDto>()))
                .Returns((PlayerDto)null);

            await Assert.ThrowsAsync<BusinessLogicException>(() => lobbyLogic.JoinLobbyAsync("CODE1", 2));
        }

        [Fact]
        public async Task JoinLobbyAsync_Success_ReturnsPlayerDto()
        {
            var lobby = new Lobby { idStatusLobby = (int)LobbyStatus.Waiting };
            var player = new Player { idPlayer = 2 };
            var dto = new PlayerDto { idPlayer = 2 };

            lobbyRepositoryMock.Setup(r => r.GetLobbyByRoomCodeAsync("CODE1")).ReturnsAsync(lobby);
            playerRepositoryMock.Setup(r => r.GetPlayerByIdAsync(2)).ReturnsAsync(player);
            sessionManagerMock.Setup(m => m.AddPlayerToLobby("CODE1", It.IsAny<PlayerDto>())).Returns(dto);

            var result = await lobbyLogic.JoinLobbyAsync("CODE1", 2);

            Assert.Equal(2, result.idPlayer);
        }

        [Fact]
        public async Task JoinLobbyAsGuestAsync_InviteUsed_ThrowsBusinessLogicException()
        {
            guestInvitationManagerMock.Setup(m => m.ValidateInvitation(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(InviteResult.Used);

            await Assert.ThrowsAsync<BusinessLogicException>(() => lobbyLogic.JoinLobbyAsGuestAsync("email", "CODE1"));
        }

        [Fact]
        public async Task JoinLobbyAsGuestAsync_InviteInvalid_ThrowsBusinessLogicException()
        {
            guestInvitationManagerMock.Setup(m => m.ValidateInvitation(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(InviteResult.NotFound);

            await Assert.ThrowsAsync<BusinessLogicException>(() => lobbyLogic.JoinLobbyAsGuestAsync("email", "CODE1"));
        }

        [Fact]
        public async Task JoinLobbyAsGuestAsync_UserAlreadyRegistered_ThrowsBusinessLogicException()
        {
            guestInvitationManagerMock.Setup(m => m.ValidateInvitation(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(InviteResult.Valid);
            playerRepositoryMock.Setup(r => r.GetPlayerByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(new Player());

            await Assert.ThrowsAsync<BusinessLogicException>(() => lobbyLogic.JoinLobbyAsGuestAsync("email", "CODE1"));
        }

        [Fact]
        public async Task JoinLobbyAsGuestAsync_LobbyNotFound_ThrowsBusinessLogicException()
        {
            guestInvitationManagerMock.Setup(m => m.ValidateInvitation(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(InviteResult.Valid);
            playerRepositoryMock.Setup(r => r.GetPlayerByEmailAsync(It.IsAny<string>())).ReturnsAsync((Player)null);
            lobbyRepositoryMock.Setup(r => r.GetLobbyByRoomCodeAsync("CODE1")).ReturnsAsync((Lobby)null);

            await Assert.ThrowsAsync<BusinessLogicException>(() => lobbyLogic.JoinLobbyAsGuestAsync("email", "CODE1"));
        }

        [Fact]
        public async Task JoinLobbyAsGuestAsync_Success_ReturnsGuestDto()
        {
            var lobby = new Lobby { idStatusLobby = (int)LobbyStatus.Waiting };
            guestInvitationManagerMock.Setup(m => m.ValidateInvitation(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(InviteResult.Valid);
            playerRepositoryMock.Setup(r => r.GetPlayerByEmailAsync(It.IsAny<string>())).ReturnsAsync((Player)null);
            lobbyRepositoryMock.Setup(r => r.GetLobbyByRoomCodeAsync("CODE1")).ReturnsAsync(lobby);
            sessionManagerMock.Setup(m => m.AddGuestToLobby("CODE1")).Returns(new PlayerDto { idPlayer = -1 });

            var result = await lobbyLogic.JoinLobbyAsGuestAsync("email", "CODE1");

            Assert.Equal(-1, result.idPlayer);
        }


        [Fact]
        public async Task LeaveLobbyAsync_LobbyNotFound_ReturnsFalse()
        {
            lobbyRepositoryMock.Setup(r => r.GetLobbyByRoomCodeAsync("CODE1")).ReturnsAsync((Lobby)null);

            var result = await lobbyLogic.LeaveLobbyAsync("CODE1", 1);

            Assert.False(result);
        }

        [Fact]
        public async Task LeaveLobbyAsync_HostLeaves_ReturnsTrue()
        {
            var lobby = new Lobby { idHostPlayer = 1, roomCode = "CODE1" };
            lobbyRepositoryMock.Setup(r => r.GetLobbyByRoomCodeAsync("CODE1")).ReturnsAsync(lobby);

            var result = await lobbyLogic.LeaveLobbyAsync("CODE1", 1);

            Assert.True(result);
        }

        [Fact]
        public async Task LeaveLobbyAsync_HostLeaves_RemovesLobbyFromSession()
        {
            var lobby = new Lobby { idHostPlayer = 1, roomCode = "CODE1" };
            lobbyRepositoryMock.Setup(r => r.GetLobbyByRoomCodeAsync("CODE1")).ReturnsAsync(lobby);

            await lobbyLogic.LeaveLobbyAsync("CODE1", 1);

            sessionManagerMock.Verify(m => m.RemoveLobby("CODE1"), Times.Once);
        }

        [Fact]
        public async Task LeaveLobbyAsync_HostLeaves_UpdatesLobbyStatusToFinished()
        {
            var lobby = new Lobby { idHostPlayer = 1, roomCode = "CODE1" };
            lobbyRepositoryMock.Setup(r => r.GetLobbyByRoomCodeAsync("CODE1")).ReturnsAsync(lobby);

            await lobbyLogic.LeaveLobbyAsync("CODE1", 1);

            Assert.Equal((int)LobbyStatus.Finished, lobby.idStatusLobby);
        }


        [Fact]
        public async Task SelectGamemodeAsync_LobbyNotFound_ThrowsBusinessLogicException()
        {
            lobbyRepositoryMock.Setup(r => r.GetLobbyByRoomCodeAsync("CODE1")).ReturnsAsync((Lobby)null);

            await Assert.ThrowsAsync<BusinessLogicException>(() => lobbyLogic.SelectGamemodeAsync("CODE1", 1));
        }

        [Fact]
        public async Task SelectGamemodeAsync_Success_UpdatesLobby()
        {
            var lobby = new Lobby { roomCode = "CODE1" };
            lobbyRepositoryMock.Setup(r => r.GetLobbyByRoomCodeAsync("CODE1")).ReturnsAsync(lobby);

            await lobbyLogic.SelectGamemodeAsync("CODE1", 1);

            sessionManagerMock.Verify(m => m.SetGamemode("CODE1", 1), Times.Once);
        }

        [Fact]
        public async Task StartGameAsync_SessionNotFound_ThrowsBusinessLogicException()
        {
            sessionManagerMock.Setup(m => m.GetLobbySession("CODE1")).Returns((LobbySession)null);

            await Assert.ThrowsAsync<BusinessLogicException>(() => lobbyLogic.StartGameAsync("CODE1"));
        }

        [Fact]
        public async Task StartGameAsync_NoGamemode_ThrowsBusinessLogicException()
        {
            var session = new LobbySession
            {
                RoomCode = "CODE1",
                Players = new List<PlayerDto> { new PlayerDto() }
            };

            sessionManagerMock.Setup(m => m.GetLobbySession("CODE1")).Returns(session);

            await Assert.ThrowsAsync<BusinessLogicException>(() => lobbyLogic.StartGameAsync("CODE1"));
        }

        [Fact]
        public async Task StartGameAsync_NotEnoughPlayers_ThrowsBusinessLogicException()
        {
            var session = new LobbySession
            {
                RoomCode = "CODE1",
                Players = new List<PlayerDto> { new PlayerDto() },
                IdGamemode = 1
            };

            sessionManagerMock.Setup(m => m.GetLobbySession("CODE1")).Returns(session);

            await Assert.ThrowsAsync<BusinessLogicException>(() => lobbyLogic.StartGameAsync("CODE1"));
        }

        [Fact]
        public async Task StartGameAsync_Success_CreatesGameInSessionManager()
        {
            var p1 = new PlayerDto { idPlayer = 1 };
            var p2 = new PlayerDto { idPlayer = 2 };

            var session = new LobbySession
            {
                RoomCode = "CODE1",
                Players = new List<PlayerDto> { p1, p2 },
                IdGamemode = 1
            };

            var lobby = new Lobby { idStatusLobby = (int)LobbyStatus.Waiting };

            sessionManagerMock.Setup(m => m.GetLobbySession("CODE1")).Returns(session);
            lobbyRepositoryMock.Setup(r => r.GetLobbyByRoomCodeAsync("CODE1")).ReturnsAsync(lobby);

            var mockGame = new Mock<ConquiánServidor.BusinessLogic.Game.GameLogic>("CODE1", 1, new List<PlayerDto> { p1, p2 });
            gameSessionManagerMock.Setup(m => m.GetGame("CODE1")).Returns(mockGame.Object);

            await lobbyLogic.StartGameAsync("CODE1");

            gameSessionManagerMock.Verify(m => m.CreateGame("CODE1", 1, It.IsAny<List<PlayerDto>>()), Times.Once);
        }

        [Fact]
        public async Task StartGameAsync_Success_UpdatesLobbyStatusToInGame()
        {
            var p1 = new PlayerDto { idPlayer = 1 };
            var p2 = new PlayerDto { idPlayer = 2 };

            var session = new LobbySession
            {
                RoomCode = "CODE1",
                Players = new List<PlayerDto> { p1, p2 },
                IdGamemode = 1
            };

            var lobby = new Lobby { idStatusLobby = (int)LobbyStatus.Waiting };

            sessionManagerMock.Setup(m => m.GetLobbySession("CODE1")).Returns(session);
            lobbyRepositoryMock.Setup(r => r.GetLobbyByRoomCodeAsync("CODE1")).ReturnsAsync(lobby);

            var mockGame = new Mock<ConquiánServidor.BusinessLogic.Game.GameLogic>("CODE1", 1, new List<PlayerDto> { p1, p2 });
            gameSessionManagerMock.Setup(m => m.GetGame("CODE1")).Returns(mockGame.Object);

            await lobbyLogic.StartGameAsync("CODE1");

            Assert.Equal((int)LobbyStatus.InGame, lobby.idStatusLobby);
        }

        [Fact]
        public async Task KickPlayerAsync_LobbyNotFound_ThrowsBusinessLogicException()
        {
            lobbyRepositoryMock.Setup(r => r.GetLobbyByRoomCodeAsync("CODE1")).ReturnsAsync((Lobby)null);

            await Assert.ThrowsAsync<BusinessLogicException>(() => lobbyLogic.KickPlayerAsync("CODE1", 1, 2));
        }

        [Fact]
        public async Task KickPlayerAsync_NotHost_ThrowsBusinessLogicException()
        {
            var lobby = new Lobby { idHostPlayer = 1 };
            lobbyRepositoryMock.Setup(r => r.GetLobbyByRoomCodeAsync("CODE1")).ReturnsAsync(lobby);

            await Assert.ThrowsAsync<BusinessLogicException>(() => lobbyLogic.KickPlayerAsync("CODE1", 2, 3));
        }

        [Fact]
        public async Task KickPlayerAsync_KickSelf_ThrowsBusinessLogicException()
        {
            var lobby = new Lobby { idHostPlayer = 1 };
            lobbyRepositoryMock.Setup(r => r.GetLobbyByRoomCodeAsync("CODE1")).ReturnsAsync(lobby);

            await Assert.ThrowsAsync<BusinessLogicException>(() => lobbyLogic.KickPlayerAsync("CODE1", 1, 1));
        }

        [Fact]
        public async Task KickPlayerAsync_Success_BansPlayer()
        {
            var lobby = new Lobby { idHostPlayer = 1 };
            lobbyRepositoryMock.Setup(r => r.GetLobbyByRoomCodeAsync("CODE1")).ReturnsAsync(lobby);

            await lobbyLogic.KickPlayerAsync("CODE1", 1, 2);

            sessionManagerMock.Verify(m => m.BanPlayer("CODE1", 2), Times.Once);
        }

        [Fact]
        public async Task KickPlayerAsync_Success_RemovesPlayerFromLobby()
        {
            var lobby = new Lobby { idHostPlayer = 1 };
            lobbyRepositoryMock.Setup(r => r.GetLobbyByRoomCodeAsync("CODE1")).ReturnsAsync(lobby);

            await lobbyLogic.KickPlayerAsync("CODE1", 1, 2);

            sessionManagerMock.Verify(m => m.RemovePlayerFromLobby("CODE1", 2), Times.Once);
        }

        [Fact]
        public async Task CreateLobbyAsync_Success_CreatesLobbyInSession()
        {
            var player = new Player { idPlayer = 1, nickname = "Host" };
            playerRepositoryMock.Setup(r => r.GetPlayerByIdAsync(1)).ReturnsAsync(player);
            lobbyRepositoryMock.Setup(r => r.DoesRoomCodeExistAsync(It.IsAny<string>())).ReturnsAsync(false);

            await lobbyLogic.CreateLobbyAsync(1);

            sessionManagerMock.Verify(m => m.CreateLobby(It.IsAny<string>(), It.IsAny<PlayerDto>()), Times.Once);
        }

        [Fact]
        public async Task JoinLobbyAsGuestAsync_LobbyInGame_ThrowsLobbyFullException()
        {
            var lobby = new Lobby { idStatusLobby = (int)LobbyStatus.InGame };
            guestInvitationManagerMock.Setup(m => m.ValidateInvitation("email", "CODE1")).Returns(InviteResult.Valid);
            playerRepositoryMock.Setup(r => r.GetPlayerByEmailAsync("email")).ReturnsAsync((Player)null);
            lobbyRepositoryMock.Setup(r => r.GetLobbyByRoomCodeAsync("CODE1")).ReturnsAsync(lobby);

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() => lobbyLogic.JoinLobbyAsGuestAsync("email", "CODE1"));

            Assert.Equal(ServiceErrorType.LobbyFull, exception.ErrorType);
        }

        [Fact]
        public async Task LeaveLobbyAsync_NonHostLeaves_ReturnsFalse()
        {
            var lobby = new Lobby { idHostPlayer = 1, roomCode = "CODE1" };
            lobbyRepositoryMock.Setup(r => r.GetLobbyByRoomCodeAsync("CODE1")).ReturnsAsync(lobby);

            var result = await lobbyLogic.LeaveLobbyAsync("CODE1", 2);

            Assert.False(result);
        }

        [Fact]
        public async Task LeaveLobbyAsync_NonHostLeaves_DoesNotRemoveLobby()
        {
            var lobby = new Lobby { idHostPlayer = 1, roomCode = "CODE1" };
            lobbyRepositoryMock.Setup(r => r.GetLobbyByRoomCodeAsync("CODE1")).ReturnsAsync(lobby);

            await lobbyLogic.LeaveLobbyAsync("CODE1", 2);

            sessionManagerMock.Verify(m => m.RemoveLobby("CODE1"), Times.Never);
        }

        [Fact]
        public async Task SelectGamemodeAsync_Success_SavesChangesToRepository()
        {
            var lobby = new Lobby { roomCode = "CODE1" };
            lobbyRepositoryMock.Setup(r => r.GetLobbyByRoomCodeAsync("CODE1")).ReturnsAsync(lobby);

            await lobbyLogic.SelectGamemodeAsync("CODE1", 1);

            lobbyRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task JoinLobbyAsync_PlayerBanned_ThrowsPlayerBannedErrorType()
        {
            var lobby = new Lobby { idStatusLobby = (int)LobbyStatus.Waiting };
            var player = new Player { idPlayer = 2 };
            lobbyRepositoryMock.Setup(r => r.GetLobbyByRoomCodeAsync("CODE1")).ReturnsAsync(lobby);
            playerRepositoryMock.Setup(r => r.GetPlayerByIdAsync(2)).ReturnsAsync(player);
            sessionManagerMock.Setup(m => m.AddPlayerToLobby(It.IsAny<string>(), It.IsAny<PlayerDto>()))
                .Throws(new InvalidOperationException("Banned"));

            var exception = await Assert.ThrowsAsync<BusinessLogicException>(() => lobbyLogic.JoinLobbyAsync("CODE1", 2));

            Assert.Equal(ServiceErrorType.PlayerBanned, exception.ErrorType);
        }
    }
}
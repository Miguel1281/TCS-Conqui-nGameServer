using ConquiánServidor.BusinessLogic;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.Enums;
using System;
using System.Linq;
using Xunit;

namespace ConquiánServidor.Tests.BusinessLogic
{
    public class LobbySessionManagerTests
    {
        private readonly LobbySessionManager lobbySessionManager;

        public LobbySessionManagerTests()
        {
            lobbySessionManager = new LobbySessionManager();
        }

        [Fact]
        public void CreateLobby_ValidRoomCode_ReturnsSessionWithHost()
        {
            string roomCode = "TEST01";
            var host = new PlayerDto { idPlayer = 1, nickname = "Host" };

            var session = lobbySessionManager.CreateLobby(roomCode, host);

            Assert.NotNull(session);
            Assert.Equal(roomCode, session.RoomCode);
            Assert.Equal(host.idPlayer, session.IdHostPlayer);
            Assert.Single(session.Players);
        }

        [Fact]
        public void AddPlayerToLobby_LobbyFull_ReturnsNull()
        {
            string roomCode = "FULL01";
            lobbySessionManager.CreateLobby(roomCode, new PlayerDto { idPlayer = 1 });
            lobbySessionManager.AddPlayerToLobby(roomCode, new PlayerDto { idPlayer = 2 });

            var result = lobbySessionManager.AddPlayerToLobby(roomCode, new PlayerDto { idPlayer = 3 });

            Assert.Null(result);
        }

        [Fact]
        public void AddPlayerToLobby_PlayerIsBanned_ThrowsInvalidOperationException()
        {
            string roomCode = "BAN01";
            int bannedId = 99;
            lobbySessionManager.CreateLobby(roomCode, new PlayerDto { idPlayer = 1 });
            lobbySessionManager.BanPlayer(roomCode, bannedId);

            Assert.Throws<InvalidOperationException>(() =>
                lobbySessionManager.AddPlayerToLobby(roomCode, new PlayerDto { idPlayer = bannedId }));
        }

        [Fact]
        public void AddGuestToLobby_ValidLobby_ReturnsGuestWithNegativeId()
        {
            string roomCode = "GUEST01";
            lobbySessionManager.CreateLobby(roomCode, new PlayerDto { idPlayer = 1 });

            var guest = lobbySessionManager.AddGuestToLobby(roomCode);

            Assert.NotNull(guest);
            Assert.True(guest.idPlayer < 0);
            Assert.StartsWith("Guest", guest.nickname);
        }

        [Fact]
        public void RemovePlayerFromLobby_GuestLeaves_ReturnsIdToPool()
        {
            string roomCode = "POOL01";
            lobbySessionManager.CreateLobby(roomCode, new PlayerDto { idPlayer = 1 });
            var guest = lobbySessionManager.AddGuestToLobby(roomCode);
            int guestId = guest.idPlayer;

            lobbySessionManager.RemovePlayerFromLobby(roomCode, guestId);
            var newGuest = lobbySessionManager.AddGuestToLobby(roomCode);

            Assert.Equal(guestId, newGuest.idPlayer);
        }

        [Fact]
        public void SetGamemode_ExistingLobby_UpdatesIdGamemode()
        {
            string roomCode = "MODE01";
            int expectedMode = 2;
            lobbySessionManager.CreateLobby(roomCode, new PlayerDto { idPlayer = 1 });

            lobbySessionManager.SetGamemode(roomCode, expectedMode);
            var session = lobbySessionManager.GetLobbySession(roomCode);

            Assert.Equal(expectedMode, session.IdGamemode);
        }

        [Fact]
        public void RemoveLobby_LobbyWithGuests_CleansUpAndReturnsGuestIds()
        {
            string roomCode = "CLEAN01";
            lobbySessionManager.CreateLobby(roomCode, new PlayerDto { idPlayer = 1 });
            var guest = lobbySessionManager.AddGuestToLobby(roomCode);
            int guestId = guest.idPlayer;

            lobbySessionManager.RemoveLobby(roomCode);

            lobbySessionManager.CreateLobby("NEW", new PlayerDto { idPlayer = 10 });
            var nextGuest = lobbySessionManager.AddGuestToLobby("NEW");

            Assert.Equal(guestId, nextGuest.idPlayer);
            Assert.Null(lobbySessionManager.GetLobbySession(roomCode));
        }

        [Fact]
        public void AddPlayerToLobby_PlayerAlreadyInLobby_DoesNotAddDuplicate()
        {
            string roomCode = "DUP01";
            var player = new PlayerDto { idPlayer = 5 };
            lobbySessionManager.CreateLobby(roomCode, new PlayerDto { idPlayer = 1 });
            lobbySessionManager.AddPlayerToLobby(roomCode, player);

            var result = lobbySessionManager.AddPlayerToLobby(roomCode, player);
            var session = lobbySessionManager.GetLobbySession(roomCode);

            Assert.Equal(2, session.Players.Count);
        }
    }
}
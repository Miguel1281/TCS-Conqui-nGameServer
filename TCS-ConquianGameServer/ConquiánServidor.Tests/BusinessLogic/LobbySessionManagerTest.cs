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
        [Fact]
        public void GetLobbySession_ReturnsNull_WhenNotExisting()
        {
            var manager = new LobbySessionManager();
            var result = manager.GetLobbySession("ABC");
            Assert.Null(result);
        }

        [Fact]
        public void CreateLobby_CreatesSessionWithHost()
        {
            var manager = new LobbySessionManager();
            var host = new PlayerDto { idPlayer = 1, nickname = "Host" };
            var session = manager.CreateLobby("ROOM1", host);
            Assert.Equal("ROOM1", session.RoomCode);
        }

        [Fact]
        public void AddPlayerToLobby_ReturnsNull_WhenSessionNotFound()
        {
            var manager = new LobbySessionManager();
            var result = manager.AddPlayerToLobby("NOPE", new PlayerDto { idPlayer = 2 });
            Assert.Null(result);
        }

        [Fact]
        public void AddPlayerToLobby_ReturnsPlayer_WhenAddedSuccessfully()
        {
            var manager = new LobbySessionManager();
            var host = new PlayerDto { idPlayer = 1 };
            manager.CreateLobby("R1", host);
            var player = new PlayerDto { idPlayer = 2 };
            var added = manager.AddPlayerToLobby("R1", player);
            Assert.Equal(2, added.idPlayer);
        }

        [Fact]
        public void AddPlayerToLobby_ReturnsNull_WhenLobbyIsFull()
        {
            var manager = new LobbySessionManager();
            var host = new PlayerDto { idPlayer = 1 };
            var other = new PlayerDto { idPlayer = 2 };
            manager.CreateLobby("R1", host);
            manager.AddPlayerToLobby("R1", other);
            var result = manager.AddPlayerToLobby("R1", new PlayerDto { idPlayer = 3 });
            Assert.Null(result);
        }

        [Fact]
        public void AddPlayerToLobby_ReturnsSame_WhenAlreadyInside()
        {
            var manager = new LobbySessionManager();
            var host = new PlayerDto { idPlayer = 1 };
            manager.CreateLobby("R1", host);
            var again = manager.AddPlayerToLobby("R1", host);
            Assert.Equal(1, again.idPlayer);
        }

        [Fact]
        public void AddPlayerToLobby_Throws_WhenPlayerIsBanned()
        {
            var manager = new LobbySessionManager();
            var host = new PlayerDto { idPlayer = 1 };
            manager.CreateLobby("R1", host);
            manager.BanPlayer("R1", 3);
            Assert.Throws<InvalidOperationException>(() => manager.AddPlayerToLobby("R1", new PlayerDto { idPlayer = 3 }));
        }

        [Fact]
        public void AddGuestToLobby_ReturnsNull_WhenSessionNotFound()
        {
            var manager = new LobbySessionManager();
            var guest = manager.AddGuestToLobby("NOPE");
            Assert.Null(guest);
        }

        [Fact]
        public void AddGuestToLobby_AddsGuest_WhenSpaceAvailable()
        {
            var manager = new LobbySessionManager();
            var host = new PlayerDto { idPlayer = 1 };
            manager.CreateLobby("R1", host);
            var guest = manager.AddGuestToLobby("R1");
            Assert.True(guest.idPlayer < 0);
        }

        [Fact]
        public void AddGuestToLobby_ReturnsNull_WhenLobbyIsFull()
        {
            var manager = new LobbySessionManager();
            var host = new PlayerDto { idPlayer = 1 };
            var other = new PlayerDto { idPlayer = 2 };
            manager.CreateLobby("R1", host);
            manager.AddPlayerToLobby("R1", other);
            var guest = manager.AddGuestToLobby("R1");
            Assert.Null(guest);
        }

        [Fact]
        public void RemovePlayerFromLobby_ReturnsNull_WhenSessionNotFound()
        {
            var manager = new LobbySessionManager();
            var removed = manager.RemovePlayerFromLobby("NOPE", 1);
            Assert.Null(removed);
        }

        [Fact]
        public void RemovePlayerFromLobby_RemovesExistingPlayer()
        {
            var manager = new LobbySessionManager();
            var host = new PlayerDto { idPlayer = 1 };
            var p2 = new PlayerDto { idPlayer = 2 };
            manager.CreateLobby("R1", host);
            manager.AddPlayerToLobby("R1", p2);
            var removed = manager.RemovePlayerFromLobby("R1", 2);
            Assert.Equal(2, removed.idPlayer);
        }

        [Fact]
        public void RemovePlayerFromLobby_ReturnsNull_WhenPlayerNotInLobby()
        {
            var manager = new LobbySessionManager();
            var host = new PlayerDto { idPlayer = 1 };
            manager.CreateLobby("R1", host);
            var removed = manager.RemovePlayerFromLobby("R1", 2);
            Assert.Null(removed);
        }

        [Fact]
        public void RemovePlayerFromLobby_RecyclesGuestId()
        {
            var manager = new LobbySessionManager();
            var host = new PlayerDto { idPlayer = 1 };
            manager.CreateLobby("R1", host);
            var guest = manager.AddGuestToLobby("R1");
            manager.RemovePlayerFromLobby("R1", guest.idPlayer);
            var guest2 = manager.AddGuestToLobby("R1");
            Assert.Equal(guest.idPlayer, guest2.idPlayer);
        }

        [Fact]
        public void SetGamemode_SetsIdGamemode()
        {
            var manager = new LobbySessionManager();
            var host = new PlayerDto { idPlayer = 1 };
            var session = manager.CreateLobby("R1", host);
            manager.SetGamemode("R1", 7);
            Assert.Equal(7, session.IdGamemode);
        }

        [Fact]
        public void RemoveLobby_RemovesActiveLobby()
        {
            var manager = new LobbySessionManager();
            var host = new PlayerDto { idPlayer = 1 };
            manager.CreateLobby("R1", host);
            manager.RemoveLobby("R1");
            Assert.Null(manager.GetLobbySession("R1"));
        }

        [Fact]
        public void RemoveLobby_RecyclesGuestIdsFromLobby()
        {
            var manager = new LobbySessionManager();
            var host = new PlayerDto { idPlayer = 1 };
            manager.CreateLobby("R1", host);
            var guest = manager.AddGuestToLobby("R1");
            manager.RemoveLobby("R1");
            manager.CreateLobby("R2", host);
            var guest2 = manager.AddGuestToLobby("R2");
            Assert.Equal(guest.idPlayer, guest2.idPlayer);
        }

        [Fact]
        public void BanPlayer_AddsToKickedPlayers()
        {
            var manager = new LobbySessionManager();
            var host = new PlayerDto { idPlayer = 1 };
            var session = manager.CreateLobby("R1", host);
            manager.BanPlayer("R1", 5);
            Assert.Contains(5, session.KickedPlayers);
        }

        [Fact]
        public void GetLobbyCodeForPlayer_ReturnsNull_WhenNotFound()
        {
            var manager = new LobbySessionManager();
            var code = manager.GetLobbyCodeForPlayer(1);
            Assert.Null(code);
        }

        [Fact]
        public void GetLobbyCodeForPlayer_ReturnsCode_WhenFound()
        {
            var manager = new LobbySessionManager();
            var host = new PlayerDto { idPlayer = 10 };
            manager.CreateLobby("R1", host);
            var code = manager.GetLobbyCodeForPlayer(10);
            Assert.Equal("R1", code);
        }
    }
}
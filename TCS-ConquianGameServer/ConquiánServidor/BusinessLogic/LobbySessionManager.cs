using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.Enums;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace ConquiánServidor.BusinessLogic
{
    public class LobbySessionManager:ILobbySessionManager 
    {
        private readonly ConcurrentDictionary<string, LobbySession> activeLobbies;
        private readonly ConcurrentStack<int> availableGuestIds;
        private const int MAX_CONCURRENT_GUESTS = 100;

        public LobbySessionManager()
        {
            activeLobbies = new ConcurrentDictionary<string, LobbySession>();
            availableGuestIds = new ConcurrentStack<int>();

            for (int i = 1; i <= MAX_CONCURRENT_GUESTS; i++)
            {
                availableGuestIds.Push(-i);
            }
        }

        public LobbySession GetLobbySession(string roomCode)
        {
            activeLobbies.TryGetValue(roomCode, out var session);
            return session;
        }

        public LobbySession CreateLobby(string roomCode, PlayerDto hostPlayer)
        {
            var newSession = new LobbySession
            {
                RoomCode = roomCode,
                IdHostPlayer = hostPlayer.idPlayer,
                IdGamemode = null
            };
            newSession.Players.Add(hostPlayer);

            activeLobbies[roomCode] = newSession;
            return newSession;
        }

        public PlayerDto AddPlayerToLobby(string roomCode, PlayerDto player)
        {
            var session = GetLobbySession(roomCode);
            if (session == null)
            {
                return null;
            }

            lock (session)
            {
                if (session.KickedPlayers.Contains(player.idPlayer))
                {
                    throw new InvalidOperationException("Banned");
                }

                if (session.Players.Count >= 2)
                {
                    return null;
                }

                if (session.Players.Any(p => p.idPlayer == player.idPlayer))
                {
                    return player;
                }

                session.Players.Add(player);
                return player;
            }
        }

        public PlayerDto AddGuestToLobby(string roomCode)
        {
            var session = GetLobbySession(roomCode);
            if (session == null) return null;

            if (!availableGuestIds.TryPop(out int guestId))
            {
                return null;
            }

            lock (session)
            {
                if (session.Players.Count >= 2)
                {
                    availableGuestIds.Push(guestId);
                    return null;
                }

                string nickname = $"Guest{Math.Abs(guestId)}";

                var guestPlayer = new PlayerDto
                {
                    idPlayer = guestId,
                    nickname = nickname,
                    pathPhoto = "/Resources/imageProfile/default.JPG",
                    Status = PlayerStatus.Online
                };

                session.Players.Add(guestPlayer);
                return guestPlayer;
            }
        }

        public PlayerDto RemovePlayerFromLobby(string roomCode, int idPlayer)
        {
            var session = GetLobbySession(roomCode);
            if (session != null)
            {
                lock (session)
                {
                    var playerToRemove = session.Players.FirstOrDefault(p => p.idPlayer == idPlayer);
                    if (playerToRemove != null)
                    {
                        session.Players.Remove(playerToRemove);

                        if (playerToRemove.idPlayer < 0)
                        {
                            availableGuestIds.Push(playerToRemove.idPlayer);
                        }

                        return playerToRemove;
                    }
                }
            }
            return null;
        }

        public void SetGamemode(string roomCode, int idGamemode)
        {
            var session = GetLobbySession(roomCode);
            if (session != null)
            {
                lock (session)
                {
                    session.IdGamemode = idGamemode;
                }
            }
        }

        public void RemoveLobby(string roomCode)
        {
            if (activeLobbies.TryRemove(roomCode, out var session) && session.Players.Any(p => p.idPlayer < 0))
            {
                lock (session)
                {
                    var guests = session.Players.Where(p => p.idPlayer < 0).ToList();
                    foreach (var guest in guests)
                    {
                        availableGuestIds.Push(guest.idPlayer);
                    }
                }
            }
        }

        public void BanPlayer(string roomCode, int idPlayer)
        {
            var session = GetLobbySession(roomCode);
            if (session != null)
            {
                lock (session)
                {
                    session.KickedPlayers.Add(idPlayer);
                }
            }
        }
    }
}
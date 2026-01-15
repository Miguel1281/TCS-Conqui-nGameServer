using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.Enums;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.ServiceModel;

namespace ConquiánServidor.BusinessLogic.Lobby
{
    public class LobbySessionManager:ILobbySessionManager 
    {
        private readonly ConcurrentDictionary<string, LobbySession> activeLobbies;
        private readonly ConcurrentStack<int> availableGuestIds;
        private const int MAX_CONCURRENT_GUESTS = 100;
        private const string PATH_PHOTO = "/Resources/imageProfile/default.JPG";

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

        public LobbySession CreateLobby(string roomCode, PlayerDto host)
        {
            var newSession = new LobbySession
            {
                RoomCode = roomCode,
                IdHostPlayer = host.idPlayer,
                IdGamemode = null
            };
            newSession.Players.Add(host);

            activeLobbies[roomCode] = newSession;
            return newSession;
        }

        public PlayerDto AddPlayerToLobby(string roomCode, PlayerDto player)
        {
            var session = GetLobbySession(roomCode);

            lock (session)
            {
                if (session.KickedPlayers.Contains(player.idPlayer))
                {
                    throw new FaultException<ServiceFaultDto>(
                        new ServiceFaultDto(
                            ServiceErrorType.PlayerBanned,
                            "El jugador ha sido expulsado de este lobby",
                            nameof(AddPlayerToLobby)), new FaultReason("El jugador ha sido expulsado de este lobby"));
                }

                if (session.Players.Count >= 2)
                {
                    throw new FaultException<ServiceFaultDto>(
                        new ServiceFaultDto(
                            ServiceErrorType.LobbyFull,
                            "El lobby está lleno",
                            nameof(AddPlayerToLobby)), new FaultReason("El lobby está lleno"));
                }

                var existingPlayer = session.Players.FirstOrDefault(p => p.idPlayer == player.idPlayer);
                if (existingPlayer != null)
                {
                    return existingPlayer;
                }

                session.Players.Add(player);
                return player;
            }
        }

        public PlayerDto AddGuestToLobby(string roomCode)
        {
            var session = GetLobbySession(roomCode);

            if (!availableGuestIds.TryPop(out int guestId))
            {
                throw new FaultException<ServiceFaultDto>(
                    new ServiceFaultDto(
                        ServiceErrorType.OperationFailed,
                        "No hay IDs de invitado disponibles. Se ha alcanzado el límite de invitados concurrentes",
                        nameof(AddGuestToLobby)
                    ),
                    new FaultReason("No hay IDs de invitado disponibles")
                );
            }

            lock (session)
            {
                if (session.Players.Count >= 2)
                {
                    availableGuestIds.Push(guestId);
                    throw new FaultException<ServiceFaultDto>(
                        new ServiceFaultDto(
                            ServiceErrorType.LobbyFull,
                            "El lobby está lleno",
                            nameof(AddGuestToLobby)
                        ),
                        new FaultReason("El lobby está lleno")
                    );
                }

                string nickname = $"Guest{Math.Abs(guestId)}";

                var guestPlayer = new PlayerDto
                {
                    idPlayer = guestId,
                    nickname = nickname,
                    pathPhoto = PATH_PHOTO,
                    Status = PlayerStatus.Online
                };

                session.Players.Add(guestPlayer);
                return guestPlayer;
            }
        }

        public PlayerDto RemovePlayerFromLobby(string roomCode, int idPlayer)
        {
            var session = GetLobbySession(roomCode);

            lock (session)
            {
                var playerToRemove = session.Players.FirstOrDefault(p => p.idPlayer == idPlayer);
                if (playerToRemove == null)
                {
                    throw new FaultException<ServiceFaultDto>(
                        new ServiceFaultDto(
                            ServiceErrorType.NotFound,
                            $"El jugador con ID {idPlayer} no se encontró en el lobby",
                            nameof(RemovePlayerFromLobby)
                        ),
                        new FaultReason($"El jugador con ID {idPlayer} no se encontró en el lobby")
                    );
                }

                session.Players.Remove(playerToRemove);

                if (playerToRemove.idPlayer < 0)
                {
                    availableGuestIds.Push(playerToRemove.idPlayer);
                }

                return playerToRemove;
            }
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

        public string GetLobbyCodeForPlayer(int idPlayer)
        {
            foreach (var session in activeLobbies.Values)
            {
                lock (session)
                {
                    if (session.Players.Any(p => p.idPlayer == idPlayer))
                    {
                        return session.RoomCode;
                    }
                }
            }

            throw new FaultException<ServiceFaultDto>(
                new ServiceFaultDto(
                    ServiceErrorType.NotFound,
                    $"El jugador con ID {idPlayer} no se encuentra en ningún lobby",
                    nameof(GetLobbyCodeForPlayer)
                ),
                new FaultReason($"El jugador con ID {idPlayer} no se encuentra en ningún lobby")
            );
        }
    }
}
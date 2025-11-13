using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.FaultContracts;
using ConquiánServidor.DataAccess.Abstractions;
using System;
using System.Data.Entity;
using System.Linq;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic
{
    public class LobbyLogic
    {
        private readonly ILobbyRepository lobbyRepository;
        private readonly IPlayerRepository playerRepository;
        private readonly LobbySessionManager sessionManager;
        private static readonly RandomNumberGenerator randomGenerator = RandomNumberGenerator.Create();
        private readonly ConquiánDBEntities dbContext;

        public LobbyLogic(ILobbyRepository lobbyRepository, IPlayerRepository playerRepository, ConquiánDBEntities dbContext)
        {
            this.lobbyRepository = lobbyRepository;
            this.playerRepository = playerRepository;
            this.sessionManager = LobbySessionManager.Instance;
            this.dbContext = dbContext;
        }

        public async Task<LobbyDto> GetLobbyStateAsync(string roomCode)
        {
            var session = sessionManager.GetLobbySession(roomCode);
            if (session == null)
            {
                return null;
            }

            var lobby = await lobbyRepository.GetLobbyByRoomCodeAsync(roomCode);
            if (lobby == null) return null;

            var lobbyDto = new LobbyDto
            {
                RoomCode = lobby.roomCode,
                idHostPlayer = lobby.idHostPlayer,
                StatusLobby = lobby.StatusLobby.statusName,
                idGamemode = session.IdGamemode,
                GameMode = lobby.Gamemode?.gamemode1,
                Players = session.Players.ToList(),

                ChatMessages = new System.Collections.Generic.List<MessageDto>()
            };

            return lobbyDto;
        }

        public async Task<string> CreateLobbyAsync(int idHostPlayer)
        {
            var hostPlayerEntity = await playerRepository.GetPlayerByIdAsync(idHostPlayer);
            if (hostPlayerEntity == null) return null;

            string newRoomCode;
            do
            {
                newRoomCode = GenerateRandomCode();
            }
            while (await lobbyRepository.DoesRoomCodeExistAsync(newRoomCode));

            var newLobby = new ConquiánDB.Lobby()
            {
                roomCode = newRoomCode,
                idHostPlayer = idHostPlayer,
                idStatusLobby = 1,
                creationDate = DateTime.UtcNow,
                idGamemode = null 
            };


            lobbyRepository.AddLobby(newLobby);
            await lobbyRepository.SaveChangesAsync();

            var hostPlayerDto = new PlayerDto
            {
                idPlayer = hostPlayerEntity.idPlayer,
                nickname = hostPlayerEntity.nickname,
                pathPhoto = hostPlayerEntity.pathPhoto
            };

            sessionManager.CreateLobby(newRoomCode, hostPlayerDto);

            return newRoomCode;
        }

        public async Task<PlayerDto> JoinLobbyAsync(string roomCode, int idPlayer)
        {
            var lobby = await lobbyRepository.GetLobbyByRoomCodeAsync(roomCode);
            if (lobby == null || lobby.idStatusLobby != 1)
            {
                return null;
            }

            var playerToJoinEntity = await playerRepository.GetPlayerByIdAsync(idPlayer);
            if (playerToJoinEntity == null) return null;

            var playerDto = new PlayerDto
            {
                idPlayer = playerToJoinEntity.idPlayer,
                nickname = playerToJoinEntity.nickname,
                pathPhoto = playerToJoinEntity.pathPhoto
            };
            var result = sessionManager.AddPlayerToLobby(roomCode, playerDto);

            return result;
        }

        public async Task<PlayerDto> JoinLobbyAsGuestAsync(string email, string roomCode)
        {
            var invitation = await dbContext.GuestInvite
                                 .FirstOrDefaultAsync(gi => gi.email == email && gi.roomCode == roomCode);

            if (invitation == null)
            {
                return null; 
            }

            if (invitation.wasUsed)
            {
                throw new GuestInviteUsedException("Esta invitación ya ha sido utilizada.");
            }

            bool isRegisteredPlayer = await dbContext.Player.AnyAsync(p => p.email == email);

            if (isRegisteredPlayer)
            {
                invitation.wasUsed = true;
                await dbContext.SaveChangesAsync();
                throw new RegisteredUserAsGuestException(
                    "Este correo ya está registrado. Por favor, inicie sesión para unirse a la sala.");
            }

            var lobby = await lobbyRepository.GetLobbyByRoomCodeAsync(roomCode);
            if (lobby == null || lobby.idStatusLobby != 1)
            {
                return null; 
            }
            var playerDto = sessionManager.AddGuestToLobby(roomCode);

            if (playerDto != null)
            {
                invitation.wasUsed = true;
                await dbContext.SaveChangesAsync();
            }

            return playerDto;
        }

        public async Task<bool> LeaveLobbyAsync(string roomCode, int idPlayer)
        {
            var lobby = await lobbyRepository.GetLobbyByRoomCodeAsync(roomCode);
            if (lobby == null) return false;

            bool wasHost = lobby.idHostPlayer == idPlayer;
            sessionManager.RemovePlayerFromLobby(roomCode, idPlayer);

            if (wasHost)
            {
                lobby.idStatusLobby = 3;
                await lobbyRepository.SaveChangesAsync();
                sessionManager.RemoveLobby(roomCode);
            }

            return wasHost;
        }

        private static string GenerateRandomCode(int length = 5)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var data = new byte[length];
            randomGenerator.GetBytes(data);
            return new string(data.Select(b => chars[b % chars.Length]).ToArray());
        }

        public async Task SelectGamemodeAsync(string roomCode, int idGamemode)
        {
            var lobby = await lobbyRepository.GetLobbyByRoomCodeAsync(roomCode);
            if (lobby != null)
            {
                lobby.idGamemode = idGamemode;
                await lobbyRepository.SaveChangesAsync();
                sessionManager.SetGamemode(roomCode, idGamemode);
            }
            else
            {
                throw new Exception("Lobby no encontrado al seleccionar modo de juego.");
            }
        }

        public async Task StartGameAsync(string roomCode)
        {
            var session = sessionManager.GetLobbySession(roomCode);

            if (session == null)
            {
                throw new Exception("El lobby no existe.");
            }
            if (!session.IdGamemode.HasValue)
            {
                throw new Exception("No se ha seleccionado un modo de juego.");
            }
            if (session.Players.Count < 2)
            {
                throw new Exception("No hay suficientes jugadores para iniciar.");
            }

            int gamemodeId = session.IdGamemode.Value;

            var players = session.Players.ToList();

            GameSessionManager.Instance.CreateGame(roomCode, gamemodeId, players);

            var lobby = await lobbyRepository.GetLobbyByRoomCodeAsync(roomCode);
            if (lobby != null)
            {
                lobby.idStatusLobby = 2;
                await lobbyRepository.SaveChangesAsync();
            }
        }
    }
}
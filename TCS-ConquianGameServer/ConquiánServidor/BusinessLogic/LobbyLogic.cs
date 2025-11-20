using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.Properties.Langs; 
using NLog;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic
{
    public class LobbyLogic
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
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
                throw new InvalidOperationException(Lang.ErrorLobbyNotFound);
            }

            var lobby = await lobbyRepository.GetLobbyByRoomCodeAsync(roomCode);
            if (lobby == null)
            {
                throw new InvalidOperationException(Lang.ErrorLobbyNotFound);
            }

            return new LobbyDto
            {
                RoomCode = lobby.roomCode,
                idHostPlayer = lobby.idHostPlayer,
                StatusLobby = lobby.StatusLobby.statusName,
                idGamemode = session.IdGamemode,
                GameMode = lobby.Gamemode?.gamemode1,
                Players = session.Players.ToList(),
                ChatMessages = new List<MessageDto>()
            };
        }

        public async Task<string> CreateLobbyAsync(int idHostPlayer)
        {
            var hostPlayerEntity = await playerRepository.GetPlayerByIdAsync(idHostPlayer);
            if (hostPlayerEntity == null)
            {
                Logger.Warn($"Intento de crear lobby con host desconocido ID: {idHostPlayer}");
                throw new ArgumentException(Lang.ErrorLobbyHostNotFound);
            }

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

            Logger.Info(string.Format(Lang.LogLobbyCreated, newRoomCode, hostPlayerEntity.nickname));
            return newRoomCode;
        }

        public async Task<PlayerDto> JoinLobbyAsync(string roomCode, int idPlayer)
        {
            var lobby = await lobbyRepository.GetLobbyByRoomCodeAsync(roomCode);

            if (lobby == null)
            {
                Logger.Warn($"Intento de unirse a lobby inexistente {roomCode}");
                throw new InvalidOperationException(Lang.ErrorLobbyNotFound);
            }

            if (lobby.idStatusLobby != 1)
            {
                Logger.Warn($"Intento de unirse a lobby lleno/en juego {roomCode}");
                throw new InvalidOperationException(Lang.ErrorLobbyFull);
            }

            var playerToJoinEntity = await playerRepository.GetPlayerByIdAsync(idPlayer);
            if (playerToJoinEntity == null)
            {
                throw new ArgumentException(Lang.ErrorUserNotFound);
            }

            var playerDto = new PlayerDto
            {
                idPlayer = playerToJoinEntity.idPlayer,
                nickname = playerToJoinEntity.nickname,
                pathPhoto = playerToJoinEntity.pathPhoto
            };

            var result = sessionManager.AddPlayerToLobby(roomCode, playerDto);

            if (result != null)
            {
                Logger.Info(string.Format(Lang.LogLobbyJoined, playerToJoinEntity.nickname, roomCode));
            }

            return result;
        }

        public async Task<PlayerDto> JoinLobbyAsGuestAsync(string email, string roomCode)
        {
            var invitation = await dbContext.GuestInvite
                                 .FirstOrDefaultAsync(gi => gi.email == email && gi.roomCode == roomCode);

            if (invitation == null)
            {
                Logger.Warn($"Invitación no encontrada para {email} en sala {roomCode}");
                throw new ArgumentException(Lang.ErrorInvalidInvitation);
            }

            if (invitation.wasUsed)
            {
                Logger.Warn($"Invitación ya usada: {email}");
                throw new GuestInviteUsedException(Lang.ErrorUsedInvitation);
            }

            bool isRegisteredPlayer = await dbContext.Player.AnyAsync(p => p.email == email);

            if (isRegisteredPlayer)
            {
                invitation.wasUsed = true;
                await dbContext.SaveChangesAsync();
                throw new RegisteredUserAsGuestException(Lang.ErrorRegisteredMail);
            }

            var lobby = await lobbyRepository.GetLobbyByRoomCodeAsync(roomCode);
            if (lobby == null) throw new InvalidOperationException(Lang.ErrorLobbyNotFound);
            if (lobby.idStatusLobby != 1) throw new InvalidOperationException(Lang.ErrorLobbyFull);

            var playerDto = sessionManager.AddGuestToLobby(roomCode);

            if (playerDto != null)
            {
                invitation.wasUsed = true;
                await dbContext.SaveChangesAsync();
                Logger.Info(string.Format(Lang.LogLobbyJoined, "Guest-" + email, roomCode));
            }

            return playerDto;
        }

        public async Task<bool> LeaveLobbyAsync(string roomCode, int idPlayer)
        {
            var lobby = await lobbyRepository.GetLobbyByRoomCodeAsync(roomCode);
            if (lobby == null) return false;

            bool wasHost = lobby.idHostPlayer == idPlayer;
            sessionManager.RemovePlayerFromLobby(roomCode, idPlayer);

            Logger.Info(string.Format(Lang.LogLobbyLeft, idPlayer, roomCode));

            if (wasHost)
            {
                lobby.idStatusLobby = 3; 
                await lobbyRepository.SaveChangesAsync();
                sessionManager.RemoveLobby(roomCode);
                Logger.Info($"Lobby {roomCode} cerrado por salida del anfitrión.");
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
                Logger.Info(string.Format(Lang.LogLobbyGamemodeChanged, roomCode, idGamemode));
            }
            else
            {
                throw new InvalidOperationException(Lang.ErrorLobbyNotFound);
            }
        }

        public async Task StartGameAsync(string roomCode)
        {
            var session = sessionManager.GetLobbySession(roomCode);

            if (session == null)
            {
                throw new InvalidOperationException(Lang.ErrorLobbyNotFound);
            }
            if (!session.IdGamemode.HasValue)
            {
                throw new InvalidOperationException(Lang.ErrorLobbyNoGamemode);
            }
            if (session.Players.Count < 2)
            {
                throw new InvalidOperationException(Lang.ErrorLobbyNotEnoughPlayers);
            }

            int gamemodeId = session.IdGamemode.Value;
            var players = session.Players.ToList();

            GameSessionManager.Instance.CreateGame(roomCode, gamemodeId, players);
            var game = GameSessionManager.Instance.GetGame(roomCode);

            if (game != null)
            {
                game.StartGameTimer();
                Logger.Info(string.Format(Lang.LogLobbyGameStarted, roomCode));
            }

            var lobby = await lobbyRepository.GetLobbyByRoomCodeAsync(roomCode);
            if (lobby != null)
            {
                lobby.idStatusLobby = 2; 
                await lobbyRepository.SaveChangesAsync();
            }
        }
    }
}
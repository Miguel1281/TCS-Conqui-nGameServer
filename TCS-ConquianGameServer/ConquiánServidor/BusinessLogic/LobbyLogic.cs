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
            Logger.Info($"Fetching lobby state for Room Code: {roomCode}");

            var session = sessionManager.GetLobbySession(roomCode);
            if (session == null)
            {
                Logger.Warn($"Lobby state lookup failed: Session not found for Room Code {roomCode}");
                throw new InvalidOperationException(Lang.ErrorLobbyNotFound);
            }

            var lobby = await lobbyRepository.GetLobbyByRoomCodeAsync(roomCode);
            if (lobby == null)
            {
                Logger.Warn($"Lobby state lookup failed: Database record not found for Room Code {roomCode}");
                throw new InvalidOperationException(Lang.ErrorLobbyNotFound);
            }

            Logger.Info($"Lobby state retrieved successfully for Room Code: {roomCode}");

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
            Logger.Info($"Lobby creation attempt by Host Player ID: {idHostPlayer}");

            var hostPlayerEntity = await playerRepository.GetPlayerByIdAsync(idHostPlayer);
            if (hostPlayerEntity == null)
            {
                Logger.Warn($"Lobby creation failed: Host Player ID {idHostPlayer} not found.");
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

            Logger.Info($"Lobby created successfully. Room Code: {newRoomCode}, Host Player ID: {idHostPlayer}");
            return newRoomCode;
        }

        public async Task<PlayerDto> JoinLobbyAsync(string roomCode, int idPlayer)
        {
            Logger.Info($"Join lobby attempt. Room Code: {roomCode}, Player ID: {idPlayer}");

            var lobby = await lobbyRepository.GetLobbyByRoomCodeAsync(roomCode);

            if (lobby == null)
            {
                Logger.Warn($"Join lobby failed: Room Code {roomCode} not found.");
                throw new InvalidOperationException(Lang.ErrorLobbyNotFound);
            }

            if (lobby.idStatusLobby != 1)
            {
                Logger.Warn($"Join lobby failed: Room Code {roomCode} is full or in-game (Status: {lobby.idStatusLobby}).");
                throw new InvalidOperationException(Lang.ErrorLobbyFull);
            }

            var playerToJoinEntity = await playerRepository.GetPlayerByIdAsync(idPlayer);
            if (playerToJoinEntity == null)
            {
                Logger.Warn($"Join lobby failed: Player ID {idPlayer} not found in database.");
                throw new ArgumentException(Lang.ErrorUserNotFound);
            }

            var playerDto = new PlayerDto
            {
                idPlayer = playerToJoinEntity.idPlayer,
                nickname = playerToJoinEntity.nickname,
                pathPhoto = playerToJoinEntity.pathPhoto
            };

            try
            {
                var result = sessionManager.AddPlayerToLobby(roomCode, playerDto);

                if (result != null)
                {
                    Logger.Info($"Player joined lobby successfully. Room Code: {roomCode}, Player ID: {idPlayer}");
                }
                else
                {
                    Logger.Warn($"Join lobby failed: Lobby session logic returned null (likely full). Room: {roomCode}");
                }

                return result;
            }
            catch (InvalidOperationException ex) when (ex.Message == "Banned")
            {
                Logger.Warn($"Join lobby failed: Player {idPlayer} is banned from room {roomCode}.");
                throw new InvalidOperationException(Lang.ErrorYouAreKicked);
            }
        }

        public async Task<PlayerDto> JoinLobbyAsGuestAsync(string email, string roomCode)
        {
            Logger.Info($"Guest join attempt for Room Code: {roomCode}");

            var invitation = await dbContext.GuestInvite
                                      .FirstOrDefaultAsync(gi => gi.email == email && gi.roomCode == roomCode);

            if (invitation == null)
            {
                Logger.Warn($"Guest join failed: Invitation not found/matched for Room Code {roomCode}.");
                throw new ArgumentException(Lang.ErrorInvalidInvitation);
            }

            if (invitation.wasUsed)
            {
                Logger.Warn($"Guest join failed: Invitation already used for Room Code {roomCode}.");
                throw new GuestInviteUsedException(Lang.ErrorUsedInvitation);
            }

            bool isRegisteredPlayer = await dbContext.Player.AnyAsync(p => p.email == email);

            if (isRegisteredPlayer)
            {
                invitation.wasUsed = true;
                await dbContext.SaveChangesAsync();
                Logger.Warn($"Guest join failed: User is already registered. Room Code {roomCode}.");
                throw new RegisteredUserAsGuestException(Lang.ErrorRegisteredMail);
            }

            var lobby = await lobbyRepository.GetLobbyByRoomCodeAsync(roomCode);
            if (lobby == null)
            {
                Logger.Warn($"Guest join failed: Room Code {roomCode} not found.");
                throw new InvalidOperationException(Lang.ErrorLobbyNotFound);
            }
            if (lobby.idStatusLobby != 1)
            {
                Logger.Warn($"Guest join failed: Room Code {roomCode} is full or in-game.");
                throw new InvalidOperationException(Lang.ErrorLobbyFull);
            }

            var playerDto = sessionManager.AddGuestToLobby(roomCode);

            if (playerDto != null)
            {
                invitation.wasUsed = true;
                await dbContext.SaveChangesAsync();
                Logger.Info($"Guest joined lobby successfully. Room Code: {roomCode}, Guest Temp ID: {playerDto.idPlayer}");
            }

            return playerDto;
        }

        public async Task<bool> LeaveLobbyAsync(string roomCode, int idPlayer)
        {
            Logger.Info($"Leave lobby attempt. Room Code: {roomCode}, Player ID: {idPlayer}");

            var lobby = await lobbyRepository.GetLobbyByRoomCodeAsync(roomCode);
            if (lobby == null)
            {
                Logger.Warn($"Leave lobby failed: Room Code {roomCode} not found.");
                return false;
            }

            bool wasHost = lobby.idHostPlayer == idPlayer;
            sessionManager.RemovePlayerFromLobby(roomCode, idPlayer);

            Logger.Info($"Player left lobby. Room Code: {roomCode}, Player ID: {idPlayer}");

            if (wasHost)
            {
                lobby.idStatusLobby = 3;
                await lobbyRepository.SaveChangesAsync();
                sessionManager.RemoveLobby(roomCode);
                Logger.Info($"Lobby closed: Host left. Room Code: {roomCode}");
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
            Logger.Info($"Gamemode change attempt. Room Code: {roomCode}, New Gamemode ID: {idGamemode}");

            var lobby = await lobbyRepository.GetLobbyByRoomCodeAsync(roomCode);
            if (lobby != null)
            {
                lobby.idGamemode = idGamemode;
                await lobbyRepository.SaveChangesAsync();
                sessionManager.SetGamemode(roomCode, idGamemode);
                Logger.Info($"Gamemode changed successfully. Room Code: {roomCode}, Gamemode ID: {idGamemode}");
            }
            else
            {
                Logger.Warn($"Gamemode change failed: Room Code {roomCode} not found.");
                throw new InvalidOperationException(Lang.ErrorLobbyNotFound);
            }
        }

        public async Task StartGameAsync(string roomCode)
        {
            Logger.Info($"Start game attempt. Room Code: {roomCode}");

            var session = sessionManager.GetLobbySession(roomCode);

            if (session == null)
            {
                Logger.Warn($"Start game failed: Session not found for Room Code {roomCode}");
                throw new InvalidOperationException(Lang.ErrorLobbyNotFound);
            }
            if (!session.IdGamemode.HasValue)
            {
                Logger.Warn($"Start game failed: No gamemode selected. Room Code: {roomCode}");
                throw new InvalidOperationException(Lang.ErrorLobbyNoGamemode);
            }
            if (session.Players.Count < 2)
            {
                Logger.Warn($"Start game failed: Not enough players ({session.Players.Count}). Room Code: {roomCode}");
                throw new InvalidOperationException(Lang.ErrorLobbyNotEnoughPlayers);
            }

            int gamemodeId = session.IdGamemode.Value;
            var players = session.Players.ToList();

            GameSessionManager.Instance.CreateGame(roomCode, gamemodeId, players);
            var game = GameSessionManager.Instance.GetGame(roomCode);

            if (game != null)
            {
                game.StartGameTimer();
                Logger.Info($"Game started successfully. Room Code: {roomCode}");
            }

            var lobby = await lobbyRepository.GetLobbyByRoomCodeAsync(roomCode);
            if (lobby != null)
            {
                lobby.idStatusLobby = 2; 
                await lobbyRepository.SaveChangesAsync();
            }
        }

        public async Task KickPlayerAsync(string roomCode, int idRequestingPlayer, int idPlayerToKick)
        {
            Logger.Info($"Kick attempt. Room: {roomCode}, Host: {idRequestingPlayer}, Target: {idPlayerToKick}");

            var lobby = await lobbyRepository.GetLobbyByRoomCodeAsync(roomCode);
            if (lobby == null)
            {
                throw new InvalidOperationException(Lang.ErrorLobbyNotFound);
            }

            if (lobby.idHostPlayer != idRequestingPlayer)
            {
                Logger.Warn($"Kick failed: Player {idRequestingPlayer} is not the host of room {roomCode}.");
                throw new UnauthorizedAccessException(Lang.ErrorNotLobbyHost);
            }

            if (idRequestingPlayer == idPlayerToKick)
            {
                throw new InvalidOperationException("Cannot kick yourself.");
            }

            if (idPlayerToKick > 0)
            {
                sessionManager.BanPlayer(roomCode, idPlayerToKick);
            }

            sessionManager.RemovePlayerFromLobby(roomCode, idPlayerToKick);

            Logger.Info($"Player {idPlayerToKick} kicked and banned from room {roomCode} by host.");
        }
    }
}
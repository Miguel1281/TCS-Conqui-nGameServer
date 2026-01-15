using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.Enums;
using ConquiánServidor.DataAccess.Abstractions;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using ConquiánServidor.BusinessLogic.Guest;

namespace ConquiánServidor.BusinessLogic.Lobby
{
    public class LobbyLogic:ILobbyLogic
    {
        private const int ROOM_CODE_LENGTH = 5;
        private const string ROOM_CODE_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int MINIMUM_PLAYERS_TO_START = 2;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly ILobbyRepository lobbyRepository;
        private readonly IPlayerRepository playerRepository;
        private readonly ILobbySessionManager sessionManager;
        private readonly IGameSessionManager gameSessionManager;
        private readonly IGuestInvitationManager guestInvitationManager;
        private static readonly RandomNumberGenerator randomGenerator = RandomNumberGenerator.Create();

        public LobbyLogic(
                    ILobbyRepository lobbyRepository,
                    IPlayerRepository playerRepository,
                    ILobbySessionManager sessionManager,
                    IGameSessionManager gameSessionManager,
                    IGuestInvitationManager guestInvitationManager)
        {
            this.lobbyRepository = lobbyRepository;
            this.playerRepository = playerRepository;

            this.sessionManager = sessionManager;
            this.gameSessionManager = gameSessionManager;
            this.guestInvitationManager = guestInvitationManager;
        }

        public async Task<LobbyDto> GetLobbyStateAsync(string roomCode)
        {
            Logger.Info($"Fetching lobby state for Room Code: {roomCode}");

            var session = sessionManager.GetLobbySession(roomCode);
            if (session == null)
            {
                Logger.Warn($"Lobby state lookup failed: Session not found for Room Code {roomCode}");
                throw new BusinessLogicException(ServiceErrorType.LobbyNotFound);
            }

            var lobby = await lobbyRepository.GetLobbyByRoomCodeAsync(roomCode);
            if (lobby == null)
            {
                Logger.Warn($"Lobby state lookup failed: Database record not found for Room Code {roomCode}");
                throw new BusinessLogicException(ServiceErrorType.LobbyNotFound);
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
                throw new BusinessLogicException(ServiceErrorType.HostUserNotFound);
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
                idStatusLobby = (int)LobbyStatus.Waiting,
                creationDate = DateTime.UtcNow,
                idGamemode = null
            };

            lobbyRepository.AddLobby(newLobby);
            await lobbyRepository.SaveChangesAsync();

            var hostPlayerDto = new PlayerDto
            {
                idPlayer = hostPlayerEntity.idPlayer,
                nickname = hostPlayerEntity.nickname,
                pathPhoto = hostPlayerEntity.pathPhoto,
                Status = PlayerStatus.Online
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
                throw new BusinessLogicException(ServiceErrorType.LobbyNotFound);
            }

            if (lobby.idStatusLobby != (int)LobbyStatus.Waiting)
            {
                Logger.Warn($"Join lobby failed: Room Code {roomCode} is full or in-game (Status: {lobby.idStatusLobby}).");
                throw new BusinessLogicException(ServiceErrorType.LobbyFull);
            }

            var playerToJoinEntity = await playerRepository.GetPlayerByIdAsync(idPlayer);
            if (playerToJoinEntity == null)
            {
                Logger.Warn($"Join lobby failed: Player ID {idPlayer} not found in database.");
                throw new BusinessLogicException(ServiceErrorType.UserNotFound);
            }

            var playerDto = new PlayerDto
            {
                idPlayer = playerToJoinEntity.idPlayer,
                nickname = playerToJoinEntity.nickname,
                pathPhoto = playerToJoinEntity.pathPhoto,
                Status = PlayerStatus.Online
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
                    throw new BusinessLogicException(ServiceErrorType.LobbyFull);
                }

                return result;
            }
            catch (InvalidOperationException ex) when (ex.Message == "Banned")
            {
                Logger.Warn(ex, $"Join lobby failed: Player {idPlayer} is banned from room {roomCode}.");
                throw new BusinessLogicException(ServiceErrorType.PlayerBanned);
            }
        }

        public async Task<PlayerDto> JoinLobbyAsGuestAsync(string email, string roomCode)
        {
            Logger.Info($"Guest join attempt for Room Code: {roomCode}");

            var inviteResult = this.guestInvitationManager.ValidateInvitation(email, roomCode);

            if (inviteResult == GuestInvitationManager.InviteResult.Used)
            {
                Logger.Warn($"Guest join failed: Invitation already used. Room Code {roomCode}.");
                throw new BusinessLogicException(ServiceErrorType.GuestInviteUsed);
            }

            if (inviteResult != GuestInvitationManager.InviteResult.Valid)
            {
                Logger.Warn($"Guest join failed: Invitation not found or expired. Room Code {roomCode}.");
                throw new BusinessLogicException(ServiceErrorType.ValidationFailed);
            }

            var existingPlayer = await playerRepository.GetPlayerByEmailAsync(email);
            bool isRegisteredPlayer = existingPlayer != null;

            if (isRegisteredPlayer)
            {
                Logger.Warn($"Guest join failed: User is already registered. Room Code {roomCode}.");
                throw new BusinessLogicException(ServiceErrorType.RegisteredUserAsGuest);
            }

            var lobby = await lobbyRepository.GetLobbyByRoomCodeAsync(roomCode);
            if (lobby == null)
            {
                Logger.Warn($"Guest join failed: Room Code {roomCode} not found.");
                throw new BusinessLogicException(ServiceErrorType.LobbyNotFound);
            }
            if (lobby.idStatusLobby != (int)LobbyStatus.Waiting)
            {
                Logger.Warn($"Guest join failed: Room Code {roomCode} is full or in-game.");
                throw new BusinessLogicException(ServiceErrorType.LobbyFull);
            }

            var playerDto = sessionManager.AddGuestToLobby(roomCode);

            if (playerDto != null)
            {
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
                lobby.idStatusLobby = (int)LobbyStatus.Finished;
                await lobbyRepository.SaveChangesAsync();
                sessionManager.RemoveLobby(roomCode);
                Logger.Info($"Lobby closed: Host left. Room Code: {roomCode}");
            }

            return wasHost;
        }

        private static string GenerateRandomCode(int length = ROOM_CODE_LENGTH)
        {
            var data = new byte[length];
            randomGenerator.GetBytes(data);
            return new string(data.Select(b => ROOM_CODE_CHARS[b % ROOM_CODE_CHARS.Length]).ToArray());
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
                throw new BusinessLogicException(ServiceErrorType.LobbyNotFound);
            }
        }

        public async Task StartGameAsync(string roomCode)
        {
            Logger.Info($"Start game attempt. Room Code: {roomCode}");

            var session = sessionManager.GetLobbySession(roomCode);

            if (session == null)
            {
                Logger.Warn($"Start game failed: Session not found for Room Code {roomCode}");
                throw new BusinessLogicException(ServiceErrorType.LobbyNotFound);
            }
            if (!session.IdGamemode.HasValue)
            {
                Logger.Warn($"Start game failed: No gamemode selected. Room Code: {roomCode}");
                throw new BusinessLogicException(ServiceErrorType.OperationFailed);
            }
            if (session.Players.Count < MINIMUM_PLAYERS_TO_START)
            {
                Logger.Warn($"Start game failed: Not enough players ({session.Players.Count}). Room Code: {roomCode}");
                throw new BusinessLogicException(ServiceErrorType.NotEnoughPlayers);
            }

            int gamemodeId = session.IdGamemode.Value;
            var players = session.Players.ToList();

            this.gameSessionManager.CreateGame(roomCode, gamemodeId, players);
            var game = this.gameSessionManager.GetGame(roomCode);

            if (game != null)
            {
                game.StartGameTimer();
                Logger.Info($"Game started successfully. Room Code: {roomCode}");
            }

            var lobby = await lobbyRepository.GetLobbyByRoomCodeAsync(roomCode);
            if (lobby != null)
            {
                lobby.idStatusLobby = (int)LobbyStatus.InGame;
                await lobbyRepository.SaveChangesAsync();
            }
        }

        public async Task KickPlayerAsync(string roomCode, int idRequestingPlayer, int idPlayerToKick)
        {
            Logger.Info($"Kick attempt. Room: {roomCode}, Host: {idRequestingPlayer}, Target: {idPlayerToKick}");

            var lobby = await lobbyRepository.GetLobbyByRoomCodeAsync(roomCode);
            if (lobby == null)
            {
                throw new BusinessLogicException(ServiceErrorType.LobbyNotFound);
            }

            if (lobby.idHostPlayer != idRequestingPlayer)
            {
                Logger.Warn($"Kick failed: Player {idRequestingPlayer} is not the host of room {roomCode}.");
                throw new BusinessLogicException(ServiceErrorType.NotLobbyHost);
            }

            if (idRequestingPlayer == idPlayerToKick)
            {
                throw new BusinessLogicException(ServiceErrorType.NotKickYourSelf);
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
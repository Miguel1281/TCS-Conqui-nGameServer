using Autofac;
using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.Enums;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class Lobby : ILobby
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<int, ILobbyCallback>> lobbyCallbacks =
            new ConcurrentDictionary<string, ConcurrentDictionary<int, ILobbyCallback>>();

        private static readonly ConcurrentDictionary<string, List<MessageDto>> chatHistories =
            new ConcurrentDictionary<string, List<MessageDto>>();

        private readonly ILobbyLogic lobbyLogic;
        private readonly IPresenceManager presenceManager;

        private const string LOGIC_ERROR_MESSAGE = "Logic Error";
        private const string INTERNAL_SERVER_ERROR_MESSAGE = "Internal Server Error";
        private const string INTERNAL_ERROR_REASON = "Internal Error";
        private const string LOBBY_NOT_FOUND_MESSAGE = "Lobby not found in memory";
        private const string LOBBY_NOT_FOUND_REASON = "Lobby Not Found";
        private const string DATABASE_ERROR_MESSAGE = "Error connecting to database";
        private const string DATABASE_UNAVAILABLE_REASON = "Database Unavailable";

        public Lobby()
        {
            Bootstrapper.Init();
            this.lobbyLogic = Bootstrapper.Container.Resolve<ILobbyLogic>();
            this.presenceManager = Bootstrapper.Container.Resolve<IPresenceManager>();
        }

        public Lobby(ILobbyLogic lobbyLogic, IPresenceManager presenceManager)
        {
            this.lobbyLogic = lobbyLogic;
            this.presenceManager = presenceManager;
        }

        public async Task<LobbyDto> GetLobbyStateAsync(string roomCode)
        {
            try
            {
                var lobbyState = await lobbyLogic.GetLobbyStateAsync(roomCode);
                if (lobbyState != null)
                {
                    chatHistories.TryAdd(roomCode, new List<MessageDto>());
                    lobbyState.ChatMessages = chatHistories[roomCode];
                }
                return lobbyState;
            }
            catch (BusinessLogicException ex)
            {
                Logger.Debug($"Business logic error getting lobby state. ErrorType: {ex.ErrorType}");
                var faultData = new ServiceFaultDto(ex.ErrorType, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.ErrorType.ToString()));
            }
            catch (SqlException ex)
            {
                Logger.Error(ex, $"Database error getting lobby state for room {roomCode}. SqlError: {ex.Number}");
                var faultData = new ServiceFaultDto(ServiceErrorType.DatabaseError, DATABASE_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(DATABASE_UNAVAILABLE_REASON));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error getting lobby state for room {roomCode}. Type: {ex.GetType().Name}");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, INTERNAL_SERVER_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(INTERNAL_ERROR_REASON));
            }
        }

        public async Task<string> CreateLobbyAsync(int idHostPlayer)
        {
            try
            {
                string newRoomCode = await lobbyLogic.CreateLobbyAsync(idHostPlayer);

                chatHistories.TryAdd(newRoomCode, new List<MessageDto>());
                lobbyCallbacks.TryAdd(newRoomCode, new ConcurrentDictionary<int, ILobbyCallback>());
                await presenceManager.NotifyStatusChange(idHostPlayer, (int)PlayerStatus.InLobby);

                Logger.Info($"Lobby {newRoomCode} created by host {idHostPlayer}");
                return newRoomCode;
            }
            catch (BusinessLogicException ex)
            {
                Logger.Debug($"Business logic error creating lobby. ErrorType: {ex.ErrorType}");
                var faultData = new ServiceFaultDto(ex.ErrorType, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.ErrorType.ToString()));
            }
            catch (SqlException ex)
            {
                Logger.Error(ex, $"Database error creating lobby for host {idHostPlayer}. SqlError: {ex.Number}");
                var faultData = new ServiceFaultDto(ServiceErrorType.DatabaseError, DATABASE_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(DATABASE_UNAVAILABLE_REASON));
            }
            catch (EntityException ex)
            {
                Logger.Error(ex, $"Entity framework error creating lobby for host {idHostPlayer}");
                var faultData = new ServiceFaultDto(ServiceErrorType.DatabaseError, DATABASE_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(DATABASE_UNAVAILABLE_REASON));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error creating lobby for host {idHostPlayer}. Type: {ex.GetType().Name}");
                var faultInternal = new ServiceFaultDto(ServiceErrorType.ServerInternalError, INTERNAL_SERVER_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(faultInternal, new FaultReason(INTERNAL_ERROR_REASON));
            }
        }

        public async Task<bool> JoinAndSubscribeAsync(string roomCode, int idPlayer)
        {
            var callback = OperationContext.Current.GetCallbackChannel<ILobbyCallback>();
            bool joinSuccessful = false;

            try
            {
                if (!lobbyCallbacks.ContainsKey(roomCode))
                {
                    Logger.Warn($"Lobby {roomCode} not found in memory for player {idPlayer}");
                    var faultData = new ServiceFaultDto(ServiceErrorType.NotFound, LOBBY_NOT_FOUND_MESSAGE);
                    throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(LOBBY_NOT_FOUND_REASON));
                }

                var playerDto = await lobbyLogic.JoinLobbyAsync(roomCode, idPlayer);
                if (playerDto == null)
                {
                    Logger.Warn($"Player {idPlayer} could not join lobby {roomCode} - playerDto is null");
                    joinSuccessful = false;
                }
                else
                {
                    RegisterPlayerCallback(roomCode, idPlayer, callback);
                    await presenceManager.NotifyStatusChange(idPlayer, (int)PlayerStatus.InLobby);
                    NotifyPlayersInLobby(roomCode, null, (cb) => cb.PlayerJoined(playerDto));

                    Logger.Info($"Player {idPlayer} successfully joined lobby {roomCode}");
                    joinSuccessful = true;
                }
            }
            catch (BusinessLogicException ex)
            {
                Logger.Warn(ex, $"Business logic error in JoinAndSubscribeAsync. Player: {idPlayer}, Room: {roomCode}, ErrorType: {ex.ErrorType}");
                var faultData = new ServiceFaultDto(ex.ErrorType, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.ErrorType.ToString()));
            }
            catch (FaultException<ServiceFaultDto>)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error in JoinAndSubscribeAsync. Player: {idPlayer}, Room: {roomCode}, Type: {ex.GetType().Name}");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, INTERNAL_SERVER_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(INTERNAL_ERROR_REASON));
            }

            return joinSuccessful;
        }

        private void RegisterPlayerCallback(string roomCode, int idPlayer, ILobbyCallback callback)
        {
            lobbyCallbacks[roomCode][idPlayer] = callback;

            if (callback is ICommunicationObject communicationObject)
            {
                communicationObject.Closed += (sender, args) => HandleClientDisconnect(roomCode, idPlayer);
                communicationObject.Faulted += (sender, args) => HandleClientDisconnect(roomCode, idPlayer);
            }

            Logger.Debug($"Callback registered for player {idPlayer} in room {roomCode}");
        }

        public async Task<PlayerDto> JoinAndSubscribeAsGuestAsync(string email, string roomCode)
        {
            var callback = OperationContext.Current.GetCallbackChannel<ILobbyCallback>();

            try
            {
                if (!lobbyCallbacks.ContainsKey(roomCode))
                {
                    Logger.Warn($"Lobby {roomCode} not found in memory for guest {email}");
                    var faultData = new ServiceFaultDto(ServiceErrorType.NotFound, LOBBY_NOT_FOUND_MESSAGE);
                    throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(LOBBY_NOT_FOUND_REASON));
                }

                var playerDto = await lobbyLogic.JoinLobbyAsGuestAsync(email, roomCode);

                if (playerDto == null)
                {
                    Logger.Warn($"Guest {email} could not join lobby {roomCode}");
                    var faultData = new ServiceFaultDto(ServiceErrorType.OperationFailed, "Failed to join as guest");
                    throw new FaultException<ServiceFaultDto>(faultData, new FaultReason("Operation Failed"));
                }

                RegisterPlayerCallback(roomCode, playerDto.idPlayer, callback);
                NotifyPlayersInLobby(roomCode, null, (cb) => cb.PlayerJoined(playerDto));

                Logger.Info($"Guest {email} (ID: {playerDto.idPlayer}) joined lobby {roomCode}");
                return playerDto;
            }
            catch (BusinessLogicException ex)
            {
                Logger.Debug($"Business logic error in JoinAndSubscribeAsGuestAsync. ErrorType: {ex.ErrorType}");
                var faultData = new ServiceFaultDto(ex.ErrorType, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.ErrorType.ToString()));
            }
            catch (FaultException<ServiceFaultDto>)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error in JoinAndSubscribeAsGuestAsync. Email: {email}, Room: {roomCode}, Type: {ex.GetType().Name}");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, INTERNAL_SERVER_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(INTERNAL_ERROR_REASON));
            }
        }

        public void LeaveAndUnsubscribe(string roomCode, int idPlayer)
        {
            InternalLeaveLobby(roomCode, idPlayer, isDisconnecting: false);
        }

        private void InternalLeaveLobby(string roomCode, int idPlayer, bool isDisconnecting)
        {
            try
            {
                bool isHost = lobbyLogic.LeaveLobbyAsync(roomCode, idPlayer).Result;

                if (!isDisconnecting)
                {
                    Task.Run(() => presenceManager.NotifyStatusChange(idPlayer, (int)PlayerStatus.Online));
                }

                if (isHost)
                {
                    NotifyPlayersInLobby(roomCode, idPlayer, (cb) => cb.HostLeft());
                    CleanupLobbyForHost(roomCode);
                }
                else
                {
                    NotifyPlayersInLobby(roomCode, idPlayer, (cb) => cb.PlayerLeft(idPlayer));
                    RemovePlayerFromLobby(roomCode, idPlayer);
                }

                Logger.Info($"Player {idPlayer} left lobby {roomCode}. IsHost: {isHost}, IsDisconnecting: {isDisconnecting}");
            }
            catch (BusinessLogicException ex)
            {
                Logger.Warn(ex, $"Business logic error in InternalLeaveLobby. Player: {idPlayer}, Room: {roomCode}, ErrorType: {ex.ErrorType}");
            }
            catch (AggregateException ex)
            {
                Logger.Error(ex, $"Aggregate error in InternalLeaveLobby. Player: {idPlayer}, Room: {roomCode}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error in InternalLeaveLobby. Player: {idPlayer}, Room: {roomCode}, Type: {ex.GetType().Name}");
            }
        }

        private void CleanupLobbyForHost(string roomCode)
        {
            if (lobbyCallbacks.TryGetValue(roomCode, out var callbacks))
            {
                foreach (var remainingPlayerId in callbacks.Keys)
                {
                    Task.Run(() => presenceManager.NotifyStatusChange(remainingPlayerId, (int)PlayerStatus.Online));
                }

                lobbyCallbacks.TryRemove(roomCode, out _);
                chatHistories.TryRemove(roomCode, out _);
                Logger.Debug($"Lobby {roomCode} cleaned up after host left");
            }
        }

        private void RemovePlayerFromLobby(string roomCode, int idPlayer)
        {
            if (lobbyCallbacks.TryGetValue(roomCode, out var callbacks))
            {
                callbacks.TryRemove(idPlayer, out _);

                if (callbacks.IsEmpty)
                {
                    lobbyCallbacks.TryRemove(roomCode, out _);
                    chatHistories.TryRemove(roomCode, out _);
                    Logger.Debug($"Lobby {roomCode} removed - no players remaining");
                }
            }
        }

        public Task SendMessageAsync(string roomCode, MessageDto message)
        {
            try
            {
                if (!chatHistories.ContainsKey(roomCode) || !lobbyCallbacks.ContainsKey(roomCode))
                {
                    Logger.Warn($"Cannot send message to lobby {roomCode} - lobby not found");
                    return Task.CompletedTask;
                }

                if (!string.IsNullOrEmpty(message.Message))
                {
                    message.Message = ProfanityFilter.CensorMessage(message.Message);
                }

                message.Timestamp = DateTime.UtcNow;
                chatHistories[roomCode].Add(message);

                NotifyPlayersInLobby(roomCode, null, (cb) => cb.MessageReceived(message));
            }
            catch (ArgumentException ex)
            {
                Logger.Warn(ex, $"Invalid message in room {roomCode}");
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error(ex, $"Invalid operation sending message in room {roomCode}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error sending message in room {roomCode}. Type: {ex.GetType().Name}");
            }

            return Task.CompletedTask;
        }

        public async Task SelectGamemodeAsync(string roomCode, int idGamemode)
        {
            try
            {
                await lobbyLogic.SelectGamemodeAsync(roomCode, idGamemode);
                NotifyPlayersInLobby(roomCode, null, (cb) => cb.NotifyGamemodeChanged(idGamemode));
                Logger.Info($"Gamemode {idGamemode} selected for lobby {roomCode}");
            }
            catch (BusinessLogicException ex)
            {
                Logger.Debug($"Business logic error selecting gamemode. ErrorType: {ex.ErrorType}");
                var faultData = new ServiceFaultDto(ex.ErrorType, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.ErrorType.ToString()));
            }
            catch (SqlException ex)
            {
                Logger.Error(ex, $"Database error selecting gamemode for room {roomCode}. SqlError: {ex.Number}");
                var faultData = new ServiceFaultDto(ServiceErrorType.DatabaseError, DATABASE_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(DATABASE_UNAVAILABLE_REASON));
            }
            catch (EntityException ex)
            {
                Logger.Error(ex, $"Entity framework error selecting gamemode for room {roomCode}");
                var faultData = new ServiceFaultDto(ServiceErrorType.DatabaseError, DATABASE_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(DATABASE_UNAVAILABLE_REASON));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error selecting gamemode for room {roomCode}. Type: {ex.GetType().Name}");
                var faultInternal = new ServiceFaultDto(ServiceErrorType.ServerInternalError, INTERNAL_SERVER_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(faultInternal, new FaultReason(INTERNAL_ERROR_REASON));
            }
        }

        public async Task StartGameAsync(string roomCode)
        {
            try
            {
                await VerifyLobbyParticipantsAsync(roomCode);
                await lobbyLogic.StartGameAsync(roomCode);
                await UpdatePlayersStatusToInGameAsync(roomCode);
                NotifyPlayersInLobby(roomCode, null, (cb) => cb.NotifyGameStarting());

                Logger.Info($"Game started for lobby {roomCode}");
            }
            catch (BusinessLogicException ex)
            {
                Logger.Debug($"Business logic error starting game. ErrorType: {ex.ErrorType}");
                var faultData = new ServiceFaultDto(ex.ErrorType, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.ErrorType.ToString()));
            }
            catch (FaultException<ServiceFaultDto>)
            {
                throw;
            }
            catch (SqlException ex)
            {
                Logger.Error(ex, $"Database error starting game in lobby {roomCode}. SqlError: {ex.Number}");
                var faultData = new ServiceFaultDto(ServiceErrorType.DatabaseError, DATABASE_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(DATABASE_UNAVAILABLE_REASON));
            }
            catch (EntityException ex)
            {
                Logger.Error(ex, $"Entity framework error starting game in lobby {roomCode}");
                var faultData = new ServiceFaultDto(ServiceErrorType.DatabaseError, DATABASE_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(DATABASE_UNAVAILABLE_REASON));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error starting game in lobby {roomCode}. Type: {ex.GetType().Name}");
                var faultInternal = new ServiceFaultDto(ServiceErrorType.ServerInternalError, INTERNAL_SERVER_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(faultInternal, new FaultReason(INTERNAL_ERROR_REASON));
            }
        }

        private static async Task VerifyLobbyParticipantsAsync(string roomCode)
        {
            if (!lobbyCallbacks.TryGetValue(roomCode, out var participants))
            {
                Logger.Error($"Lobby {roomCode} not found in memory during verification");
                var faultData = new ServiceFaultDto(ServiceErrorType.LobbyNotFound, LOBBY_NOT_FOUND_MESSAGE);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(LOBBY_NOT_FOUND_REASON));
            }

            Logger.Debug($"All participants in room {roomCode} verified successfully");
            await Task.CompletedTask;
        }

        private async Task UpdatePlayersStatusToInGameAsync(string roomCode)
        {
            if (lobbyCallbacks.TryGetValue(roomCode, out var validParticipants))
            {
                foreach (var playerId in validParticipants.Keys)
                {
                    await presenceManager.NotifyStatusChange(playerId, (int)PlayerStatus.InGame);
                }
            }
        }

        private void NotifyPlayersInLobby(string roomCode, int? idPlayerToExclude, Action<ILobbyCallback> action)
        {
            if (!lobbyCallbacks.TryGetValue(roomCode, out var callbacks))
            {
                return;
            }

            foreach (var entry in callbacks)
            {
                int playerId = entry.Key;
                ILobbyCallback callback = entry.Value;

                if (idPlayerToExclude.HasValue && playerId == idPlayerToExclude.Value)
                {
                    continue;
                }

                Task.Run(() => ExecuteSafeNotification(roomCode, playerId, callback, action));
            }
        }

        private void ExecuteSafeNotification(string roomCode, int playerId, ILobbyCallback callback, Action<ILobbyCallback> action)
        {
            try
            {
                var commObj = callback as ICommunicationObject;

                if (commObj != null && (commObj.State == CommunicationState.Closed || commObj.State == CommunicationState.Faulted))
                {
                    HandleClientDisconnect(roomCode, playerId);
                    return;
                }

                action(callback);
            }
            catch (CommunicationException ex)
            {
                Logger.Warn(ex, $"Communication error notifying player {playerId} in lobby {roomCode}");
                HandleClientDisconnect(roomCode, playerId);
            }
            catch (TimeoutException ex)
            {
                Logger.Warn(ex, $"Timeout notifying player {playerId} in lobby {roomCode}");
                HandleClientDisconnect(roomCode, playerId);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error notifying player {playerId} in lobby {roomCode}. Type: {ex.GetType().Name}");
                HandleClientDisconnect(roomCode, playerId);
            }
        }

        public async Task KickPlayerAsync(string roomCode, int idRequestingPlayer, int idPlayerToKick)
        {
            try
            {
                await lobbyLogic.KickPlayerAsync(roomCode, idRequestingPlayer, idPlayerToKick);
                await presenceManager.NotifyStatusChange(idPlayerToKick, (int)PlayerStatus.Online);

                if (lobbyCallbacks.TryGetValue(roomCode, out var roomCallbacks) &&
                    roomCallbacks.TryRemove(idPlayerToKick, out var kickedClientCallback))
                {
                    NotifyKickedPlayer(kickedClientCallback, idPlayerToKick);
                }

                NotifyPlayersInLobby(roomCode, null, (cb) => cb.PlayerLeft(idPlayerToKick));
                Logger.Info($"Player {idPlayerToKick} kicked from lobby {roomCode} by player {idRequestingPlayer}");
            }
            catch (BusinessLogicException ex)
            {
                Logger.Debug($"Business logic error kicking player. ErrorType: {ex.ErrorType}");
                var faultData = new ServiceFaultDto(ex.ErrorType, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.ErrorType.ToString()));
            }
            catch (SqlException ex)
            {
                Logger.Error(ex, $"Database error kicking player {idPlayerToKick} from room {roomCode}. SqlError: {ex.Number}");
                var faultData = new ServiceFaultDto(ServiceErrorType.DatabaseError, DATABASE_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(DATABASE_UNAVAILABLE_REASON));
            }
            catch (EntityException ex)
            {
                Logger.Error(ex, $"Entity framework error kicking player {idPlayerToKick} from room {roomCode}");
                var faultData = new ServiceFaultDto(ServiceErrorType.DatabaseError, DATABASE_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(DATABASE_UNAVAILABLE_REASON));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error kicking player {idPlayerToKick} from room {roomCode}. Type: {ex.GetType().Name}");
                var faultInternal = new ServiceFaultDto(ServiceErrorType.ServerInternalError, INTERNAL_SERVER_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(faultInternal, new FaultReason(INTERNAL_ERROR_REASON));
            }
        }

        private void NotifyKickedPlayer(ILobbyCallback kickedClientCallback, int idPlayerToKick)
        {
            try
            {
                kickedClientCallback.YouWereKicked();
                Logger.Debug($"Kick notification sent to player {idPlayerToKick}");
            }
            catch (CommunicationException ex)
            {
                Logger.Warn(ex, $"Communication error notifying kicked player {idPlayerToKick}");
            }
            catch (TimeoutException ex)
            {
                Logger.Warn(ex, $"Timeout notifying kicked player {idPlayerToKick}");
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, $"Could not notify kicked player {idPlayerToKick}. Type: {ex.GetType().Name}");
            }
        }

        private void HandleClientDisconnect(string roomCode, int idPlayer)
        {
            try
            {
                Logger.Info($"Handling abrupt disconnect (Closed/Faulted) for player {idPlayer} in room {roomCode}");
                InternalLeaveLobby(roomCode, idPlayer, isDisconnecting: true);
            }
            catch (BusinessLogicException ex)
            {
                Logger.Warn(ex, $"Business logic error handling disconnect for player {idPlayer} in room {roomCode}. ErrorType: {ex.ErrorType}");
            }
            catch (AggregateException ex)
            {
                Logger.Error(ex, $"Aggregate error handling disconnect for player {idPlayer} in room {roomCode}");
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error(ex, $"Invalid operation handling disconnect for player {idPlayer} in room {roomCode}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error handling disconnect for player {idPlayer} in room {roomCode}. Type: {ex.GetType().Name}");
            }
        }
    }
}
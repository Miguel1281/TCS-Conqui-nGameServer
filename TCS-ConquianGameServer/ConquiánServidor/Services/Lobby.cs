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
                var faultData = new ServiceFaultDto(ex.ErrorType, LOGIC_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.ErrorType.ToString()));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error crítico en GetLobbyStateAsync {roomCode}");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, INTERNAL_SERVER_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(INTERNAL_ERROR_REASON));
            }
        }

        public async Task<string> CreateLobbyAsync(int idHostPlayer)
        {
            try
            {
                string newRoomCode = await lobbyLogic.CreateLobbyAsync(idHostPlayer);
                if (newRoomCode != null)
                {
                    chatHistories.TryAdd(newRoomCode, new List<MessageDto>());
                    lobbyCallbacks.TryAdd(newRoomCode, new ConcurrentDictionary<int, ILobbyCallback>());
                    await presenceManager.NotifyStatusChange(idHostPlayer, (int)PlayerStatus.InLobby);
                }
                return newRoomCode;
            }
            catch (BusinessLogicException ex)
            {
                var faultData = new ServiceFaultDto(ex.ErrorType, LOGIC_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.ErrorType.ToString()));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error creando lobby para host {idHostPlayer}");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, INTERNAL_SERVER_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(INTERNAL_ERROR_REASON));
            }
        }

        public async Task<bool> JoinAndSubscribeAsync(string roomCode, int idPlayer)
        {
            var callback = OperationContext.Current.GetCallbackChannel<ILobbyCallback>();

            try
            {
                if (!lobbyCallbacks.ContainsKey(roomCode))
                {
                    var faultData = new ServiceFaultDto(ServiceErrorType.NotFound, LOBBY_NOT_FOUND_MESSAGE);
                    throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(LOBBY_NOT_FOUND_REASON));
                }

                var playerDto = await lobbyLogic.JoinLobbyAsync(roomCode, idPlayer);
                if (playerDto == null) return false;

                lobbyCallbacks[roomCode][idPlayer] = callback;

                if (callback is ICommunicationObject communicationObject)
                {
                    communicationObject.Closed += (sender, args) => HandleClientDisconnect(roomCode, idPlayer);
                    communicationObject.Faulted += (sender, args) => HandleClientDisconnect(roomCode, idPlayer);
                }

                await presenceManager.NotifyStatusChange(idPlayer, (int)PlayerStatus.InLobby);
                NotifyPlayersInLobby(roomCode, null, (cb) => cb.PlayerJoined(playerDto));
                return true;
            }
            catch (BusinessLogicException ex)
            {
                var faultData = new ServiceFaultDto(ex.ErrorType, LOGIC_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.ErrorType.ToString()));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error en JoinAndSubscribeAsync room {roomCode} player {idPlayer}");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, INTERNAL_SERVER_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(INTERNAL_ERROR_REASON));
            }
        }

        public async Task<PlayerDto> JoinAndSubscribeAsGuestAsync(string email, string roomCode)
        {
            var callback = OperationContext.Current.GetCallbackChannel<ILobbyCallback>();

            try
            {
                if (!lobbyCallbacks.ContainsKey(roomCode))
                {
                    var faultData = new ServiceFaultDto(ServiceErrorType.NotFound, LOBBY_NOT_FOUND_MESSAGE);
                    throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(LOBBY_NOT_FOUND_REASON));
                }

                var playerDto = await lobbyLogic.JoinLobbyAsGuestAsync(email, roomCode);

                if (playerDto == null)
                {
                    var faultData = new ServiceFaultDto(ServiceErrorType.OperationFailed, "Failed to join as guest");
                    throw new FaultException<ServiceFaultDto>(faultData, new FaultReason("Operation Failed"));
                }

                lobbyCallbacks[roomCode][playerDto.idPlayer] = callback;

                if (callback is ICommunicationObject communicationObject)
                {
                    communicationObject.Closed += (sender, args) => HandleClientDisconnect(roomCode, playerDto.idPlayer);
                    communicationObject.Faulted += (sender, args) => HandleClientDisconnect(roomCode, playerDto.idPlayer);
                }

                NotifyPlayersInLobby(roomCode, null, (cb) => cb.PlayerJoined(playerDto));
                return playerDto;
            }
            catch (BusinessLogicException ex)
            {
                var faultData = new ServiceFaultDto(ex.ErrorType, LOGIC_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.ErrorType.ToString()));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error crítico en JoinAndSubscribeAsGuestAsync room {roomCode} email {email}");
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
                }
                else
                {
                    NotifyPlayersInLobby(roomCode, idPlayer, (cb) => cb.PlayerLeft(idPlayer));
                }

                if (lobbyCallbacks.TryGetValue(roomCode, out var callbacks))
                {
                    callbacks.TryRemove(idPlayer, out _);

                    if (callbacks.IsEmpty || isHost)
                    {
                        if (isHost)
                        {
                            foreach (var remainingPlayerId in callbacks.Keys)
                            {
                                Task.Run(() => presenceManager.NotifyStatusChange(remainingPlayerId, (int)PlayerStatus.Online));
                            }
                        }

                        lobbyCallbacks.TryRemove(roomCode, out _);
                        chatHistories.TryRemove(roomCode, out _);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error en LeaveAndUnsubscribe room {roomCode}");
            }
        }

        public Task SendMessageAsync(string roomCode, MessageDto message)
        {
            try
            {
                if (chatHistories.ContainsKey(roomCode) && lobbyCallbacks.ContainsKey(roomCode))
                {

                    if (!string.IsNullOrEmpty(message.Message))
                    {
                        message.Message = ProfanityFilter.CensorMessage(message.Message);
                    }

                    message.Timestamp = DateTime.UtcNow;
                    chatHistories[roomCode].Add(message);

                    NotifyPlayersInLobby(roomCode, null, (cb) => cb.MessageReceived(message));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error sending chat message.");
            }
            return Task.CompletedTask;
        }

        public async Task SelectGamemodeAsync(string roomCode, int idGamemode)
        {
            try
            {
                await lobbyLogic.SelectGamemodeAsync(roomCode, idGamemode);
                NotifyPlayersInLobby(roomCode, null, (cb) => cb.NotifyGamemodeChanged(idGamemode));
            }
            catch (BusinessLogicException ex)
            {
                var faultData = new ServiceFaultDto(ex.ErrorType, LOGIC_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.ErrorType.ToString()));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error seleccionando modo de juego");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, INTERNAL_SERVER_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(INTERNAL_ERROR_REASON));
            }
        }

        public async Task StartGameAsync(string roomCode)
        {
            try
            {
                await lobbyLogic.StartGameAsync(roomCode);
                if (lobbyCallbacks.TryGetValue(roomCode, out var participants))
                {
                    foreach (var playerId in participants.Keys)
                    {
                        await presenceManager.NotifyStatusChange(playerId, (int)PlayerStatus.InGame);
                    }
                }
                NotifyPlayersInLobby(roomCode, null, (cb) => cb.NotifyGameStarting());
            }
            catch (BusinessLogicException ex)
            {
                var faultData = new ServiceFaultDto(ex.ErrorType, LOGIC_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.ErrorType.ToString()));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error al iniciar juego en lobby {roomCode}");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, INTERNAL_SERVER_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(INTERNAL_ERROR_REASON));
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
            catch (Exception ex)
            {
                Logger.Warn(ex, "error during player notification in lobby.");
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
                    try
                    {
                        kickedClientCallback.YouWereKicked();
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, "Could not notify kicked player (connection might be lost).");
                    }
                }

                NotifyPlayersInLobby(roomCode, null, (cb) => cb.PlayerLeft(idPlayerToKick));
            }
            catch (BusinessLogicException ex)
            {
                var faultData = new ServiceFaultDto(ex.ErrorType, LOGIC_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.ErrorType.ToString()));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error kicking player {idPlayerToKick} from room {roomCode}");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, INTERNAL_SERVER_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(INTERNAL_ERROR_REASON));
            }
        }

        private void HandleClientDisconnect(string roomCode, int idPlayer)
        {
            try
            {
                Logger.Info($"Detectada desconexión abrupta (Closed/Faulted) del jugador {idPlayer} en sala {roomCode}");
                InternalLeaveLobby(roomCode, idPlayer, isDisconnecting: true);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error manejando desconexión abrupta del jugador {idPlayer}");
            }
        }
    }
}
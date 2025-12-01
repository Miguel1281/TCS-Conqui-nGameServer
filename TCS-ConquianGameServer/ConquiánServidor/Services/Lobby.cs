using Autofac;
using ConquiánServidor.BusinessLogic;
using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.ConquiánDB.Repositories;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.DataAccess.Repositories;
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

        private readonly LobbyLogic lobbyLogic;

        private const string LOGIC_ERROR_MESSAGE = "Logic Error";
        private const string INTERNAL_SERVER_ERROR_MESSAGE = "Internal Server Error";
        private const string INTERNAL_ERROR_REASON = "Internal Error";
        private const string LOBBY_NOT_FOUND_MESSAGE = "Lobby not found in memory";
        private const string LOBBY_NOT_FOUND_REASON = "Lobby Not Found";

        public Lobby()
        {
            Bootstrapper.Init();
            this.lobbyLogic = Bootstrapper.Container.Resolve<LobbyLogic>();
        }

        public Lobby(LobbyLogic lobbyLogic)
        {
            this.lobbyLogic = lobbyLogic;
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
            try
            {
                bool isHost = lobbyLogic.LeaveLobbyAsync(roomCode, idPlayer).Result;

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
                    message.Timestamp = DateTime.UtcNow;
                    chatHistories[roomCode].Add(message);
                    NotifyPlayersInLobby(roomCode, null, (cb) => cb.MessageReceived(message));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error enviando mensaje de chat.");
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

        private static void NotifyPlayersInLobby(string roomCode, int? idPlayerToExclude, Action<ILobbyCallback> action)
        {
            if (!lobbyCallbacks.TryGetValue(roomCode, out var callbacks))
            {
                return;
            }

            List<int> disconnectedPlayers = new List<int>();

            foreach (var entry in callbacks)
            {
                if (idPlayerToExclude.HasValue && entry.Key == idPlayerToExclude.Value)
                {
                    continue;
                }

                try
                {
                    action(entry.Value);
                }
                catch (Exception)
                {
                    disconnectedPlayers.Add(entry.Key);
                }
            }

            foreach (var id in disconnectedPlayers)
            {
                callbacks.TryRemove(id, out _);
            }
        }

        public async Task KickPlayerAsync(string roomCode, int idRequestingPlayer, int idPlayerToKick)
        {
            try
            {
                await lobbyLogic.KickPlayerAsync(roomCode, idRequestingPlayer, idPlayerToKick);

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
    }
}
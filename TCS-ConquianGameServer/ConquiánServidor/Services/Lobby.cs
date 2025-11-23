using ConquiánServidor.BusinessLogic;
using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.ConquiánDB.Repositories;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.FaultContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.DataAccess.Repositories;
using ConquiánServidor.Properties.Langs; 
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

        public Lobby()
        {
            var dbContext = new ConquiánDBEntities();
            IPlayerRepository playerRepository = new PlayerRepository(dbContext);
            ILobbyRepository lobbyRepository = new LobbyRepository(dbContext);
            lobbyLogic = new LobbyLogic(lobbyRepository, playerRepository, dbContext);
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
            catch (InvalidOperationException ex)
            {
                Logger.Warn(ex, $"Error lógico en GetLobbyStateAsync: {ex.Message}");
                var faultData = new ServiceFaultDto(ServiceErrorType.OperationFailed, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.Message));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error crítico en GetLobbyStateAsync {roomCode}");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, Lang.ErrorLobbyAction);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(Lang.InternalServerError));
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
            catch (ArgumentException ex)
            {
                var faultData = new ServiceFaultDto(ServiceErrorType.NotFound, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.Message));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error creando lobby para host {idHostPlayer}");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, Lang.ErrorLobbyAction);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(Lang.InternalServerError));
            }
        }

        public async Task<bool> JoinAndSubscribeAsync(string roomCode, int idPlayer)
        {
            var callback = OperationContext.Current.GetCallbackChannel<ILobbyCallback>();

            try
            {
                if (!lobbyCallbacks.ContainsKey(roomCode))
                {
                    throw new InvalidOperationException(Lang.ErrorLobbyNotFound);
                }

                var playerDto = await lobbyLogic.JoinLobbyAsync(roomCode, idPlayer);
                if (playerDto == null) return false;

                lobbyCallbacks[roomCode][idPlayer] = callback;

                NotifyPlayersInLobby(roomCode, null, (cb) => cb.PlayerJoined(playerDto));
                return true;
            }
            catch (InvalidOperationException ex)
            {
                var faultData = new ServiceFaultDto(ServiceErrorType.OperationFailed, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.Message));
            }
            catch (ArgumentException ex)
            {
                var faultData = new ServiceFaultDto(ServiceErrorType.NotFound, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.Message));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error en JoinAndSubscribeAsync room {roomCode} player {idPlayer}");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, Lang.ErrorLobbyAction);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(Lang.InternalServerError));
            }
        }

        public async Task<PlayerDto> JoinAndSubscribeAsGuestAsync(string email, string roomCode)
        {
            var callback = OperationContext.Current.GetCallbackChannel<ILobbyCallback>();

            try
            {
                if (!lobbyCallbacks.ContainsKey(roomCode))
                {
                    throw new InvalidOperationException(Lang.ErrorLobbyNotFound);
                }

                var playerDto = await lobbyLogic.JoinLobbyAsGuestAsync(email, roomCode);

                if (playerDto == null)
                {
                    throw new InvalidOperationException(Lang.ErrorLobbyAction);
                }

                lobbyCallbacks[roomCode][playerDto.idPlayer] = callback;
                NotifyPlayersInLobby(roomCode, null, (cb) => cb.PlayerJoined(playerDto));
                return playerDto;
            }

            catch (FaultException<ServiceFaultDto> ex)
            {
                throw ex;
            }
            catch (ArgumentException ex)
            {
                var faultData = new ServiceFaultDto(ServiceErrorType.ValidationFailed, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                var faultData = new ServiceFaultDto(ServiceErrorType.OperationFailed, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.Message));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error crítico en JoinAndSubscribeAsGuestAsync room {roomCode} email {email}");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, Lang.ErrorLobbyAction);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(Lang.InternalServerError));
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
            catch (InvalidOperationException ex)
            {
                var faultData = new ServiceFaultDto(ServiceErrorType.OperationFailed, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.Message));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error seleccionando modo de juego");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, Lang.ErrorLobbyAction);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(Lang.InternalServerError));
            }
        }

        public async Task StartGameAsync(string roomCode)
        {
            try
            {
                await lobbyLogic.StartGameAsync(roomCode);
                NotifyPlayersInLobby(roomCode, null, (cb) => cb.NotifyGameStarting());
            }
            catch (InvalidOperationException ex)
            {
                var faultData = new ServiceFaultDto(ServiceErrorType.OperationFailed, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.Message));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error al iniciar juego en lobby {roomCode}");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, Lang.ErrorLobbyAction);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(Lang.InternalServerError));
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

                if (lobbyCallbacks.TryGetValue(roomCode, out var roomCallbacks))
                {
                    if (roomCallbacks.TryRemove(idPlayerToKick, out var kickedClientCallback))
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
                }

                NotifyPlayersInLobby(roomCode, null, (cb) => cb.PlayerLeft(idPlayerToKick));
            }
            catch (UnauthorizedAccessException ex)
            {
                var faultData = new ServiceFaultDto(ServiceErrorType.OperationFailed, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                var faultData = new ServiceFaultDto(ServiceErrorType.OperationFailed, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.Message));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error kicking player {idPlayerToKick} from room {roomCode}");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, Lang.ErrorLobbyAction);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(Lang.InternalServerError));
            }
        }
    }
}
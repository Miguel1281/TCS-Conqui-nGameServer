using ConquiánServidor.BusinessLogic;
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.DataAccess.Repositories;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class Lobby : ILobby
    {
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<int, ILobbyCallback>> lobbyCallbacks =
            new ConcurrentDictionary<string, ConcurrentDictionary<int, ILobbyCallback>>();

        private static readonly ConcurrentDictionary<string, List<MessageDto>> chatHistories =
            new ConcurrentDictionary<string, List<MessageDto>>();

        private readonly LobbyLogic lobbyLogic;
        private readonly ConquiánDBEntities dbContext; 

        private string currentRoomCode;
        private int currentPlayerId;
        private ILobbyCallback currentCallback;

        public Lobby()
        {
            dbContext = new ConquiánDBEntities();
            IPlayerRepository playerRepository = new PlayerRepository(dbContext);
            ILobbyRepository lobbyRepository = new LobbyRepository(dbContext);
            lobbyLogic = new LobbyLogic(lobbyRepository, playerRepository);
        }

        public async Task<LobbyDto> GetLobbyStateAsync(string roomCode)
        {
            try
            {
                var lobbyState = await lobbyLogic.GetLobbyStateAsync(roomCode);
                if (lobbyState != null)
                {
                    lobbyState.ChatMessages = chatHistories.ContainsKey(roomCode) ? chatHistories[roomCode] : new List<MessageDto>();
                }
                return lobbyState;
            }
            catch (Exception ex)
            {
                // TODO: log del error
                return null;
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
            catch (Exception ex)
            {
                // TODO: log del error
                return null;
            }
        }

        public async Task<bool> JoinAndSubscribeAsync(string roomCode, int idPlayer)
        {
            var callback = OperationContext.Current.GetCallbackChannel<ILobbyCallback>();

            try
            {
                if (!lobbyCallbacks.ContainsKey(roomCode))
                {
                    return false; 
                }

                var playerDto = await lobbyLogic.JoinLobbyAsync(roomCode, idPlayer);
                if (playerDto == null)
                {
                    return false; 
                }

                lobbyCallbacks[roomCode][idPlayer] = callback;
                this.currentCallback = callback;
                this.currentRoomCode = roomCode;
                this.currentPlayerId = idPlayer;

                NotifyPlayersInLobby(roomCode, null, (cb) => cb.PlayerJoined(playerDto));
                return true;
            }
            catch (Exception ex)
            {
                // TODO: log del error
                return false;
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
                this.currentCallback = null;
                this.currentRoomCode = null;
                this.currentPlayerId = 0;
            }
            catch (Exception ex)
            {
                // TODO: log del error
            }
        }

        public Task SendMessageAsync(string roomCode, MessageDto message)
        {
            if (chatHistories.ContainsKey(roomCode) && lobbyCallbacks.ContainsKey(roomCode))
            {
                message.Timestamp = DateTime.UtcNow;
                chatHistories[roomCode].Add(message);
                NotifyPlayersInLobby(roomCode, null, (cb) => cb.MessageReceived(message));
            }
            return Task.CompletedTask;
        }

        private void NotifyPlayersInLobby(string roomCode, int? idPlayerToExclude, Action<ILobbyCallback> action)
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

    }
}
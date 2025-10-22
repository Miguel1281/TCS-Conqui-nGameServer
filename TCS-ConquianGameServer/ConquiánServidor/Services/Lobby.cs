using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.ServiceModel;
using System.Text;
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

        private string currentRoomCode;
        private int currentPlayerId;
        private ILobbyCallback currentCallback; 
        public async Task<LobbyDto> GetLobbyStateAsync(string roomCode)
        {
            try
            {
                using (var context = new ConquiánDBEntities())
                {
                    var lobby = await context.Lobby
                        .Include(l => l.Player1)
                        .Include(l => l.StatusLobby)
                        .FirstOrDefaultAsync(l => l.roomCode == roomCode);

                    if (lobby != null)
                    {
                        var lobbyState = new LobbyDto
                        {
                            RoomCode = lobby.roomCode,
                            idHostPlayer = lobby.idHostPlayer,
                            StatusLobby = lobby.StatusLobby.statusName,
                            Players = lobby.Player1.Select(p => new PlayerDto
                            {
                                idPlayer = p.idPlayer,
                                nickname = p.nickname,
                                pathPhoto = p.pathPhoto
                            }).ToList(),
                            ChatMessages = chatHistories.ContainsKey(roomCode) ? chatHistories[roomCode] : new List<MessageDto>()
                        };
                        return lobbyState;
                    }
                }
            }
            catch (Exception ex)
            {
                // TODO: log del error
            }
            return null;
        }

        public async Task<string> CreateLobbyAsync(int idHostPlayer)
        {
            try
            {
                using (var context = new ConquiánDBEntities())
                {
                    var hostPlayer = await context.Player.FindAsync(idHostPlayer);
                    if (hostPlayer == null)
                    {
                        return null;
                    }

                    string newRoomCode;
                    do
                    {
                        newRoomCode = GenerateRandomCode();
                    }
                    while (await context.Lobby.AnyAsync(l => l.roomCode == newRoomCode));

                    var newLobby = new ConquiánDB.Lobby()
                    {
                        roomCode = newRoomCode,
                        idHostPlayer = idHostPlayer,
                        idStatusLobby = 1,
                        creationDate = DateTime.UtcNow
                    };

                    newLobby.Player1.Add(hostPlayer);

                    context.Lobby.Add(newLobby);
                    await context.SaveChangesAsync();

                    chatHistories.TryAdd(newRoomCode, new List<MessageDto>());
                    lobbyCallbacks.TryAdd(newRoomCode, new ConcurrentDictionary<int, ILobbyCallback>());

                    return newRoomCode;
                }
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
                using (var context = new ConquiánDBEntities())
                {
                    var lobby = await context.Lobby.Include(l => l.Player1)
                        .FirstOrDefaultAsync(l => l.roomCode == roomCode);
                    var playerToJoin = await context.Player.FindAsync(idPlayer);

                    if (lobby == null || playerToJoin == null || !lobbyCallbacks.ContainsKey(roomCode))
                    {
                        return false; 
                    }
                   
                    bool isAlreadyInLobby = lobby.Player1.Any(p => p.idPlayer == idPlayer);

                    if (lobby.Player1.Count >= 2 && !isAlreadyInLobby)
                    {
                        return false;
                    }
                    if (lobby.idStatusLobby != 1)
                    {
                        return false;
                    }

                    if (!isAlreadyInLobby)
                    {
                        lobby.Player1.Add(playerToJoin);
                        await context.SaveChangesAsync();
                    }

  
                    lobbyCallbacks[roomCode][idPlayer] = callback;
                    this.currentCallback = callback;
                    this.currentRoomCode = roomCode;
                    this.currentPlayerId = idPlayer;

                    var playerDto = new PlayerDto
                    {
                        idPlayer = playerToJoin.idPlayer,
                        nickname = playerToJoin.nickname,
                        pathPhoto = playerToJoin.pathPhoto
                    };

                    NotifyPlayersInLobby(roomCode, null, (cb) => cb.PlayerJoined(playerDto));
                    return true;
                }
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
                bool isHost = false;
                using (var context = new ConquiánDBEntities())
                {
                    var lobby = context.Lobby.Include(l => l.Player1)
                        .FirstOrDefault(l => l.roomCode == roomCode);

                    if (lobby == null) return;

                    isHost = lobby.idHostPlayer == idPlayer;

                    if (isHost)
                    {
                        lobby.idStatusLobby = 3;
                    }
                    else
                    {
                        var playerToRemove = lobby.Player1.FirstOrDefault(p => p.idPlayer == idPlayer);
                        if (playerToRemove != null)
                        {
                            lobby.Player1.Remove(playerToRemove);
                        }
                    }
                    context.SaveChanges();
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

                    if (callbacks.IsEmpty)
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

        private string GenerateRandomCode(int length = 5)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
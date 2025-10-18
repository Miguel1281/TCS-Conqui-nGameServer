using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConquiánServidor.Services
{
    public class Lobby : ILobby
    {
        private static readonly ConcurrentDictionary<string, List<MessageDto>> chatHistories = new ConcurrentDictionary<string, List<MessageDto>>();
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

                    return newRoomCode;
                }
            }
            catch (Exception ex)
            {
                // TODO: log del error
                return null;
            }
        }
        public async Task<bool> JoinLobbyAsync(string roomCode, int idPlayer)
        {
            try
            {
                using (var context = new ConquiánDBEntities())
                {
                    var lobby = await context.Lobby.Include(l => l.Player1)
                        .FirstOrDefaultAsync(l => l.roomCode == roomCode);
                    var playerToJoin = await context.Player.FindAsync(idPlayer);

                    if (lobby == null || playerToJoin == null)
                    {
                        return false;
                    }
                    if (lobby.Player1.Count >= 2)
                    {
                        return false;
                    }
                    if (lobby.idStatusLobby != 1)
                    {
                        return false;
                    }
                    if (lobby.Player1.Any(p => p.idPlayer == idPlayer))
                    {
                        return true;
                    }

                    lobby.Player1.Add(playerToJoin);
                    await context.SaveChangesAsync();

                    return true;
                }
            }
            catch (Exception ex)
            {
                // TODO: log del error
                return false;
            }
        }

        public async Task LeaveLobbyAsync(string roomCode, int idPlayer)
        {
            try
            {
                using (var context = new ConquiánDBEntities())
                {
                    var lobby = await context.Lobby.Include(l => l.Player1)
                        .FirstOrDefaultAsync(l => l.roomCode == roomCode);

                    if (lobby == null) return;

                    if (lobby.idHostPlayer == idPlayer)
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
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // TODO: log del error
            }
        }
        public Task SendMessageAsync(string roomCode, MessageDto message)
        {
            if (chatHistories.ContainsKey(roomCode))
            {
                message.Timestamp = DateTime.UtcNow;
                chatHistories[roomCode].Add(message);
            }
            return Task.CompletedTask;
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
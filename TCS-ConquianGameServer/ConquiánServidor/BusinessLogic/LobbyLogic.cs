using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.DataAccess.Abstractions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic
{
    public class LobbyLogic
    {
        private readonly ILobbyRepository lobbyRepository;
        private readonly IPlayerRepository playerRepository;

        public LobbyLogic(ILobbyRepository lobbyRepository, IPlayerRepository playerRepository)
        {
            this.lobbyRepository = lobbyRepository;
            this.playerRepository = playerRepository;
        }

        public async Task<LobbyDto> GetLobbyStateAsync(string roomCode)
        {
            var lobby = await lobbyRepository.GetLobbyByRoomCodeAsync(roomCode);

            if (lobby != null)
            {
                return new LobbyDto
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
                    ChatMessages = new System.Collections.Generic.List<MessageDto>() 
                };
            }
            return null;
        }

        public async Task<string> CreateLobbyAsync(int idHostPlayer)
        {
            var hostPlayer = await playerRepository.GetPlayerByIdAsync(idHostPlayer);
            if (hostPlayer == null)
            {
                return null;
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
                creationDate = DateTime.UtcNow
            };

            newLobby.Player1.Add(hostPlayer);
            lobbyRepository.AddLobby(newLobby);
            await lobbyRepository.SaveChangesAsync();

            return newRoomCode;
        }

        public async Task<PlayerDto> JoinLobbyAsync(string roomCode, int idPlayer)
        {
            var lobby = await lobbyRepository.GetLobbyByRoomCodeAsync(roomCode);
            var playerToJoin = await playerRepository.GetPlayerByIdAsync(idPlayer);

            if (lobby == null || playerToJoin == null)
            {
                return null; 
            }

            bool isAlreadyInLobby = lobby.Player1.Any(p => p.idPlayer == idPlayer);

            if (lobby.Player1.Count >= 2 && !isAlreadyInLobby)
            {
                return null; 
            }
            if (lobby.idStatusLobby != 1) 
            {
                return null; 
            }

            if (!isAlreadyInLobby)
            {
                lobby.Player1.Add(playerToJoin);
                await lobbyRepository.SaveChangesAsync();
            }

            return new PlayerDto
            {
                idPlayer = playerToJoin.idPlayer,
                nickname = playerToJoin.nickname,
                pathPhoto = playerToJoin.pathPhoto
            };
        }

        public async Task<bool> LeaveLobbyAsync(string roomCode, int idPlayer)
        {
            var lobby = await lobbyRepository.GetLobbyByRoomCodeAsync(roomCode);
            if (lobby == null) return false;

            bool isHost = lobby.idHostPlayer == idPlayer;

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
            await lobbyRepository.SaveChangesAsync();
            return isHost;
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
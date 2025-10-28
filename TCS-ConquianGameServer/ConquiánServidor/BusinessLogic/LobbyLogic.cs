using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.DataAccess.Abstractions;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic
{
    public class LobbyLogic
    {
        private readonly ILobbyRepository lobbyRepository;
        private readonly IPlayerRepository playerRepository;
        private static readonly RandomNumberGenerator randomGenerator = RandomNumberGenerator.Create();

        public LobbyLogic(ILobbyRepository lobbyRepository, IPlayerRepository playerRepository)
        {
            this.lobbyRepository = lobbyRepository;
            this.playerRepository = playerRepository;
        }

        public async Task<LobbyDto> GetLobbyStateAsync(string roomCode)
        {
            LobbyDto lobbyDto = new LobbyDto(); 
            var lobby = await lobbyRepository.GetLobbyByRoomCodeAsync(roomCode);

            if (lobby != null)
            {
                lobbyDto = new LobbyDto
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
            return lobbyDto; 
        }

        public async Task<string> CreateLobbyAsync(int idHostPlayer)
        {
            string newRoomCode = string.Empty; 
            var hostPlayer = await playerRepository.GetPlayerByIdAsync(idHostPlayer);

            if (hostPlayer != null)
            {
                string generatedCode;
                do
                {
                    generatedCode = GenerateRandomCode();
                }
                while (await lobbyRepository.DoesRoomCodeExistAsync(generatedCode));

                newRoomCode = generatedCode; 

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
            }

            return newRoomCode; 
        }

        public async Task<PlayerDto> JoinLobbyAsync(string roomCode, int idPlayer)
        {
            PlayerDto playerDto = new PlayerDto(); 
            var lobby = await lobbyRepository.GetLobbyByRoomCodeAsync(roomCode);
            var playerToJoin = await playerRepository.GetPlayerByIdAsync(idPlayer);

            if (lobby != null && playerToJoin != null)
            {
                bool isAlreadyInLobby = lobby.Player1.Any(p => p.idPlayer == idPlayer);
                bool isFull = lobby.Player1.Count >= 2;
                bool isOpen = lobby.idStatusLobby == 1;

                if ((!isFull && isOpen) || isAlreadyInLobby)
                {
                    if (!isAlreadyInLobby)
                    {
                        lobby.Player1.Add(playerToJoin);
                        await lobbyRepository.SaveChangesAsync();
                    }

                    playerDto = new PlayerDto
                    {
                        idPlayer = playerToJoin.idPlayer,
                        nickname = playerToJoin.nickname,
                        pathPhoto = playerToJoin.pathPhoto
                    };
                }
            }

            return playerDto; 
        }

        public async Task<bool> LeaveLobbyAsync(string roomCode, int idPlayer)
        {
            bool wasHost = false; 
            var lobby = await lobbyRepository.GetLobbyByRoomCodeAsync(roomCode);

            if (lobby != null)
            {
                wasHost = lobby.idHostPlayer == idPlayer;

                if (wasHost)
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
            }

            return wasHost; 
        }

        private string GenerateRandomCode(int length = 5)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var data = new byte[length];
            randomGenerator.GetBytes(data);
            return new string(data.Select(b => chars[b % chars.Length]).ToArray());
        }
    }
}
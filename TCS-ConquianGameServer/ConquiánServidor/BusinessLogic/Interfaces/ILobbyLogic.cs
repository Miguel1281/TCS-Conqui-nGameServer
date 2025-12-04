using ConquiánServidor.Contracts.DataContracts;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic.Interfaces
{
    public interface ILobbyLogic
    {
        Task<LobbyDto> GetLobbyStateAsync(string roomCode);
        Task<string> CreateLobbyAsync(int idHostPlayer);
        Task<PlayerDto> JoinLobbyAsync(string roomCode, int idPlayer);
        Task<PlayerDto> JoinLobbyAsGuestAsync(string email, string roomCode);
        Task<bool> LeaveLobbyAsync(string roomCode, int idPlayer);
        Task SelectGamemodeAsync(string roomCode, int idGamemode);
        Task StartGameAsync(string roomCode);
        Task KickPlayerAsync(string roomCode, int idRequestingPlayer, int idPlayerToKick);
    }
}
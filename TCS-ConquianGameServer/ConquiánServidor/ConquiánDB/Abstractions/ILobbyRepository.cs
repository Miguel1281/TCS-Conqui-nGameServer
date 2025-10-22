using ConquiánServidor.ConquiánDB;
using System.Threading.Tasks;

namespace ConquiánServidor.DataAccess.Abstractions
{
    public interface ILobbyRepository
    {
        Task<Lobby> GetLobbyByRoomCodeAsync(string roomCode);
        Task<bool> DoesRoomCodeExistAsync(string roomCode);
        void AddLobby(Lobby lobby);
        Task<int> SaveChangesAsync();
    }
}
using System.Threading.Tasks;

namespace ConquiánServidor.ConquiánDB.Abstractions
{
    public interface ILobbyRepository
    {
        Task<Lobby> GetLobbyByRoomCodeAsync(string roomCode);
        Task<bool> DoesRoomCodeExistAsync(string roomCode);
        void AddLobby(Lobby lobby);
        Task<int> SaveChangesAsync();
    }
}
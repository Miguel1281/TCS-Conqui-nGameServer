using ConquiánServidor.ConquiánDB;
using System.Threading.Tasks;

namespace ConquiánServidor.DataAccess.Abstractions
{
    public interface IPlayerRepository
    {
        Task<Player> GetPlayerByEmailAsync(string email);
        Task<Player> GetPlayerByIdAsync(int idPlayer);
        Task<Player> GetPlayerForVerificationAsync(string email);
        Task<bool> DoesNicknameExistAsync(string nickname);
        Task<Player> GetPlayerByNicknameAsync(string nickname); 
        void AddPlayer(Player player);
        Task<int> SaveChangesAsync();
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConquiánServidor.ConquiánDB.Abstractions
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
        Task<bool> DeletePlayerAsync(Player playerToDelete);
        Task<int> UpdatePlayerPointsAsync(int playerId);
        Task<List<Game>> GetPlayerGamesAsync(int idPlayer);
        Task<int> GetNextLevelThresholdAsync(int currentLevelId);
    }
}
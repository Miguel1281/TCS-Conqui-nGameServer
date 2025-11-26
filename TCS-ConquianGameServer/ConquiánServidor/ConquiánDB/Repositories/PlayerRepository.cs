using ConquiánServidor.ConquiánDB;
using ConquiánServidor.DataAccess.Abstractions;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace ConquiánServidor.DataAccess.Repositories
{
    public class PlayerRepository : IPlayerRepository
    {
        private readonly ConquiánDBEntities context;

        public PlayerRepository(ConquiánDBEntities context)
        {
            this.context = context;
        }

        public void AddPlayer(Player player)
        {
            context.Player.Add(player);
        }

        public async Task<bool> DoesNicknameExistAsync(string nickname)
        {
            return await context.Player.AnyAsync(p => p.nickname == nickname);
        }

        public async Task<Player> GetPlayerByEmailAsync(string email)
        {
            return await context.Player.FirstOrDefaultAsync(p => p.email == email);
        }

        public async Task<Player> GetPlayerByIdAsync(int idPlayer)
        {
            context.Configuration.LazyLoadingEnabled = false;
            return await context.Player.FirstOrDefaultAsync(p => p.idPlayer == idPlayer);
        }
        public async Task<Player> GetPlayerByNicknameAsync(string nickname)
        {
            return await context.Player.FirstOrDefaultAsync(p => p.nickname == nickname);
        }

        public async Task<Player> GetPlayerForVerificationAsync(string email)
        {
            return await context.Player.FirstOrDefaultAsync(p => p.email == email && p.password != null);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await context.SaveChangesAsync();
        }

        public async Task<bool> DeletePlayerAsync(Player playerToDelete)
        {
            if (playerToDelete == null)
            {
                return false;
            }

            context.Player.Remove(playerToDelete);
            int result = await context.SaveChangesAsync();
            return result > 0;
        }

        public async Task UpdatePlayerPointsAsync(int playerId, int pointsToAdd)
        {
            var player = await context.Player.FindAsync(playerId);
            if (player != null)
            {
                player.currentPoints += pointsToAdd;
                int newLevel = (player.currentPoints / 100) + 1;
                player.level = newLevel.ToString();

                await context.SaveChangesAsync();
            }
        }
    }


}
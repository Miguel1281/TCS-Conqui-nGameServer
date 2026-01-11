using ConquiánServidor.ConquiánDB;
using ConquiánServidor.DataAccess.Abstractions;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Cryptography;
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
            return await context.Player
                .Include(p => p.LevelRules)
                .FirstOrDefaultAsync(p => p.idPlayer == idPlayer);
        }
        public async Task<Player> GetPlayerByNicknameAsync(string nickname)
        {
            context.Configuration.LazyLoadingEnabled = false;

            return await context.Player
                .Include(p => p.LevelRules) 
                .FirstOrDefaultAsync(p => p.nickname == nickname);
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

        public async Task<int> UpdatePlayerPointsAsync(int playerId)
        {
            int earnedPoints = 0;

            var player = await context.Player
                .Include(p => p.LevelRules)
                .FirstOrDefaultAsync(p => p.idPlayer == playerId);

            if (player != null && player.LevelRules != null)
            {
                int minReward = player.LevelRules.MinPointsReward;
                int maxReward = player.LevelRules.MaxPointsReward;
                earnedPoints = GetNextInt(minReward, maxReward + 1);

                player.currentPoints += earnedPoints;

                while (true)
                {
                    var nextLevelRule = await context.LevelRules
                        .FirstOrDefaultAsync(lr => lr.LevelNumber == player.idLevel + 1);

                    if (nextLevelRule == null || player.currentPoints < nextLevelRule.PointsRequired)
                    {
                        break;
                    }

                    player.idLevel = nextLevelRule.LevelNumber;
                    player.LevelRules = nextLevelRule;
                }

                await context.SaveChangesAsync();
            }

            return earnedPoints;
        }

        private static int GetNextInt(int min, int max)
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] data = new byte[4];
                rng.GetBytes(data);
                int value = BitConverter.ToInt32(data, 0) & int.MaxValue;
                return (value % (max - min)) + min;
            }
        }

        public async Task<int> GetNextLevelThresholdAsync(int currentLevelId)
        {
            var nextLevel = await context.LevelRules
                            .Where(lr => lr.LevelNumber == currentLevelId + 1)
                            .Select(lr => (int?)lr.PointsRequired)
                            .FirstOrDefaultAsync();

            return nextLevel ?? -1;
        }

        public async Task<List<Game>> GetPlayerGamesAsync(int idPlayer)
        {
            return await context.Game
                .Include("Gamemode")
                .Include("GamePlayer")  
                .Include("GamePlayer.Player") 
                .Where(g => g.GamePlayer.Any(gp => gp.idPlayer == idPlayer))
                .OrderByDescending(g => g.idGame)
                .ToListAsync();
        }
    }


}
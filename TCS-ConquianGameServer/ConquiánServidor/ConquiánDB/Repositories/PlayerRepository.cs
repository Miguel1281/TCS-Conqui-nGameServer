using ConquiánServidor.ConquiánDB.Abstractions;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ConquiánServidor.ConquiánDB.Repositories
{
    public class PlayerRepository : IPlayerRepository
    {
        private const int NO_CHANGES_SAVED = 0;
        private const int LEVEL_NOT_FOUND = -1;
        private const int LEVEL_INCREMENT = 1;
        private const int RNG_UPPER_BOUND_ADJUSTMENT = 1;
        private const int RANDOM_BYTES_SIZE = 4;

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
            bool nicknameExists = await context.Player.AnyAsync(p => p.nickname == nickname);
            return nicknameExists;
        }

        public async Task<Player> GetPlayerByEmailAsync(string email)
        {
            var player = await context.Player.FirstOrDefaultAsync(p => p.email == email);
            return player;
        }

        public async Task<Player> GetPlayerByIdAsync(int idPlayer)
        {
            context.Configuration.LazyLoadingEnabled = false;

            var player = await context.Player
                .Include(p => p.LevelRules)
                .FirstOrDefaultAsync(p => p.idPlayer == idPlayer);

            return player;
        }

        public async Task<Player> GetPlayerByNicknameAsync(string nickname)
        {
            context.Configuration.LazyLoadingEnabled = false;

            var player = await context.Player
                .Include(p => p.LevelRules)
                .FirstOrDefaultAsync(p => p.nickname == nickname);

            return player;
        }

        public async Task<Player> GetPlayerForVerificationAsync(string email)
        {
            var player = await context.Player
                .FirstOrDefaultAsync(p => p.email == email && p.password != null);

            return player;
        }

        public async Task<int> SaveChangesAsync()
        {
            int changesSaved = await context.SaveChangesAsync();
            return changesSaved;
        }

        public async Task<bool> DeletePlayerAsync(Player playerToDelete)
        {
            bool playerIsNull = (playerToDelete == null);

            if (playerIsNull)
            {
                const bool DELETION_FAILED = false;
                return DELETION_FAILED;
            }

            context.Player.Remove(playerToDelete);
            int changesSaved = await context.SaveChangesAsync();

            bool deletionSucceeded = (changesSaved > NO_CHANGES_SAVED);
            return deletionSucceeded;
        }

        public async Task<int> UpdatePlayerPointsAsync(int playerId)
        {
            int earnedPoints = NO_CHANGES_SAVED;

            var player = await GetPlayerWithLevelRulesAsync(playerId);
            bool playerHasValidLevelRules = ValidatePlayerAndLevelRules(player);

            if (playerHasValidLevelRules)
            {
                earnedPoints = CalculateRandomRewardPoints(player);
                AddPointsToPlayer(player, earnedPoints);
                await UpdatePlayerLevelIfEligibleAsync(player);
                await context.SaveChangesAsync();
            }

            return earnedPoints;
        }

        private async Task<Player> GetPlayerWithLevelRulesAsync(int playerId)
        {
            var player = await context.Player
                .Include(p => p.LevelRules)
                .FirstOrDefaultAsync(p => p.idPlayer == playerId);

            return player;
        }

        private bool ValidatePlayerAndLevelRules(Player player)
        {
            bool playerExists = (player != null);
            bool hasLevelRules = (player?.LevelRules != null);

            bool isValid = (playerExists && hasLevelRules);
            return isValid;
        }

        private int CalculateRandomRewardPoints(Player player)
        {
            int minReward = player.LevelRules.MinPointsReward;
            int maxReward = player.LevelRules.MaxPointsReward;
            int upperBound = maxReward + RNG_UPPER_BOUND_ADJUSTMENT;

            int randomPoints = GetSecureRandomInt(minReward, upperBound);
            return randomPoints;
        }

        private void AddPointsToPlayer(Player player, int points)
        {
            player.currentPoints += points;
        }

        private async Task UpdatePlayerLevelIfEligibleAsync(Player player)
        {
            bool shouldContinueChecking = true;

            while (shouldContinueChecking)
            {
                int nextLevelNumber = player.idLevel + LEVEL_INCREMENT;
                var nextLevelRule = await GetLevelRuleByLevelNumberAsync(nextLevelNumber);

                bool cannotLevelUp = ShouldStopLevelUpCheck(nextLevelRule, player);

                if (cannotLevelUp)
                {
                    shouldContinueChecking = false;
                }
                else
                {
                    PromotePlayerToNextLevel(player, nextLevelRule);
                }
            }
        }

        private async Task<LevelRules> GetLevelRuleByLevelNumberAsync(int levelNumber)
        {
            var levelRule = await context.LevelRules
                .FirstOrDefaultAsync(lr => lr.LevelNumber == levelNumber);

            return levelRule;
        }

        private bool ShouldStopLevelUpCheck(LevelRules nextLevelRule, Player player)
        {
            bool nextLevelDoesNotExist = (nextLevelRule == null);
            bool playerLacksRequiredPoints = (player.currentPoints < nextLevelRule?.PointsRequired);

            bool shouldStop = (nextLevelDoesNotExist || playerLacksRequiredPoints);
            return shouldStop;
        }

        private void PromotePlayerToNextLevel(Player player, LevelRules nextLevelRule)
        {
            player.idLevel = nextLevelRule.LevelNumber;
            player.LevelRules = nextLevelRule;
        }

        private static int GetSecureRandomInt(int min, int max)
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] data = new byte[RANDOM_BYTES_SIZE];
                rng.GetBytes(data);
                int generatedValue = BitConverter.ToInt32(data, 0) & int.MaxValue;
                int range = max - min;
                int randomValue = (generatedValue % range) + min;

                return randomValue;
            }
        }

        public async Task<int> GetNextLevelThresholdAsync(int currentLevelId)
        {
            int nextLevelNumber = currentLevelId + LEVEL_INCREMENT;

            var nextLevelPoints = await context.LevelRules
                .Where(lr => lr.LevelNumber == nextLevelNumber)
                .Select(lr => (int?)lr.PointsRequired)
                .FirstOrDefaultAsync();

            int threshold = nextLevelPoints ?? LEVEL_NOT_FOUND;
            return threshold;
        }

        public async Task<List<Game>> GetPlayerGamesAsync(int idPlayer)
        {
            var playerGames = await context.Game
                .Include("Gamemode")
                .Include("GamePlayer")
                .Include("GamePlayer.Player")
                .Where(g => g.GamePlayer.Any(gp => gp.idPlayer == idPlayer))
                .OrderByDescending(g => g.idGame)
                .ToListAsync();

            return playerGames;
        }
    }
}
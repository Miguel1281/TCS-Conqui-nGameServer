using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.Enums;
using ConquiánServidor.ConquiánDB.Abstractions;
using ConquiánServidor.Utilities;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic.UserProfile
{
    public class UserProfileLogic : IUserProfileLogic
    {
        private const int LEVEL_NOT_FOUND = -1;
        private const int DEFAULT_SCORE = 0;
        private const int DEFAULT_POINTS_EARNED = 0;

        private const string DEFAULT_RANK_NAME = "Unknown";
        private const string DEFAULT_OPPONENT_NAME = "Unknown";
        private const string DEFAULT_PLAYER_NAME = "Player";
        private const string DEFAULT_GAME_MODE = "Classic";

        private const string RESULT_VICTORY = "Victory";
        private const string RESULT_DEFEAT = "Defeat";
        private const string RESULT_DRAW = "Draw";

        private const string TIME_FORMAT = "{0:D2}:{1:D2}";

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IPlayerRepository playerRepository;
        private readonly ISocialRepository socialRepository;
        private readonly IPresenceManager presenceManager;

        public UserProfileLogic(
            IPlayerRepository playerRepository,
            ISocialRepository socialRepository,
            IPresenceManager presenceManager)
        {
            this.playerRepository = playerRepository;
            this.socialRepository = socialRepository;
            this.presenceManager = presenceManager;
        }

        public async Task<PlayerDto> GetPlayerByIdAsync(int idPlayer)
        {
            Logger.Info($"Fetching profile for Player ID: {idPlayer}");

            var dbPlayer = await playerRepository.GetPlayerByIdAsync(idPlayer);
            ValidatePlayerExists(dbPlayer, idPlayer);

            int nextLevelTarget = await GetNextLevelTargetPoints(dbPlayer);
            string rankName = GetPlayerRankName(dbPlayer);
            PlayerStatus playerStatus = GetPlayerStatus(dbPlayer.idPlayer);

            Logger.Info($"Profile retrieved successfully for Player ID: {idPlayer}");

            return BuildPlayerDto(dbPlayer, nextLevelTarget, rankName, playerStatus);
        }

        private void ValidatePlayerExists(ConquiánDB.Player player, int playerId)
        {
            bool playerNotFound = (player == null);

            if (playerNotFound)
            {
                Logger.Warn($"Profile lookup failed: Player ID {playerId} not found.");
                throw new BusinessLogicException(ServiceErrorType.UserNotFound);
            }
        }

        private async Task<int> GetNextLevelTargetPoints(ConquiánDB.Player player)
        {
            int nextLevelTarget = await playerRepository.GetNextLevelThresholdAsync(player.idLevel);
            bool levelNotFound = (nextLevelTarget == LEVEL_NOT_FOUND);

            if (levelNotFound)
            {
                nextLevelTarget = player.currentPoints;
            }

            return nextLevelTarget;
        }

        private string GetPlayerRankName(ConquiánDB.Player player)
        {
            string rankName = player.LevelRules?.RankName ?? DEFAULT_RANK_NAME;
            return rankName;
        }

        private PlayerStatus GetPlayerStatus(int playerId)
        {
            bool isOnline = this.presenceManager.IsPlayerOnline(playerId);
            PlayerStatus status = isOnline ? PlayerStatus.Online : PlayerStatus.Offline;
            return status;
        }

        private PlayerDto BuildPlayerDto(
            ConquiánDB.Player player,
            int nextLevelTarget,
            string rankName,
            PlayerStatus status)
        {
            var playerDto = new PlayerDto
            {
                idPlayer = player.idPlayer,
                name = player.name,
                lastName = player.lastName,
                nickname = player.nickname,
                email = player.email,
                idLevel = player.idLevel,
                pathPhoto = player.pathPhoto,
                currentPoints = player.currentPoints,
                PointsToNextLevel = nextLevelTarget,
                RankName = rankName,
                Status = status
            };

            return playerDto;
        }

        public async Task<List<SocialDto>> GetPlayerSocialsAsync(int idPlayer)
        {
            Logger.Info($"Fetching social media links for Player ID: {idPlayer}");

            await ValidatePlayerExistsForSocials(idPlayer);

            var dbSocials = await socialRepository.GetSocialsByPlayerIdAsync(idPlayer);

            Logger.Info($"Socials retrieved for Player ID: {idPlayer}. Count: {dbSocials.Count}");

            List<SocialDto> socialDtos = ConvertToSocialDtos(dbSocials);
            return socialDtos;
        }

        private async Task ValidatePlayerExistsForSocials(int idPlayer)
        {
            var playerExists = await playerRepository.GetPlayerByIdAsync(idPlayer);
            bool playerNotFound = (playerExists == null);

            if (playerNotFound)
            {
                Logger.Warn($"Socials lookup failed: Player ID {idPlayer} not found.");
                throw new BusinessLogicException(ServiceErrorType.UserNotFound);
            }
        }

        private List<SocialDto> ConvertToSocialDtos(List<ConquiánDB.Social> dbSocials)
        {
            var socialDtos = dbSocials.Select(dbSocial => new SocialDto
            {
                IdSocial = dbSocial.idSocial,
                IdSocialType = (int)dbSocial.idSocialType,
                UserLink = dbSocial.userLink
            }).ToList();

            return socialDtos;
        }

        public async Task UpdatePlayerAsync(PlayerDto playerDto)
        {
            ValidatePlayerDto(playerDto);

            Logger.Info($"Profile update attempt for Player ID: {playerDto.idPlayer}");

            var playerToUpdate = await playerRepository.GetPlayerByIdAsync(playerDto.idPlayer);
            ValidatePlayerExistsForUpdate(playerToUpdate, playerDto.idPlayer);

            UpdatePlayerBasicInfo(playerToUpdate, playerDto);
            UpdatePasswordIfProvided(playerToUpdate, playerDto);

            await playerRepository.SaveChangesAsync();

            Logger.Info($"Profile updated successfully for Player ID: {playerDto.idPlayer}");
        }

        private void ValidatePlayerDto(PlayerDto playerDto)
        {
            bool playerDtoIsNull = (playerDto == null);

            if (playerDtoIsNull)
            {
                throw new BusinessLogicException(ServiceErrorType.ValidationFailed);
            }
        }

        private void ValidatePlayerExistsForUpdate(ConquiánDB.Player player, int playerId)
        {
            bool playerNotFound = (player == null);

            if (playerNotFound)
            {
                Logger.Warn($"Profile update failed: Player ID {playerId} not found.");
                throw new BusinessLogicException(ServiceErrorType.UserNotFound);
            }
        }

        private void UpdatePlayerBasicInfo(ConquiánDB.Player playerToUpdate, PlayerDto playerDto)
        {
            playerToUpdate.name = playerDto.name;
            playerToUpdate.lastName = playerDto.lastName;
            playerToUpdate.nickname = playerDto.nickname;
            playerToUpdate.pathPhoto = playerDto.pathPhoto;
        }

        private void UpdatePasswordIfProvided(ConquiánDB.Player playerToUpdate, PlayerDto playerDto)
        {
            bool passwordProvided = !string.IsNullOrEmpty(playerDto.password);

            if (passwordProvided)
            {
                Logger.Info($"Password update included for Player ID: {playerDto.idPlayer}");
                string hashedPassword = PasswordHasher.hashPassword(playerDto.password);
                playerToUpdate.password = hashedPassword;
            }
        }

        public async Task UpdatePlayerSocialsAsync(int idPlayer, List<SocialDto> socialDtos)
        {
            ValidateSocialDtos(socialDtos);

            Logger.Info($"Socials update attempt for Player ID: {idPlayer}");

            await ValidatePlayerExistsInSocialRepository(idPlayer);
            await RemoveExistingSocials(idPlayer);
            AddNewSocials(idPlayer, socialDtos);

            await socialRepository.SaveChangesAsync();

            Logger.Info($"Socials updated successfully for Player ID: {idPlayer}. New count: {socialDtos.Count}");
        }

        private void ValidateSocialDtos(List<SocialDto> socialDtos)
        {
            bool socialDtosIsNull = (socialDtos == null);

            if (socialDtosIsNull)
            {
                throw new BusinessLogicException(ServiceErrorType.ValidationFailed);
            }
        }

        private async Task ValidatePlayerExistsInSocialRepository(int idPlayer)
        {
            var playerExists = await socialRepository.DoesPlayerExistAsync(idPlayer);
            bool playerNotFound = !playerExists;

            if (playerNotFound)
            {
                Logger.Warn($"Socials update failed: Player ID {idPlayer} not found.");
                throw new BusinessLogicException(ServiceErrorType.UserNotFound);
            }
        }

        private async Task RemoveExistingSocials(int idPlayer)
        {
            var existingSocials = await socialRepository.GetSocialsByPlayerIdAsync(idPlayer);
            socialRepository.RemoveSocialsRange(existingSocials);
        }

        private void AddNewSocials(int idPlayer, List<SocialDto> socialDtos)
        {
            foreach (var socialDto in socialDtos)
            {
                var newSocial = CreateSocialEntity(idPlayer, socialDto);
                socialRepository.AddSocial(newSocial);
            }
        }

        private ConquiánDB.Social CreateSocialEntity(int idPlayer, SocialDto socialDto)
        {
            var social = new ConquiánDB.Social
            {
                idPlayer = idPlayer,
                idSocialType = socialDto.IdSocialType,
                userLink = socialDto.UserLink
            };

            return social;
        }

        public async Task UpdateProfilePictureAsync(int idPlayer, string newPath)
        {
            Logger.Info($"Profile picture update attempt for Player ID: {idPlayer}");

            var playerToUpdate = await playerRepository.GetPlayerByIdAsync(idPlayer);
            ValidatePlayerExistsForPictureUpdate(playerToUpdate, idPlayer);

            playerToUpdate.pathPhoto = newPath;
            await playerRepository.SaveChangesAsync();

            Logger.Info($"Profile picture updated successfully for Player ID: {idPlayer}");
        }

        private void ValidatePlayerExistsForPictureUpdate(ConquiánDB.Player player, int playerId)
        {
            bool playerNotFound = (player == null);

            if (playerNotFound)
            {
                Logger.Warn($"Profile picture update failed: Player ID {playerId} not found.");
                throw new BusinessLogicException(ServiceErrorType.UserNotFound);
            }
        }

        public async Task<List<GameHistoryDto>> GetPlayerGameHistoryAsync(int idPlayer)
        {
            Logger.Info($"Fetching game history for Player ID: {idPlayer}");

            var games = await playerRepository.GetPlayerGamesAsync(idPlayer);

            Logger.Info($"Game history retrieved for Player ID: {idPlayer}. Count: {games.Count}");

            List<GameHistoryDto> gameHistory = ConvertGamesToHistory(games, idPlayer);
            return gameHistory;
        }

        private List<GameHistoryDto> ConvertGamesToHistory(
            List<ConquiánDB.Game> games,
            int idPlayer)
        {
            var gameHistoryList = games.Select(game =>
                ConvertGameToHistoryDto(game, idPlayer)
            ).ToList();

            return gameHistoryList;
        }

        private GameHistoryDto ConvertGameToHistoryDto(ConquiánDB.Game game, int idPlayer)
        {
            var myStats = FindPlayerStats(game, idPlayer);
            var rivalStats = FindRivalStats(game, idPlayer);

            string opponentName = GetOpponentName(rivalStats);
            string myName = GetPlayerName(myStats);
            int myScore = GetPlayerScore(myStats);

            var gameResult = DetermineGameResult(myStats, rivalStats, myScore);
            string formattedTime = FormatGameTime(game.gameTime);
            string gameMode = GetGameModeName(game);

            var historyDto = new GameHistoryDto
            {
                PlayerName = myName,
                OpponentName = opponentName,
                ResultStatus = gameResult.ResultStatus,
                PointsEarned = gameResult.PointsEarned,
                GameTime = formattedTime,
                GameMode = gameMode
            };

            return historyDto;
        }

        private ConquiánDB.GamePlayer FindPlayerStats(ConquiánDB.Game game, int idPlayer)
        {
            var playerStats = game.GamePlayer.FirstOrDefault(gp => gp.idPlayer == idPlayer);
            return playerStats;
        }

        private ConquiánDB.GamePlayer FindRivalStats(ConquiánDB.Game game, int idPlayer)
        {
            var rivalStats = game.GamePlayer.FirstOrDefault(gp => gp.idPlayer != idPlayer);
            return rivalStats;
        }

        private string GetOpponentName(ConquiánDB.GamePlayer rivalStats)
        {
            string opponentName = rivalStats?.Player?.nickname ?? DEFAULT_OPPONENT_NAME;
            return opponentName;
        }

        private string GetPlayerName(ConquiánDB.GamePlayer myStats)
        {
            string playerName = myStats?.Player?.nickname ?? DEFAULT_PLAYER_NAME;
            return playerName;
        }

        private int GetPlayerScore(ConquiánDB.GamePlayer myStats)
        {
            int score = myStats?.score ?? DEFAULT_SCORE;
            return score;
        }

        private GameResult DetermineGameResult(
            ConquiánDB.GamePlayer myStats,
            ConquiánDB.GamePlayer rivalStats,
            int myScore)
        {
            var result = new GameResult
            {
                ResultStatus = RESULT_DRAW,
                PointsEarned = DEFAULT_POINTS_EARNED
            };

            bool myStatsExist = (myStats != null);

            if (!myStatsExist)
            {
                return result;
            }

            bool iWon = myStats.isWinner;
            bool rivalWon = (rivalStats != null && rivalStats.isWinner);

            if (iWon)
            {
                result.ResultStatus = RESULT_VICTORY;
                result.PointsEarned = myScore;
            }
            else if (rivalWon)
            {
                result.ResultStatus = RESULT_DEFEAT;
                result.PointsEarned = DEFAULT_POINTS_EARNED;
            }
            else
            {
                result.ResultStatus = RESULT_DRAW;
                result.PointsEarned = myScore;
            }

            return result;
        }

        private string FormatGameTime(int gameTimeInSeconds)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(gameTimeInSeconds);
            int totalMinutes = (int)timeSpan.TotalMinutes;
            int seconds = timeSpan.Seconds;

            string formattedTime = string.Format(TIME_FORMAT, totalMinutes, seconds);
            return formattedTime;
        }

        private string GetGameModeName(ConquiánDB.Game game)
        {
            string gameModeName = game.Gamemode?.gamemode1 ?? DEFAULT_GAME_MODE;
            return gameModeName;
        }

        private class GameResult
        {
            public string ResultStatus { get; set; }
            public int PointsEarned { get; set; }
        }
    }
}
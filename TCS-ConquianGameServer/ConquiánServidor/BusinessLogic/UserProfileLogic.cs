using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.Enums;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.Utilities;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic
{
    public class UserProfileLogic:IUserProfileLogic
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IPlayerRepository playerRepository;
        private readonly ISocialRepository socialRepository;
        private readonly IPresenceManager presenceManager;

        public UserProfileLogic(IPlayerRepository playerRepository, ISocialRepository socialRepository, IPresenceManager presenceManager)
        {
            this.playerRepository = playerRepository;
            this.socialRepository = socialRepository;
            this.presenceManager = presenceManager;
        }

        public async Task<PlayerDto> GetPlayerByIdAsync(int idPlayer)
        {
            Logger.Info($"Fetching profile for Player ID: {idPlayer}");

            var dbPlayer = await playerRepository.GetPlayerByIdAsync(idPlayer);

            if (dbPlayer == null)
            {
                Logger.Warn($"Profile lookup failed: Player ID {idPlayer} not found.");
                throw new BusinessLogicException(ServiceErrorType.UserNotFound);
            }

            int nextLevelTarget = await playerRepository.GetNextLevelThresholdAsync(dbPlayer.idLevel);

            if (nextLevelTarget == -1)
            {
                nextLevelTarget = dbPlayer.currentPoints;
            }

            string rankName = dbPlayer.LevelRules?.RankName ?? "Unknown";

            bool isOnline = this.presenceManager.IsPlayerOnline(dbPlayer.idPlayer);

            Logger.Info($"Profile retrieved successfully for Player ID: {idPlayer}");

            return new PlayerDto
            {
                idPlayer = dbPlayer.idPlayer,
                name = dbPlayer.name,
                lastName = dbPlayer.lastName,
                nickname = dbPlayer.nickname,
                email = dbPlayer.email,
                idLevel = dbPlayer.idLevel,
                pathPhoto = dbPlayer.pathPhoto,
                currentPoints = dbPlayer.currentPoints,
                PointsToNextLevel = nextLevelTarget,
                RankName = rankName,
                Status = isOnline ? PlayerStatus.Online : PlayerStatus.Offline
            };
        }

        public async Task<List<SocialDto>> GetPlayerSocialsAsync(int idPlayer)
        {
            Logger.Info($"Fetching social media links for Player ID: {idPlayer}");

            var playerExists = await playerRepository.GetPlayerByIdAsync(idPlayer);
            if (playerExists == null)
            {
                Logger.Warn($"Socials lookup failed: Player ID {idPlayer} not found.");
                throw new BusinessLogicException(ServiceErrorType.UserNotFound);
            }

            var dbSocials = await socialRepository.GetSocialsByPlayerIdAsync(idPlayer);

            Logger.Info($"Socials retrieved for Player ID: {idPlayer}. Count: {dbSocials.Count}");

            return dbSocials.Select(dbSocial => new SocialDto
            {
                IdSocial = dbSocial.idSocial,
                IdSocialType = (int)dbSocial.idSocialType,
                UserLink = dbSocial.userLink
            }).ToList();
        }

        public async Task UpdatePlayerAsync(PlayerDto playerDto)
        {
            if (playerDto == null)
            {
                throw new BusinessLogicException(ServiceErrorType.ValidationFailed);
            }

            Logger.Info($"Profile update attempt for Player ID: {playerDto.idPlayer}");

            var playerToUpdate = await playerRepository.GetPlayerByIdAsync(playerDto.idPlayer);

            if (playerToUpdate == null)
            {
                Logger.Warn($"Profile update failed: Player ID {playerDto.idPlayer} not found.");
                throw new BusinessLogicException(ServiceErrorType.UserNotFound);
            }

            playerToUpdate.name = playerDto.name;
            playerToUpdate.lastName = playerDto.lastName;
            playerToUpdate.nickname = playerDto.nickname;
            playerToUpdate.pathPhoto = playerDto.pathPhoto;

            if (!string.IsNullOrEmpty(playerDto.password))
            {
                Logger.Info($"Password update included for Player ID: {playerDto.idPlayer}");
                playerToUpdate.password = PasswordHasher.hashPassword(playerDto.password);
            }

            await playerRepository.SaveChangesAsync();

            Logger.Info($"Profile updated successfully for Player ID: {playerDto.idPlayer}");
        }

        public async Task UpdatePlayerSocialsAsync(int idPlayer, List<SocialDto> socialDtos)
        {
            if (socialDtos == null)
            {
                throw new BusinessLogicException(ServiceErrorType.ValidationFailed);
            }

            Logger.Info($"Socials update attempt for Player ID: {idPlayer}");

            var playerExists = await socialRepository.DoesPlayerExistAsync(idPlayer);
            if (!playerExists)
            {
                Logger.Warn($"Socials update failed: Player ID {idPlayer} not found.");
                throw new BusinessLogicException(ServiceErrorType.UserNotFound);
            }

            var existingSocials = await socialRepository.GetSocialsByPlayerIdAsync(idPlayer);
            socialRepository.RemoveSocialsRange(existingSocials);

            foreach (var socialDto in socialDtos)
            {
                socialRepository.AddSocial(new ConquiánDB.Social
                {
                    idPlayer = idPlayer,
                    idSocialType = socialDto.IdSocialType,
                    userLink = socialDto.UserLink
                });
            }

            await socialRepository.SaveChangesAsync();

            Logger.Info($"Socials updated successfully for Player ID: {idPlayer}. New count: {socialDtos.Count}");
        }

        public async Task UpdateProfilePictureAsync(int idPlayer, string newPath)
        {
            Logger.Info($"Profile picture update attempt for Player ID: {idPlayer}");

            var playerToUpdate = await playerRepository.GetPlayerByIdAsync(idPlayer);

            if (playerToUpdate == null)
            {
                Logger.Warn($"Profile picture update failed: Player ID {idPlayer} not found.");
                throw new BusinessLogicException(ServiceErrorType.UserNotFound);
            }

            playerToUpdate.pathPhoto = newPath;
            await playerRepository.SaveChangesAsync();

            Logger.Info($"Profile picture updated successfully for Player ID: {idPlayer}");
        }

        public async Task<List<GameHistoryDto>> GetPlayerGameHistoryAsync(int idPlayer)
        {
            Logger.Info($"Fetching game history for Player ID: {idPlayer}");

            var games = await playerRepository.GetPlayerGamesAsync(idPlayer);

            Logger.Info($"Game history retrieved for Player ID: {idPlayer}. Count: {games.Count}");

            return games.Select(g =>
            {
                var myStats = g.GamePlayer.FirstOrDefault(gp => gp.idPlayer == idPlayer);
                var rivalStats = g.GamePlayer.FirstOrDefault(gp => gp.idPlayer != idPlayer);

                string opponentName = rivalStats?.Player?.nickname ?? "Unknown";
                string myName = myStats?.Player?.nickname ?? "Player";

                int myScore = myStats?.score ?? 0;

                string resultString = "Draw";
                int pointsDisplay = 0;

                if (myStats != null)
                {
                    if (myStats.isWinner)
                    {
                        resultString = "Victory";
                        pointsDisplay = myScore;
                    }
                    else if (rivalStats != null && rivalStats.isWinner)
                    {
                        resultString = "Defeat";
                        pointsDisplay = 0;
                    }
                    else
                    {
                        resultString = "Draw";
                        pointsDisplay = myScore;
                    }
                }

                TimeSpan timeSpan = TimeSpan.FromSeconds(g.gameTime);
                string timeFormatted = string.Format("{0:D2}:{1:D2}", (int)timeSpan.TotalMinutes, timeSpan.Seconds);

                return new GameHistoryDto
                {
                    PlayerName = myName,
                    OpponentName = opponentName,
                    ResultStatus = resultString,
                    PointsEarned = pointsDisplay,
                    GameTime = timeFormatted,
                    GameMode = g.Gamemode != null ? g.Gamemode.gamemode1 : "Classic"
                };
            }).ToList();
        }
    }
}
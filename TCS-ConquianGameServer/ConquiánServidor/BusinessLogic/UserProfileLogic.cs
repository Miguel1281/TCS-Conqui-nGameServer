using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.Utilities;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic
{
    public class UserProfileLogic
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IPlayerRepository playerRepository;
        private readonly ISocialRepository socialRepository;

        public UserProfileLogic(IPlayerRepository playerRepository, ISocialRepository socialRepository)
        {
            this.playerRepository = playerRepository;
            this.socialRepository = socialRepository;
        }

        public async Task<PlayerDto> GetPlayerByIdAsync(int idPlayer)
        {
            Logger.Info($"Fetching profile for Player ID: {idPlayer}");

            var dbPlayer = await playerRepository.GetPlayerByIdAsync(idPlayer);

            if (dbPlayer == null)
            {
                Logger.Warn($"Profile lookup failed: Player ID {idPlayer} not found.");
                throw new KeyNotFoundException("El jugador solicitado no existe.");
            }

            Logger.Info($"Profile retrieved successfully for Player ID: {idPlayer}");

            return new PlayerDto
            {
                idPlayer = dbPlayer.idPlayer,
                name = dbPlayer.name,
                lastName = dbPlayer.lastName,
                nickname = dbPlayer.nickname,
                email = dbPlayer.email,
                level = dbPlayer.level,
                pathPhoto = dbPlayer.pathPhoto,
            };
        }

        public async Task<List<SocialDto>> GetPlayerSocialsAsync(int idPlayer)
        {
            Logger.Info($"Fetching social media links for Player ID: {idPlayer}");

            var playerExists = await playerRepository.GetPlayerByIdAsync(idPlayer);
            if (playerExists == null)
            {
                Logger.Warn($"Socials lookup failed: Player ID {idPlayer} not found.");
                throw new KeyNotFoundException("El jugador no existe.");
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
                throw new ArgumentNullException(nameof(playerDto));
            }

            Logger.Info($"Profile update attempt for Player ID: {playerDto.idPlayer}");

            var playerToUpdate = await playerRepository.GetPlayerByIdAsync(playerDto.idPlayer);

            if (playerToUpdate == null)
            {
                Logger.Warn($"Profile update failed: Player ID {playerDto.idPlayer} not found.");
                throw new KeyNotFoundException("No se encontró el perfil del jugador a actualizar.");
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
                throw new ArgumentNullException(nameof(socialDtos));
            }

            Logger.Info($"Socials update attempt for Player ID: {idPlayer}");

            var playerExists = await socialRepository.DoesPlayerExistAsync(idPlayer);
            if (!playerExists)
            {
                Logger.Warn($"Socials update failed: Player ID {idPlayer} not found.");
                throw new KeyNotFoundException("El jugador no existe para actualizar redes sociales.");
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
                throw new KeyNotFoundException("Jugador no encontrado.");
            }

            playerToUpdate.pathPhoto = newPath;
            await playerRepository.SaveChangesAsync();

            Logger.Info($"Profile picture updated successfully for Player ID: {idPlayer}");
        }
    }
}
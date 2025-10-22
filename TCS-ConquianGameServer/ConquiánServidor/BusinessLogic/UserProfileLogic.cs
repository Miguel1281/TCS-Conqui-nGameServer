using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic
{
    public class UserProfileLogic
    {
        private readonly IPlayerRepository playerRepository;
        private readonly ISocialRepository socialRepository;

        public UserProfileLogic(IPlayerRepository playerRepository, ISocialRepository socialRepository)
        {
            this.playerRepository = playerRepository;
            this.socialRepository = socialRepository;
        }

        public async Task<PlayerDto> GetPlayerByIdAsync(int idPlayer)
        {
            var dbPlayer = await playerRepository.GetPlayerByIdAsync(idPlayer);
            if (dbPlayer != null)
            {
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
            return null;
        }

        public async Task<List<SocialDto>> GetPlayerSocialsAsync(int idPlayer)
        {
            var dbSocials = await socialRepository.GetSocialsByPlayerIdAsync(idPlayer);
            return dbSocials.Select(dbSocial => new SocialDto
            {
                IdSocial = dbSocial.idSocial,
                IdSocialType = (int)dbSocial.idSocialType,
                UserLink = dbSocial.userLink
            }).ToList();
        }

        public async Task<bool> UpdatePlayerAsync(PlayerDto playerDto)
        {
            if (playerDto == null) return false;

            var playerToUpdate = await playerRepository.GetPlayerByIdAsync(playerDto.idPlayer);
            if (playerToUpdate != null)
            {
                playerToUpdate.name = playerDto.name;
                playerToUpdate.lastName = playerDto.lastName;
                playerToUpdate.nickname = playerDto.nickname;
                playerToUpdate.pathPhoto = playerDto.pathPhoto;

                if (!string.IsNullOrEmpty(playerDto.password))
                {
                    playerToUpdate.password = PasswordHasher.hashPassword(playerDto.password);
                }

                await playerRepository.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> UpdatePlayerSocialsAsync(int idPlayer, List<SocialDto> socialDtos)
        {
            if (socialDtos == null) return false;

            var playerExists = await socialRepository.DoesPlayerExistAsync(idPlayer);
            if (!playerExists)
            {
                return false;
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
            return true;
        }

        public async Task<bool> UpdateProfilePictureAsync(int idPlayer, string newPath)
        {
            var playerToUpdate = await playerRepository.GetPlayerByIdAsync(idPlayer);
            if (playerToUpdate != null)
            {
                playerToUpdate.pathPhoto = newPath;
                await playerRepository.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }
}
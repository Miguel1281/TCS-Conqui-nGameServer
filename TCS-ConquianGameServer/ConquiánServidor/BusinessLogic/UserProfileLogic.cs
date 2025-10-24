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
            PlayerDto playerDto = new PlayerDto(); 
            var dbPlayer = await playerRepository.GetPlayerByIdAsync(idPlayer);

            if (dbPlayer != null)
            {
                playerDto = new PlayerDto
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
            return playerDto; 
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
            bool success = false; 
            if (playerDto != null)
            {
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
                    success = true;
                }
            }
            return success; 
        }

        public async Task<bool> UpdatePlayerSocialsAsync(int idPlayer, List<SocialDto> socialDtos)
        {
            bool success = false; 
            if (socialDtos != null)
            {
                var playerExists = await socialRepository.DoesPlayerExistAsync(idPlayer);
                if (playerExists)
                {
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
                    success = true;
                }
            }
            return success; 
        }

        public async Task<bool> UpdateProfilePictureAsync(int idPlayer, string newPath)
        {
            bool success = false; 
            var playerToUpdate = await playerRepository.GetPlayerByIdAsync(idPlayer);

            if (playerToUpdate != null)
            {
                playerToUpdate.pathPhoto = newPath;
                await playerRepository.SaveChangesAsync();
                success = true;
            }
            return success; 
        }
    }
}
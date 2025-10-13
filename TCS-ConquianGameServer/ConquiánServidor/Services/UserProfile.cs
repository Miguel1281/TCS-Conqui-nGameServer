using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using System;
using System.Collections.Generic;
using System.Data.Entity; 
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks; 

namespace ConquiánServidor.Services
{
    public class UserProfile : IUserProfile
    {
        public async Task<PlayerDto> GetPlayerByIdAsync(int idPlayer)
        {
            try
            {
                using (var context = new ConquiánDBEntities())
                {
                    context.Configuration.LazyLoadingEnabled = false;
                    var dbPlayer = await context.Player.FirstOrDefaultAsync(p => p.idPlayer == idPlayer);

                    if (dbPlayer != null)
                    {
                        return new PlayerDto
                        {
                            idPlayer = dbPlayer.idPlayer,
                            name = dbPlayer.name,
                            lastName = dbPlayer.lastName,
                            nickname = dbPlayer.nickname,
                            email = dbPlayer.email,
                            pathPhoto = dbPlayer.pathPhoto,
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                throw new FaultException("Error al recuperar la información del jugador.");
            }
            return null;
        }

        public async Task<List<SocialDto>> GetPlayerSocialsAsync(int idPlayer)
        {
            try
            {
                using (var context = new ConquiánDBEntities())
                {
                    context.Configuration.LazyLoadingEnabled = false;
                    var dbSocials = await context.Social.Where(s => s.idPlayer == idPlayer).ToListAsync();

                    return dbSocials.Select(dbSocial => new SocialDto
                    {
                        IdSocial = dbSocial.idSocial,
                        IdSocialType = (int)dbSocial.idSocialType,
                        UserLink = dbSocial.userLink
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                throw new FaultException("Error al recuperar las redes sociales.");
            }
        }

        public async Task<bool> UpdatePlayerAsync(PlayerDto playerDto)
        {
            if (playerDto == null) return false;

            try
            {
                using (var context = new ConquiánDBEntities())
                {
                    var playerToUpdate = await context.Player.FirstOrDefaultAsync(p => p.idPlayer == playerDto.idPlayer);

                    if (playerToUpdate != null)
                    {
                        playerToUpdate.name = playerDto.name;
                        playerToUpdate.lastName = playerDto.lastName;
                        playerToUpdate.nickname = playerDto.nickname;
                        playerToUpdate.pathPhoto = playerDto.pathPhoto;
                        context.Entry(playerToUpdate).State = System.Data.Entity.EntityState.Modified;


                        await context.SaveChangesAsync();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new FaultException("Error al actualizar el perfil.");
            }
            return false;
        }

        public async Task<bool> UpdatePlayerSocialsAsync(int idPlayer, List<SocialDto> socialDtos)
        {
            if (socialDtos == null) return false;

            try
            {
                using (var context = new ConquiánDBEntities())
                {
                    var playerExists = await context.Player.AnyAsync(p => p.idPlayer == idPlayer);
                    if (!playerExists)
                    {
                        return false;
                    }

                    var existingSocials = context.Social.Where(s => s.idPlayer == idPlayer);
                    context.Social.RemoveRange(existingSocials);

                    foreach (var socialDto in socialDtos)
                    {
                        var newSocial = new ConquiánDB.Social
                        {
                            idPlayer = idPlayer,
                            idSocialType = socialDto.IdSocialType,
                            userLink = socialDto.UserLink
                        };
                        context.Social.Add(newSocial);
                    }

                    await context.SaveChangesAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new FaultException("Ocurrió un error al actualizar las redes sociales.");
            }
        }
    }
}
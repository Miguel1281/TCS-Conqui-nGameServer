using ConquiánServidor.BusinessLogic;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.DataAccess.Repositories;
using ConquiánServidor.ConquiánDB;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
using System;

namespace ConquiánServidor.Services
{
    public class UserProfile : IUserProfile
    {
        private readonly UserProfileLogic profileLogic;

        // Constructor
        public UserProfile()
        {
            var dbContext = new ConquiánDBEntities();
            IPlayerRepository playerRepository = new PlayerRepository(dbContext);
            ISocialRepository socialRepository = new SocialRepository(dbContext);
            profileLogic = new UserProfileLogic(playerRepository, socialRepository);
        }

        public async Task<PlayerDto> GetPlayerByIdAsync(int idPlayer)
        {
            try
            {
                return await profileLogic.GetPlayerByIdAsync(idPlayer);
            }
            catch (Exception ex)
            {
                // TODO: Registrar el error 'ex'
                throw new FaultException("Error al recuperar la información del jugador.");
            }
        }

        public async Task<List<SocialDto>> GetPlayerSocialsAsync(int idPlayer)
        {
            try
            {
                return await profileLogic.GetPlayerSocialsAsync(idPlayer);
            }
            catch (Exception ex)
            {
                throw new FaultException("Error al recuperar las redes sociales.");
            }
        }

        public async Task<bool> UpdatePlayerAsync(PlayerDto playerDto)
        {
            try
            {
                return await profileLogic.UpdatePlayerAsync(playerDto);
            }
            catch (Exception ex)
            {
                throw new FaultException("Error al actualizar el perfil.");
            }
        }

        public async Task<bool> UpdatePlayerSocialsAsync(int idPlayer, List<SocialDto> socialDtos)
        {
            try
            {
                return await profileLogic.UpdatePlayerSocialsAsync(idPlayer, socialDtos);
            }
            catch (Exception ex)
            {
                throw new FaultException("Ocurrió un error al actualizar las redes sociales.");
            }
        }

        public async Task<bool> UpdateProfilePictureAsync(int idPlayer, string newPath)
        {
            try
            {
                return await profileLogic.UpdateProfilePictureAsync(idPlayer, newPath);
            }
            catch (Exception ex)
            {
                return false; 
            }
        }
    }
}
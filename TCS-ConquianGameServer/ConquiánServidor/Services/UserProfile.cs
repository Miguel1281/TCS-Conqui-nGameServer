using ConquiánServidor.BusinessLogic;
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.DataAccess.Repositories;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Services
{
    public class UserProfile : IUserProfile
    {
        private readonly UserProfileLogic profileLogic;

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
            catch (KeyNotFoundException ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.NotFound, ex.Message);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Jugador no encontrado"));
            }
            catch (Exception ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.OperationFailed, "Error al recuperar la información del jugador.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.Message));
            }
        }

        public async Task<List<SocialDto>> GetPlayerSocialsAsync(int idPlayer)
        {
            try
            {
                return await profileLogic.GetPlayerSocialsAsync(idPlayer);
            }
            catch (KeyNotFoundException ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.NotFound, ex.Message);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Jugador no encontrado"));
            }
            catch (Exception ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.OperationFailed, "Error al recuperar las redes sociales.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.Message));
            }
        }

        public async Task UpdatePlayerAsync(PlayerDto playerDto)
        {
            try
            {
                await profileLogic.UpdatePlayerAsync(playerDto);
            }
            catch (KeyNotFoundException ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.NotFound, ex.Message);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Jugador no encontrado"));
            }
            catch (DbUpdateException)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, "Error al guardar los cambios en la base de datos.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Error BD"));
            }
            catch (SqlException)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, "La base de datos no responde.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Error SQL"));
            }
            catch (Exception ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.OperationFailed, "Error inesperado al actualizar el perfil.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.Message));
            }
        }

        public async Task UpdatePlayerSocialsAsync(int idPlayer, List<SocialDto> socials)
        {
            try
            {
                await profileLogic.UpdatePlayerSocialsAsync(idPlayer, socials);
            }
            catch (KeyNotFoundException ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.NotFound, ex.Message);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Jugador no encontrado"));
            }
            catch (DbUpdateException)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, "Error al actualizar redes sociales.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Error BD"));
            }
            catch (Exception ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.OperationFailed, "Ocurrió un error inesperado.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.Message));
            }
        }

        public async Task UpdateProfilePictureAsync(int idPlayer, string newPath)
        {
            try
            {
                await profileLogic.UpdateProfilePictureAsync(idPlayer, newPath);
            }
            catch (KeyNotFoundException ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.NotFound, ex.Message);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Jugador no encontrado"));
            }
            catch (DbUpdateException)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, "Error al guardar la foto.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Error BD"));
            }
            catch (Exception ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.OperationFailed, "Error al actualizar la foto.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.Message));
            }
        }
    }
}
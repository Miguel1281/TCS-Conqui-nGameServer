using ConquiánServidor.BusinessLogic;
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.DataAccess.Repositories;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class FriendList : IFriendList
    {
        private readonly FriendshipLogic friendshipLogic;

        public FriendList()
        {
            var dbContext = new ConquiánDBEntities();
            IFriendshipRepository friendshipRepository = new FriendshipRepository(dbContext);
            IPlayerRepository playerRepository = new PlayerRepository(dbContext);

            friendshipLogic = new FriendshipLogic(friendshipRepository, playerRepository);
        }

        public async Task<PlayerDto> GetPlayerByNicknameAsync(string nickname, int idCurrentUser)
        {
            try
            {
                return await friendshipLogic.GetPlayerByNicknameAsync(nickname, idCurrentUser);
            }
            catch (KeyNotFoundException ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.NotFound, ex.Message);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Jugador no encontrado"));
            }
            catch (ArgumentException ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.ValidationFailed, ex.Message);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Búsqueda inválida"));
            }
            catch (Exception ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.OperationFailed, "Error al buscar jugador.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.Message));
            }
        }

        public async Task<List<PlayerDto>> GetFriendsAsync(int idPlayer)
        {
            try
            {
                return await friendshipLogic.GetFriendsAsync(idPlayer);
            }
            catch (EntityException)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, "No se pudo conectar con la base de datos.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Error Conexión BD"));
            }
            catch (SqlException)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, "Error interno de la base de datos.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Error SQL"));
            }
            catch (Exception ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.OperationFailed, "Error al obtener la lista de amigos.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.Message));
            }
        }

        public async Task<List<FriendRequestDto>> GetFriendRequestsAsync(int idPlayer)
        {
            try
            {
                return await friendshipLogic.GetFriendRequestsAsync(idPlayer);
            }
            catch (EntityException)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, "No se pudo recuperar las solicitudes.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Error Conexión BD"));
            }
            catch (SqlException)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, "Error de base de datos al leer solicitudes.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Error SQL"));
            }
            catch (Exception ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.OperationFailed, "Error al obtener las solicitudes de amistad.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.Message));
            }
        }

        public async Task SendFriendRequestAsync(int idSender, int idReceiver)
        {
            try
            {
                await friendshipLogic.SendFriendRequestAsync(idSender, idReceiver);
            }
            catch (InvalidOperationException ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.DuplicateRecord, ex.Message);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Solicitud existente"));
            }
            catch (DbUpdateException)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, "Error al guardar la solicitud.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Error BD"));
            }
            catch (EntityException)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, "No hay conexión con la base de datos.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Error Conexión BD"));
            }
            catch (Exception ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.OperationFailed, "Error al enviar la solicitud.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.Message));
            }
        }

        public async Task UpdateFriendRequestStatusAsync(int idFriendship, int idStatus)
        {
            try
            {
                await friendshipLogic.UpdateFriendRequestStatusAsync(idFriendship, idStatus);
            }
            catch (KeyNotFoundException ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.NotFound, ex.Message);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Solicitud no encontrada"));
            }
            catch (DbUpdateException)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, "Error al actualizar el estado de la solicitud.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Error BD"));
            }
            catch (EntityException)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, "No hay conexión con la base de datos.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Error Conexión BD"));
            }
            catch (Exception ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.OperationFailed, "Error al actualizar la solicitud.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.Message));
            }
        }

        public async Task DeleteFriendAsync(int idPlayer, int idFriend)
        {
            try
            {
                await friendshipLogic.DeleteFriendAsync(idPlayer, idFriend);
            }
            catch (KeyNotFoundException ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.NotFound, ex.Message);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Amistad no encontrada"));
            }
            catch (DbUpdateException)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, "Error al eliminar al amigo.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Error BD"));
            }
            catch (EntityException)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, "No hay conexión con la base de datos.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Error Conexión BD"));
            }
            catch (Exception ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.OperationFailed, "Error al eliminar amigo.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.Message));
            }
        }
    }
}
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

        public async Task<PlayerDto> GetPlayerByNicknameAsync(string nickname, int idPlayer)
        {
            try
            {
                return await friendshipLogic.GetPlayerByNicknameAsync(nickname, idPlayer);
            }
            catch (Exception ex)
            {
                throw new FaultException("Error al buscar jugador.");
            }
        }

        public async Task<List<PlayerDto>> GetFriendsAsync(int idPlayer)
        {
            try
            {
                return await friendshipLogic.GetFriendsAsync(idPlayer);
            }
            catch (Exception ex)
            {
                throw new FaultException("Error al obtener la lista de amigos.");
            }
        }

        public async Task<List<FriendRequestDto>> GetFriendRequestsAsync(int idPlayer)
        {
            try
            {
                return await friendshipLogic.GetFriendRequestsAsync(idPlayer);
            }
            catch (Exception ex)
            {
                throw new FaultException("Error al obtener las solicitudes de amistad.");
            }
        }

        public async Task<bool> SendFriendRequestAsync(int idPlayer, int idFriend)
        {
            try
            {
                return await friendshipLogic.SendFriendRequestAsync(idPlayer, idFriend);
            }
            catch (Exception ex)
            {
                throw new FaultException("Error al enviar la solicitud.");
            }
        }

        public async Task<bool> UpdateFriendRequestStatusAsync(int idFriendship, int newStatus)
        {
            try
            {
                return await friendshipLogic.UpdateFriendRequestStatusAsync(idFriendship, newStatus);
            }
            catch (Exception ex)
            {
                throw new FaultException("Error al actualizar la solicitud.");
            }
        }

        public async Task<bool> DeleteFriendAsync(int idPlayer, int idFriend)
        {
            try
            {
                return await friendshipLogic.DeleteFriendAsync(idPlayer, idFriend);
            }
            catch (Exception ex)
            {
                throw new FaultException("Error al eliminar amigo.");
            }
        }
    }
}
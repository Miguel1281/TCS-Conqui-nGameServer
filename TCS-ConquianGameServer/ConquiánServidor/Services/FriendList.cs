using Autofac;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.Utilities.ExceptionHandler;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class FriendList : IFriendList
    {
        private readonly IFriendshipLogic friendshipLogic;
        private readonly IServiceExceptionHandler serviceExceptionHandler;

        public FriendList()
        {
            Bootstrapper.Init();
            this.friendshipLogic = Bootstrapper.Container.Resolve<IFriendshipLogic>();
            this.serviceExceptionHandler = Bootstrapper.Container.Resolve<IServiceExceptionHandler>();
        }

        public FriendList(IFriendshipLogic friendshipLogic, IServiceExceptionHandler serviceExceptionHandler)
        {
            this.friendshipLogic = friendshipLogic;
            this.serviceExceptionHandler = serviceExceptionHandler;
        }

        public async Task<PlayerDto> GetPlayerByNicknameAsync(string nickname, int idCurrentUser)
        {
            try
            {
                return await friendshipLogic.GetPlayerByNicknameAsync(nickname, idCurrentUser);
            }
            catch (Exception ex)
            {
                throw serviceExceptionHandler.HandleException(ex, "FriendList: GetPlayerByNicknameAsync");
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
                throw serviceExceptionHandler.HandleException(ex, "FriendList: GetFriendsAsync");
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
                throw serviceExceptionHandler.HandleException(ex, "FriendList: GetFriendRequestsAsync");
            }
        }

        public async Task SendFriendRequestAsync(int idSender, int idReceiver)
        {
            try
            {
                await friendshipLogic.SendFriendRequestAsync(idSender, idReceiver);
            }
            catch (Exception ex)
            {
                throw serviceExceptionHandler.HandleException(ex, "FriendList: SendFriendRequestAsync");
            }
        }

        public async Task UpdateFriendRequestStatusAsync(int idFriendship, int idStatus)
        {
            try
            {
                await friendshipLogic.UpdateFriendRequestStatusAsync(idFriendship, idStatus);
            }
            catch (Exception ex)
            {
                throw serviceExceptionHandler.HandleException(ex, "FriendList: UpdateFriendRequestStatusAsync");
            }
        }

        public async Task DeleteFriendAsync(int idPlayer, int idFriend)
        {
            try
            {
                await friendshipLogic.DeleteFriendAsync(idPlayer, idFriend);
            }
            catch (Exception ex)
            {
                throw serviceExceptionHandler.HandleException(ex, "FriendList: DeleteFriendAsync");
            }
        }
    }
}
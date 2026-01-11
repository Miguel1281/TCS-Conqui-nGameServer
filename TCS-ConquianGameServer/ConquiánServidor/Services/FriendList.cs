using Autofac;
using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class FriendList : IFriendList
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IFriendshipLogic friendshipLogic;

        private const string LOGIC_ERROR_MESSAGE = "logic error";
        private const string INTERNAL_SERVER_ERROR_MESSAGE = "internal server error";
        private const string INTERNAL_FAULT_REASON = "internal error";
        private const string DATABASE_ERROR_MESSAGE = "database error";
        private const string DATABASE_UNAVAILABLE_REASON = "database unavailable";

        public FriendList()
        {
            Bootstrapper.Init();
            this.friendshipLogic = Bootstrapper.Container.Resolve<IFriendshipLogic>();
        }

        public FriendList(IFriendshipLogic friendshipLogic)
        {
            this.friendshipLogic = friendshipLogic;
        }

        public async Task<PlayerDto> GetPlayerByNicknameAsync(string nickname, int idCurrentUser)
        {
            try
            {
                return await friendshipLogic.GetPlayerByNicknameAsync(nickname, idCurrentUser);
            }
            catch (Exception ex)
            {
                throw HandleException(ex, "error getting player by nickname");
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
                throw HandleException(ex, "error getting friends list");
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
                throw HandleException(ex, "error getting friend requests");
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
                throw HandleException(ex, "error sending friend request");
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
                throw HandleException(ex, "error updating friend request status");
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
                throw HandleException(ex, "error deleting friend");
            }
        }

        private static Exception HandleException(Exception ex, string logMessage)
        {
            if (ex is BusinessLogicException businessEx)
            {
                var fault = new ServiceFaultDto(businessEx.ErrorType, LOGIC_ERROR_MESSAGE);
                return new FaultException<ServiceFaultDto>(fault, new FaultReason(businessEx.ErrorType.ToString()));
            }

            if (ex is SqlException || ex is EntityException)
            {
                Logger.Error(ex, logMessage);
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, DATABASE_ERROR_MESSAGE);
                return new FaultException<ServiceFaultDto>(fault, new FaultReason(DATABASE_UNAVAILABLE_REASON));
            }

            Logger.Error(ex, logMessage);
            var faultInternal = new ServiceFaultDto(ServiceErrorType.ServerInternalError, INTERNAL_SERVER_ERROR_MESSAGE);
            return new FaultException<ServiceFaultDto>(faultInternal, new FaultReason(INTERNAL_FAULT_REASON));
        }
    }
}
using Autofac;
using ConquiánServidor.BusinessLogic;
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
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class FriendList : IFriendList
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IFriendshipLogic friendshipLogic;

        private const string LOGIC_ERROR_MESSAGE = "Logic Error";
        private const string INTERNAL_SERVER_ERROR_MESSAGE = "Internal Server Error";
        private const string INTERNAL_FAULT_REASON = "Internal Error";
        private const string DATABASE_ERROR_MESSAGE = "Database Error";
        private const string DATA_ACCESS_ERROR_MESSAGE = "Data Access Error";
        private const string DATABASE_UNAVAILABLE_REASON = "Database Unavailable";

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
            catch (BusinessLogicException ex)
            {
                var fault = new ServiceFaultDto(ex.ErrorType, LOGIC_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.ErrorType.ToString()));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error getting player by nickname");
                var fault = new ServiceFaultDto(ServiceErrorType.ServerInternalError, INTERNAL_SERVER_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(INTERNAL_FAULT_REASON));
            }
        }

        public async Task<List<PlayerDto>> GetFriendsAsync(int idPlayer)
        {
            try
            {
                return await friendshipLogic.GetFriendsAsync(idPlayer);
            }
            catch (SqlException ex)
            {
                Logger.Error(ex, "SQL Error getting friends list");
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, DATABASE_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(DATABASE_UNAVAILABLE_REASON));
            }
            catch (EntityException ex)
            {
                Logger.Error(ex, "Entity Framework Error getting friends list");
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, DATA_ACCESS_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(DATABASE_UNAVAILABLE_REASON));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unexpected error getting friends list");
                var fault = new ServiceFaultDto(ServiceErrorType.ServerInternalError, INTERNAL_SERVER_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(INTERNAL_FAULT_REASON));
            }
        }

        public async Task<List<FriendRequestDto>> GetFriendRequestsAsync(int idPlayer)
        {
            try
            {
                return await friendshipLogic.GetFriendRequestsAsync(idPlayer);
            }
            catch (SqlException ex)
            {
                Logger.Error(ex, "SQL Error getting friend requests");
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, DATABASE_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(DATABASE_UNAVAILABLE_REASON));
            }
            catch (EntityException ex)
            {
                Logger.Error(ex, "Entity Framework Error getting friend requests");
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, DATA_ACCESS_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(DATABASE_UNAVAILABLE_REASON));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unexpected error getting friend requests");
                var fault = new ServiceFaultDto(ServiceErrorType.ServerInternalError, INTERNAL_SERVER_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(INTERNAL_FAULT_REASON));
            }
        }

        public async Task SendFriendRequestAsync(int idSender, int idReceiver)
        {
            try
            {
                await friendshipLogic.SendFriendRequestAsync(idSender, idReceiver);
            }
            catch (BusinessLogicException ex)
            {
                var fault = new ServiceFaultDto(ex.ErrorType, LOGIC_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.ErrorType.ToString()));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error sending friend request");
                var fault = new ServiceFaultDto(ServiceErrorType.ServerInternalError, INTERNAL_SERVER_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(INTERNAL_FAULT_REASON));
            }
        }

        public async Task UpdateFriendRequestStatusAsync(int idFriendship, int idStatus)
        {
            try
            {
                await friendshipLogic.UpdateFriendRequestStatusAsync(idFriendship, idStatus);
            }
            catch (BusinessLogicException ex)
            {
                var fault = new ServiceFaultDto(ex.ErrorType, LOGIC_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.ErrorType.ToString()));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error updating friend request status");
                var fault = new ServiceFaultDto(ServiceErrorType.ServerInternalError, INTERNAL_SERVER_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(INTERNAL_FAULT_REASON));
            }
        }

        public async Task DeleteFriendAsync(int idPlayer, int idFriend)
        {
            try
            {
                await friendshipLogic.DeleteFriendAsync(idPlayer, idFriend);
            }
            catch (BusinessLogicException ex)
            {
                var fault = new ServiceFaultDto(ex.ErrorType, LOGIC_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.ErrorType.ToString()));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error deleting friend");
                var fault = new ServiceFaultDto(ServiceErrorType.ServerInternalError, INTERNAL_SERVER_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(INTERNAL_FAULT_REASON));
            }
        }
    }
}
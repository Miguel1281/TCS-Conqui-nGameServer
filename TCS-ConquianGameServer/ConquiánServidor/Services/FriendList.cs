using Autofac;
using ConquiánServidor.BusinessLogic;
using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.DataAccess.Repositories;
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
        private readonly FriendshipLogic friendshipLogic;

        public FriendList()
        {
            Bootstrapper.Init();
            this.friendshipLogic = Bootstrapper.Container.Resolve<FriendshipLogic>();
        }

        public FriendList(FriendshipLogic friendshipLogic)
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
                var fault = new ServiceFaultDto(ex.ErrorType, "Logic Error");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.ErrorType.ToString()));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error getting player by nickname");
                var fault = new ServiceFaultDto(ServiceErrorType.ServerInternalError, "Internal Server Error");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Internal Error"));
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
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, "Database Error");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Database Unavailable"));
            }
            catch (EntityException ex)
            {
                Logger.Error(ex, "Entity Framework Error getting friends list");
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, "Data Access Error");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Database Unavailable"));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unexpected error getting friends list");
                var fault = new ServiceFaultDto(ServiceErrorType.ServerInternalError, "Internal Server Error");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Internal Error"));
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
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, "Database Error");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Database Unavailable"));
            }
            catch (EntityException ex)
            {
                Logger.Error(ex, "Entity Framework Error getting friend requests");
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, "Data Access Error");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Database Unavailable"));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unexpected error getting friend requests");
                var fault = new ServiceFaultDto(ServiceErrorType.ServerInternalError, "Internal Server Error");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Internal Error"));
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
                var fault = new ServiceFaultDto(ex.ErrorType, "Logic Error");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.ErrorType.ToString()));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error sending friend request");
                var fault = new ServiceFaultDto(ServiceErrorType.ServerInternalError, "Internal Server Error");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Internal Error"));
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
                var fault = new ServiceFaultDto(ex.ErrorType, "Logic Error");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.ErrorType.ToString()));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error updating friend request status");
                var fault = new ServiceFaultDto(ServiceErrorType.ServerInternalError, "Internal Server Error");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Internal Error"));
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
                var fault = new ServiceFaultDto(ex.ErrorType, "Logic Error");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.ErrorType.ToString()));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error deleting friend");
                var fault = new ServiceFaultDto(ServiceErrorType.ServerInternalError, "Internal Server Error");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Internal Error"));
            }
        }
    }
}
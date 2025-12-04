using Autofac;
using ConquiánServidor.BusinessLogic;
using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.DataAccess.Repositories;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Services
{
    public class UserProfile : IUserProfile
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IUserProfileLogic userProfileLogic;

        private const string LOGIC_ERROR_MESSAGE = "Logic Error";
        private const string INTERNAL_SERVER_ERROR_MESSAGE = "Internal Server Error";
        private const string INTERNAL_ERROR_REASON = "Internal Error";

        private const string DATABASE_UNAVAILABLE_MESSAGE = "Database Unavailable";
        private const string DATABASE_ERROR_REASON = "Database Error";
        private const string DB_SAVE_ERROR_MESSAGE = "Error saving changes";
        private const string SQL_ERROR_REASON = "SQL Error";

        public UserProfile()
        {
            Bootstrapper.Init();
            this.userProfileLogic = Bootstrapper.Container.Resolve<IUserProfileLogic>();
        }

        public UserProfile(IUserProfileLogic userProfileLogic)
        {
            this.userProfileLogic = userProfileLogic;
        }

        public async Task<PlayerDto> GetPlayerByIdAsync(int idPlayer)
        {
            try
            {
                return await userProfileLogic.GetPlayerByIdAsync(idPlayer);
            }
            catch (BusinessLogicException ex)
            {
                var fault = new ServiceFaultDto(ex.ErrorType, LOGIC_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.ErrorType.ToString()));
            }
            catch (Exception ex) when (ex is SqlException || ex is EntityException)
            {
                Logger.Error(ex, "Database error getting player profile");
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, DATABASE_UNAVAILABLE_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(DATABASE_ERROR_REASON));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unexpected error getting player profile");
                var fault = new ServiceFaultDto(ServiceErrorType.ServerInternalError, INTERNAL_SERVER_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(INTERNAL_ERROR_REASON));
            }
        }

        public async Task<List<SocialDto>> GetPlayerSocialsAsync(int idPlayer)
        {
            try
            {
                return await userProfileLogic.GetPlayerSocialsAsync(idPlayer);
            }
            catch (BusinessLogicException ex)
            {
                var fault = new ServiceFaultDto(ex.ErrorType, LOGIC_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.ErrorType.ToString()));
            }
            catch (Exception ex) when (ex is SqlException || ex is EntityException)
            {
                Logger.Error(ex, "Database error getting player socials");
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, DATABASE_UNAVAILABLE_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(DATABASE_ERROR_REASON));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unexpected error getting player socials");
                var fault = new ServiceFaultDto(ServiceErrorType.ServerInternalError, INTERNAL_SERVER_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(INTERNAL_ERROR_REASON));
            }
        }

        public async Task UpdatePlayerAsync(PlayerDto playerDto)
        {
            try
            {
                await userProfileLogic.UpdatePlayerAsync(playerDto);
            }
            catch (BusinessLogicException ex)
            {
                var fault = new ServiceFaultDto(ex.ErrorType, LOGIC_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.ErrorType.ToString()));
            }
            catch (DbUpdateException ex)
            {
                Logger.Error(ex, "Database error updating player profile");
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, DB_SAVE_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(DATABASE_ERROR_REASON));
            }
            catch (SqlException ex)
            {
                Logger.Error(ex, "SQL error updating player profile");
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, DATABASE_UNAVAILABLE_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(SQL_ERROR_REASON));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unexpected error updating player profile");
                var fault = new ServiceFaultDto(ServiceErrorType.ServerInternalError, INTERNAL_SERVER_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(INTERNAL_ERROR_REASON));
            }
        }

        public async Task UpdatePlayerSocialsAsync(int idPlayer, List<SocialDto> socials)
        {
            try
            {
                await userProfileLogic.UpdatePlayerSocialsAsync(idPlayer, socials);
            }
            catch (BusinessLogicException ex)
            {
                var fault = new ServiceFaultDto(ex.ErrorType, LOGIC_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.ErrorType.ToString()));
            }
            catch (DbUpdateException ex)
            {
                Logger.Error(ex, "Database error updating player socials");
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, DB_SAVE_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(DATABASE_ERROR_REASON));
            }
            catch (Exception ex) when (ex is SqlException || ex is EntityException)
            {
                Logger.Error(ex, "Database connectivity error in UpdatePlayerSocials");
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, DATABASE_UNAVAILABLE_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(DATABASE_ERROR_REASON));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unexpected error updating player socials");
                var fault = new ServiceFaultDto(ServiceErrorType.ServerInternalError, INTERNAL_SERVER_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(INTERNAL_ERROR_REASON));
            }
        }

        public async Task UpdateProfilePictureAsync(int idPlayer, string newPath)
        {
            try
            {
                await userProfileLogic.UpdateProfilePictureAsync(idPlayer, newPath);
            }
            catch (BusinessLogicException ex)
            {
                var fault = new ServiceFaultDto(ex.ErrorType, LOGIC_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.ErrorType.ToString()));
            }
            catch (DbUpdateException ex)
            {
                Logger.Error(ex, "Database error updating profile picture");
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, DB_SAVE_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(DATABASE_ERROR_REASON));
            }
            catch (Exception ex) when (ex is SqlException || ex is EntityException)
            {
                Logger.Error(ex, "Database connectivity error in UpdateProfilePicture");
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, DATABASE_UNAVAILABLE_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(DATABASE_ERROR_REASON));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unexpected error updating profile picture");
                var fault = new ServiceFaultDto(ServiceErrorType.ServerInternalError, INTERNAL_SERVER_ERROR_MESSAGE);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(INTERNAL_ERROR_REASON));
            }
        }
    }
}
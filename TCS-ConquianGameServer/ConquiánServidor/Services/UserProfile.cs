using Autofac;
using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
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
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class UserProfile : IUserProfile
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IUserProfileLogic userProfileLogic;

        private const string LOGIC_ERROR_MESSAGE = "logic error";
        private const string INTERNAL_SERVER_ERROR_MESSAGE = "internal server error";
        private const string INTERNAL_ERROR_REASON = "internal error";
        private const string DATABASE_UNAVAILABLE_MESSAGE = "database unavailable";
        private const string DATABASE_ERROR_REASON = "database error";
        private const string DB_SAVE_ERROR_MESSAGE = "error saving changes";

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
            catch (Exception ex)
            {
                throw HandleException(ex, "error getting player profile");
            }
        }

        public async Task<List<SocialDto>> GetPlayerSocialsAsync(int idPlayer)
        {
            try
            {
                return await userProfileLogic.GetPlayerSocialsAsync(idPlayer);
            }
            catch (Exception ex)
            {
                throw HandleException(ex, "error getting player socials");
            }
        }

        public async Task UpdatePlayerAsync(PlayerDto playerDto)
        {
            try
            {
                await userProfileLogic.UpdatePlayerAsync(playerDto);
            }
            catch (Exception ex)
            {
                throw HandleException(ex, "error updating player profile");
            }
        }

        public async Task UpdatePlayerSocialsAsync(int idPlayer, List<SocialDto> socials)
        {
            try
            {
                await userProfileLogic.UpdatePlayerSocialsAsync(idPlayer, socials);
            }
            catch (Exception ex)
            {
                throw HandleException(ex, "error updating player socials");
            }
        }

        public async Task UpdateProfilePictureAsync(int idPlayer, string newPath)
        {
            try
            {
                await userProfileLogic.UpdateProfilePictureAsync(idPlayer, newPath);
            }
            catch (Exception ex)
            {
                throw HandleException(ex, "error updating profile picture");
            }
        }

        public async Task<List<GameHistoryDto>> GetPlayerGameHistoryAsync(int idPlayer)
        {
            try
            {
                return await userProfileLogic.GetPlayerGameHistoryAsync(idPlayer);
            }
            catch (Exception ex)
            {
                throw HandleException(ex, "error getting player game history");
            }
        }

        private static Exception HandleException(Exception ex, string logMessage)
        {
            if (ex is BusinessLogicException businessEx)
            {
                var fault = new ServiceFaultDto(businessEx.ErrorType, LOGIC_ERROR_MESSAGE);
                return new FaultException<ServiceFaultDto>(fault, new FaultReason(businessEx.ErrorType.ToString()));
            }

            if (ex is DbUpdateException)
            {
                Logger.Error(ex, logMessage);
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, DB_SAVE_ERROR_MESSAGE);
                return new FaultException<ServiceFaultDto>(fault, new FaultReason(DATABASE_ERROR_REASON));
            }

            if (ex is SqlException || ex is EntityException)
            {
                Logger.Error(ex, logMessage);
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, DATABASE_UNAVAILABLE_MESSAGE);
                return new FaultException<ServiceFaultDto>(fault, new FaultReason(DATABASE_ERROR_REASON));
            }

            Logger.Error(ex, logMessage);
            var faultInternal = new ServiceFaultDto(ServiceErrorType.ServerInternalError, INTERNAL_SERVER_ERROR_MESSAGE);
            return new FaultException<ServiceFaultDto>(faultInternal, new FaultReason(INTERNAL_ERROR_REASON));
        }
    }
}
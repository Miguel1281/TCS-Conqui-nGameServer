using Autofac;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.Utilities.ExceptionHandler;
using NLog;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class UserProfile : IUserProfile
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IUserProfileLogic userProfileLogic;
        private readonly IServiceExceptionHandler exceptionHandler;

        public UserProfile()
        {
            Bootstrapper.Init();
            this.userProfileLogic = Bootstrapper.Container.Resolve<IUserProfileLogic>();
            this.exceptionHandler = Bootstrapper.Container.Resolve<IServiceExceptionHandler>();
        }

        public UserProfile(IUserProfileLogic userProfileLogic, IServiceExceptionHandler exceptionHandler)
        {
            this.userProfileLogic = userProfileLogic;
            this.exceptionHandler = exceptionHandler;
        }

        public async Task<PlayerDto> GetPlayerByIdAsync(int idPlayer)
        {
            PlayerDto foundPlayer;

            try
            {
                foundPlayer = await userProfileLogic.GetPlayerByIdAsync(idPlayer);
            }
            catch (Exception ex)
            {
                throw exceptionHandler.HandleException(ex, "GetPlayerByIdAsync");
            }

            return foundPlayer;
        }

        public async Task<List<SocialDto>> GetPlayerSocialsAsync(int idPlayer)
        {
            List<SocialDto> playerSocials;

            try
            {
                playerSocials = await userProfileLogic.GetPlayerSocialsAsync(idPlayer);
            }
            catch (Exception ex)
            {
                throw exceptionHandler.HandleException(ex, "GetPlayerSocialsAsync");
            }

            return playerSocials;
        }

        public async Task UpdatePlayerAsync(PlayerDto playerDto)
        {
            try
            {
                await userProfileLogic.UpdatePlayerAsync(playerDto);
            }
            catch (Exception ex)
            {
                throw exceptionHandler.HandleException(ex, "UpdatePlayerAsync");
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
                throw exceptionHandler.HandleException(ex, "UpdatePlayerSocialsAsync");
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
                throw exceptionHandler.HandleException(ex, "UpdateProfilePictureAsync");
            }
        }

        public async Task<List<GameHistoryDto>> GetPlayerGameHistoryAsync(int idPlayer)
        {
            List<GameHistoryDto> historyList;

            try
            {
                historyList = await userProfileLogic.GetPlayerGameHistoryAsync(idPlayer);
            }
            catch (Exception ex)
            {
                throw exceptionHandler.HandleException(ex, "GetPlayerGameHistoryAsync");
            }

            return historyList;
        }
    }
}

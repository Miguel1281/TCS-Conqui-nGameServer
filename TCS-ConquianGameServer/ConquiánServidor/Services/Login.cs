using Autofac;
using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.Utilities.ExceptionHandler;
using NLog;
using System;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Services
{
    public class Login : ILogin
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IAuthenticationLogic authenticationLogic;
        private readonly IGameSessionManager gameSessionManager;
        private readonly IServiceExceptionHandler exceptionHandler;


        public Login()
        {
            Bootstrapper.Init();
            this.authenticationLogic = Bootstrapper.Container.Resolve<IAuthenticationLogic>();
            this.gameSessionManager = Bootstrapper.Container.Resolve<IGameSessionManager>();
            this.exceptionHandler = Bootstrapper.Container.Resolve<IServiceExceptionHandler>();

        }

        public Login(IAuthenticationLogic authenticationLogic, IGameSessionManager gameSessionManager, IServiceExceptionHandler exceptionHandler)
        {
            this.authenticationLogic = authenticationLogic;
            this.gameSessionManager = gameSessionManager;
            this.exceptionHandler = exceptionHandler;

        }

        public async Task<PlayerDto> AuthenticatePlayerAsync(string email, string password)
        {
            try
            {
                var player = await authenticationLogic.AuthenticatePlayerAsync(email, password);

                if (player != null)
                {
                    ClearPlayerActiveSessions(player.idPlayer);
                }

                Logger.Info($"Player authenticated successfully: {player?.idPlayer ?? 0}");
                return player;
            }
            catch (Exception ex)
            {
                throw exceptionHandler.HandleException(ex, "AuthenticatePlayerAsync");
            }
        }

        private void ClearPlayerActiveSessions(int playerId)
        {
            try
            {
                gameSessionManager.CheckAndClearActiveSessions(playerId);
                Logger.Debug($"Active sessions cleared for player {playerId}");
            }
            catch (InvalidOperationException ex)
            {
                Logger.Warn(ex, $"Invalid operation clearing sessions for player {playerId}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error clearing active sessions for player {playerId}. Type: {ex.GetType().Name}");
            }
        }

        public async Task SignOutPlayerAsync(int idPlayer)
        {
            try
            {
                await authenticationLogic.SignOutPlayerAsync(idPlayer);
                Logger.Info($"Player {idPlayer} signed out successfully");
            }
            catch (Exception ex)
            {
                throw exceptionHandler.HandleException(ex, "SignOutPlayerAsync");
            }
        }
    }
}
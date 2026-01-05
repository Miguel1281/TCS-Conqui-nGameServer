using Autofac;
using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
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

        public Login()
        {
            Bootstrapper.Init();
            this.authenticationLogic = Bootstrapper.Container.Resolve<IAuthenticationLogic>();
            this.gameSessionManager = Bootstrapper.Container.Resolve<IGameSessionManager>();
        }

        public Login(IAuthenticationLogic authenticationLogic, IGameSessionManager gameSessionManager)
        {
            this.authenticationLogic = authenticationLogic;
            this.gameSessionManager = gameSessionManager;
        }

        public async Task<PlayerDto> AuthenticatePlayerAsync(string email, string password)
        {
            try
            {
                var player = await authenticationLogic.AuthenticatePlayerAsync(email, password);

                if (player != null)
                {
                    try
                    {
                        gameSessionManager.CheckAndClearActiveSessions(player.idPlayer);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, $"Error cleaning active sessions for player {player.idPlayer}");
                    }
                }

                return player;
            }
            catch (BusinessLogicException ex)
            {
                var faultData = new ServiceFaultDto(ex.ErrorType, "Logic Error");
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.ErrorType.ToString()));
            }
            catch (SqlException ex)
            {
                Logger.Error(ex, "SQL Error in Login");
                var faultData = new ServiceFaultDto(ServiceErrorType.DatabaseError, "Database Error");
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason("Database Unavailable"));
            }
            catch (EntityException ex)
            {
                Logger.Error(ex, "Entity Framework Error in Login");
                var faultData = new ServiceFaultDto(ServiceErrorType.DatabaseError, "Data Access Error");
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason("Database Unavailable"));
            }
            catch (TimeoutException ex)
            {
                Logger.Error(ex, "Timeout in Login");
                var faultData = new ServiceFaultDto(ServiceErrorType.CommunicationError, "Timeout");
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason("Request Timeout"));
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "Unexpected error in Login service");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, "Internal Server Error");
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason("Internal Error"));
            }
        }

        public async Task SignOutPlayerAsync(int idPlayer)
        {
            try
            {
                await authenticationLogic.SignOutPlayerAsync(idPlayer);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"SignOut failed for Player ID {idPlayer}");
                var faultData = new ServiceFaultDto(ServiceErrorType.OperationFailed, "Error closing session");
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason("Logout Error"));
            }
        }
    }
}
using Autofac;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.Utilities.ExceptionHandler;
using NLog;
using System;
using System.Threading.Tasks;

namespace ConquiánServidor.Services
{
    public class SignUp : ISignUp
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IAuthenticationLogic authenticationLogic;
        private readonly IServiceExceptionHandler exceptionHandler;

        public SignUp()
        {
            Bootstrapper.Init();
            this.authenticationLogic = Bootstrapper.Container.Resolve<IAuthenticationLogic>();
            this.exceptionHandler = Bootstrapper.Container.Resolve<IServiceExceptionHandler>();
        }

        public SignUp(IAuthenticationLogic authenticationLogic, IServiceExceptionHandler exceptionHandler)
        {
            this.authenticationLogic = authenticationLogic;
            this.exceptionHandler = exceptionHandler;
        }

        public async Task RegisterPlayerAsync(PlayerDto newPlayer)
        {
            try
            {
                await authenticationLogic.RegisterPlayerAsync(newPlayer);
            }
            catch (Exception ex)
            {
                throw exceptionHandler.HandleException(ex, "RegisterPlayerAsync");
            }
        }

        public async Task<string> SendVerificationCodeAsync(string email)
        {
            string sentCodeResult;

            try
            {
                sentCodeResult = await authenticationLogic.SendVerificationCodeAsync(email);
            }
            catch (Exception ex)
            {
                throw exceptionHandler.HandleException(ex, "SendVerificationCodeAsync");
            }

            return sentCodeResult;
        }

        public async Task VerifyCodeAsync(string email, string code)
        {
            try
            {
                await authenticationLogic.VerifyCodeAsync(email, code);
            }
            catch (Exception ex)
            {
                throw exceptionHandler.HandleException(ex, "VerifyCodeAsync");
            }
        }

        public async Task CancelRegistrationAsync(string email)
        {
            try
            {
                await authenticationLogic.DeleteTemporaryPlayerAsync(email);
            }
            catch (Exception ex)
            {
                throw exceptionHandler.HandleException(ex, "CancelRegistrationAsync");
            }
        }
    }
}
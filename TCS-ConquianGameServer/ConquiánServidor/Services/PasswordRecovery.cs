using Autofac;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.Utilities.Email;
using ConquiánServidor.Utilities.Email.Templates;
using ConquiánServidor.Utilities.ExceptionHandler;
using System;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class PasswordRecovery : IPasswordRecovery
    {

        private enum PasswordUpdateMode
        {
            Recovery = 0,
            Change = 1
        }
        private readonly IAuthenticationLogic authenticationLogic;
        private readonly IEmailService emailService;
        private readonly IServiceExceptionHandler exceptionHandler;

        public PasswordRecovery()
        {
            Bootstrapper.Init();
            this.authenticationLogic = Bootstrapper.Container.Resolve<IAuthenticationLogic>();
            this.emailService = Bootstrapper.Container.Resolve<IEmailService>();
            this.exceptionHandler = Bootstrapper.Container.Resolve<IServiceExceptionHandler>();
        }

        public PasswordRecovery(IAuthenticationLogic authenticationLogic, IEmailService emailService, IServiceExceptionHandler exceptionHandler)
        {
            this.authenticationLogic = authenticationLogic;
            this.emailService = emailService;
            this.exceptionHandler = exceptionHandler;
        }

        public async Task RequestPasswordRecoveryAsync(string email, int mode)
        {
            try
            {
                string token = await authenticationLogic.GenerateAndStoreRecoveryTokenAsync(email);

                IEmailTemplate emailTemplate;

                if (mode == (int)PasswordUpdateMode.Change)
                {
                    emailTemplate = new ChangePasswordEmailTemplate(token);
                }
                else
                {
                    emailTemplate = new RecoveryEmailTemplate(token);
                }

                await emailService.SendEmailAsync(email, emailTemplate);

            }
            catch (Exception ex)
            {
                throw exceptionHandler.HandleException(ex, "RequestPasswordRecoveryAsync");
            }
        }

        public async Task ValidateRecoveryTokenAsync(string email, string token)
        {
            try
            {
                await authenticationLogic.HandleTokenValidationAsync(email, token);
            }
            catch (Exception ex)
            {
                throw exceptionHandler.HandleException(ex, "ValidateRecoveryTokenAsync");
            }
        }

        public async Task ResetPasswordAsync(string email, string token, string newPassword)
        {
            try
            {
                await authenticationLogic.HandlePasswordResetAsync(email, token, newPassword);
            }
            catch (Exception ex)
            {
                throw exceptionHandler.HandleException(ex, "ResetPasswordAsync");
            }
        }
    }
}
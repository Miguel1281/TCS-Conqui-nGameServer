using Autofac;
using ConquiánServidor.BusinessLogic;
using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.DataAccess.Repositories;
using ConquiánServidor.Utilities.Email;
using ConquiánServidor.Utilities.Email.Templates;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Net.Mail;
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
        private readonly AuthenticationLogic authenticationLogic;

        private readonly IEmailService emailService;

        public PasswordRecovery()
        {
            Bootstrapper.Init();
            this.authenticationLogic = Bootstrapper.Container.Resolve<AuthenticationLogic>();
            this.emailService = Bootstrapper.Container.Resolve<IEmailService>();
        }

        public PasswordRecovery(AuthenticationLogic authenticationLogic, IEmailService emailService)
        {
            this.authenticationLogic = authenticationLogic;
            this.emailService = emailService;
        }

        public async Task<bool> RequestPasswordRecoveryAsync(string email, int mode)
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

                return true;
            }
            catch (BusinessLogicException ex)
            {
                var faultData = new ServiceFaultDto(ex.ErrorType, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.Message));
            }
            catch (SmtpException ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.CommunicationError, "Error al enviar el correo de recuperación.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.Message));
            }
            catch (Exception ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.OperationFailed, "Error inesperado en la solicitud de recuperación.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.Message));
            }
        }

        public async Task<bool> ValidateRecoveryTokenAsync(string email, string token)
        {
            try
            {
                await authenticationLogic.HandleTokenValidationAsync(email, token);
                return true;
            }
            catch (BusinessLogicException ex)
            {
                var faultData = new ServiceFaultDto(ex.ErrorType, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.Message));
            }
            catch (Exception ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.OperationFailed, "Error al validar el token.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.Message));
            }
        }

        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
        {
            try
            {
                await authenticationLogic.HandlePasswordResetAsync(email, token, newPassword);
                return true;
            }
            catch (BusinessLogicException ex)
            {
                var faultData = new ServiceFaultDto(ex.ErrorType, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.Message));
            }
            catch (DbUpdateException)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, "Error al guardar la nueva contraseña.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Error BD"));
            }
            catch (SqlException)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, "La base de datos no responde.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Error SQL"));
            }
            catch (Exception ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.OperationFailed, "Error inesperado al restablecer la contraseña.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.Message));
            }
        }
    }
}
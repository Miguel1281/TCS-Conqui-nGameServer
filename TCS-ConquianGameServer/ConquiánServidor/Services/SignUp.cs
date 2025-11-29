using Autofac;
using ConquiánServidor.BusinessLogic;
using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.DataAccess.Repositories;
using ConquiánServidor.Utilities.Email;
using NLog;
using System;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Net.Mail;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Services
{
    public class SignUp : ISignUp
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly AuthenticationLogic authenticationLogic;

        public SignUp()
        {
            Bootstrapper.Init();
            this.authenticationLogic = Bootstrapper.Container.Resolve<AuthenticationLogic>();
        }

        public SignUp(AuthenticationLogic authenticationLogic)
        {
            this.authenticationLogic = authenticationLogic;
        }

        public async Task<bool> RegisterPlayerAsync(PlayerDto newPlayer)
        {
            try
            {
                await authenticationLogic.RegisterPlayerAsync(newPlayer);
                return true;
            }
            catch (BusinessLogicException ex)
            {
                var fault = new ServiceFaultDto(ex.ErrorType, "Validation Error");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.ErrorType.ToString()));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "RegisterPlayerAsync");
            }
        }

        public async Task<string> SendVerificationCodeAsync(string email)
        {
            try
            {
                return await authenticationLogic.SendVerificationCodeAsync(email);
            }
            catch (BusinessLogicException ex)
            {
                var fault = new ServiceFaultDto(ex.ErrorType, "Validation Error");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.ErrorType.ToString()));
            }
            catch (SmtpException ex)
            {
                Logger.Error(ex, "SMTP Error sending verification code.");
                var fault = new ServiceFaultDto(ServiceErrorType.CommunicationError, "Email Service Error");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Email Error"));
            }
            catch (Exception ex)
            {
                HandleException(ex, "SendVerificationCodeAsync");
                return null;
            }
        }

        public async Task<bool> VerifyCodeAsync(string email, string code)
        {
            try
            {
                await authenticationLogic.VerifyCodeAsync(email, code);
                return true;
            }
            catch (BusinessLogicException ex)
            {
                var fault = new ServiceFaultDto(ex.ErrorType, "Verification Error");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.ErrorType.ToString()));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "VerifyCodeAsync");
            }
        }

        public async Task<bool> CancelRegistrationAsync(string email)
        {
            try
            {
                await authenticationLogic.DeleteTemporaryPlayerAsync(email);
                return true;
            }
            catch (Exception ex)
            {
                return HandleException(ex, "CancelRegistrationAsync");
            }
        }

        private bool HandleException(Exception ex, string context)
        {
            if (ex is SqlException || ex is EntityException)
            {
                Logger.Error(ex, $"Database error in {context}");
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, "Database Error");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Database Error"));
            }

            if (ex is TimeoutException)
            {
                Logger.Error(ex, $"Timeout in {context}");
                var fault = new ServiceFaultDto(ServiceErrorType.CommunicationError, "Operation Timed Out");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Timeout"));
            }

            Logger.Fatal(ex, $"Unexpected error in {context}");
            var genericFault = new ServiceFaultDto(ServiceErrorType.ServerInternalError, "Unexpected Error");
            throw new FaultException<ServiceFaultDto>(genericFault, new FaultReason("Internal Server Error"));
        }
    }
}
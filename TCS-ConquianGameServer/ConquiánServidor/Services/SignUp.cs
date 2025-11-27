using ConquiánServidor.BusinessLogic;
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.DataAccess.Repositories;
using ConquiánServidor.Utilities.Email;
using ConquiánServidor.Utilities.Messages;
using System;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Net.Mail;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Services
{
    public class SignUp : ISignUp
    {
        private readonly AuthenticationLogic authLogic;

        public SignUp()
        {
            var dbContext = new ConquiánDBEntities();
            IPlayerRepository playerRepository = new PlayerRepository(dbContext);
            IEmailService emailService = new EmailService();
            IMessageResolver messageResolver = new ResourceMessageResolver();
            authLogic = new AuthenticationLogic(playerRepository, emailService, messageResolver);
        }

        public async Task<bool> RegisterPlayerAsync(PlayerDto newPlayer)
        {
            try
            {
                await authLogic.RegisterPlayerAsync(newPlayer);
                return true;
            }
            catch (ArgumentException ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.ValidationFailed, ex.Message);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Validación fallida"));
            }
            catch (InvalidOperationException ex)
            {
                var type = ex.Message.Contains("nickname") ? ServiceErrorType.DuplicateRecord : ServiceErrorType.OperationFailed;
                var fault = new ServiceFaultDto(type, ex.Message);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.Message));
            }
            catch (DbUpdateException)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, "Error al guardar en la base de datos.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Error BD"));
            }
            catch (SqlException)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, "La base de datos no responde.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Error SQL"));
            }
            catch (Exception ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.Unknown, "Error inesperado en el registro.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Error Interno"));
            }
        }

        public async Task<string> SendVerificationCodeAsync(string email)
        {
            try
            {
                return await authLogic.SendVerificationCodeAsync(email);
            }
            catch (ArgumentException ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.ValidationFailed, ex.Message);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Email inválido"));
            }
            catch (InvalidOperationException ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.DuplicateRecord, "El correo ya está registrado.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Email duplicado"));
            }
            catch (SmtpException ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.CommunicationError, "No se pudo enviar el correo.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.Message));
            }
            catch (Exception ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.OperationFailed, "Error procesando la solicitud.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.Message));
            }
        }

        public async Task<bool> VerifyCodeAsync(string email, string code)
        {
            try
            {
                await authLogic.VerifyCodeAsync(email, code);
                return true;
            }
            catch (ArgumentException ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.ValidationFailed, ex.Message);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Código inválido"));
            }
            catch (Exception ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.OperationFailed, "Error verificando el código.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.Message));
            }
        }

        public async Task<bool> CancelRegistrationAsync(string email)
        {
            try
            {
                await authLogic.DeleteTemporaryPlayerAsync(email);
                return true;
            }
            catch (Exception ex)
            {
                var fault = new ServiceFaultDto(ServiceErrorType.DatabaseError, "Error al cancelar registro.");
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason("Error BD"));
            }
        }
    }
}
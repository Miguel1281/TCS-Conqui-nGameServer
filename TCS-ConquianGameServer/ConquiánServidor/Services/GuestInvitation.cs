using ConquiánServidor.BusinessLogic; 
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.ConquiánDB.Repositories;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.Properties.Langs;
using ConquiánServidor.Utilities.Email;
using ConquiánServidor.Utilities.Email.Templates;
using NLog;
using System;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class GuestInvitation : IGuestInvitation
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly ConquiánDBEntities dbContext;
        private readonly IEmailService emailService;
        private readonly ILobbyRepository lobbyRepository;

        public GuestInvitation()
        {
            dbContext = new ConquiánDBEntities();
            emailService = new EmailService();
            lobbyRepository = new LobbyRepository(dbContext);
        }

        public async Task SendGuestInviteAsync(string roomCode, string email)
        {
            try
            {
                var lobby = await lobbyRepository.GetLobbyByRoomCodeAsync(roomCode);
                if (lobby == null)
                {
                    throw new InvalidOperationException(Lang.ErrorLobbyNotFound);
                }

                GuestInvitationManager.Instance.AddInvitation(email, roomCode);

                try
                {
                    var emailTemplate = new GuestInviteEmailTemplate(roomCode);
                    await emailService.SendEmailAsync(email, emailTemplate);
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, $"Fallo envío de correo invitado a {email}. Revirtiendo cambios en memoria.");
                    throw new InvalidOperationException(string.Format(Lang.ErrorGuestInviteEmailFailed, email));
                }

                Logger.Info(string.Format(Lang.LogGuestInviteSent, email, roomCode));
            }
            catch (InvalidOperationException ex)
            {
                var faultData = new ServiceFaultDto(ServiceErrorType.OperationFailed, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.Message));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error crítico enviando invitación huésped a {email} para sala {roomCode}");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, Lang.ErrorInvitationFailed);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(Lang.InternalServerError));
            }
        }
    }
}
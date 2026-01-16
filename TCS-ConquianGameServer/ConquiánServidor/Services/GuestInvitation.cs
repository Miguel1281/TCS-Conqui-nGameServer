using Autofac;
using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
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
        private readonly IGuestInvitationLogic guestInvitationLogic;


        public GuestInvitation()
        {
            Bootstrapper.Init();
            this.guestInvitationLogic = Bootstrapper.Container.Resolve<IGuestInvitationLogic>();
        }

        public GuestInvitation(IGuestInvitationLogic guestInvitationLogic)
        {
            this.guestInvitationLogic =
                guestInvitationLogic ?? throw new ArgumentNullException(nameof(guestInvitationLogic));
        }

        public async Task SendGuestInviteAsync(string roomCode, string email)
        {
            try
            {
                await guestInvitationLogic.SendGuestInviteAsync(roomCode, email);
            }
            catch (BusinessLogicException ex)
            {
                Logger.Debug($"Business logic error: {ex.ErrorType} - {ex.Message}");
                var faultData = new ServiceFaultDto(ex.ErrorType, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.Message));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error in service layer. Type: {ex.GetType().Name}");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, "Internal server error");
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason("Internal server error"));
            }
        }
    }
}
using Autofac;
using ConquiánServidor.BusinessLogic;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.Properties.Langs;
using System;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class Invitation : IInvitationService
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly InvitationManager invitationManager;

        public Invitation()
        {
            Bootstrapper.Init();
            this.invitationManager = Bootstrapper.Container.Resolve<InvitationManager>();
        }

        public Invitation(InvitationManager invitationManager)
        {
            this.invitationManager = invitationManager;
        }
        public void Subscribe(int idPlayer)
        {
            try
            {
                var currentCallback = OperationContext.Current.GetCallbackChannel<IInvitationCallback>();
                invitationManager.Subscribe(idPlayer, currentCallback);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error en Subscribe para jugador {idPlayer}");
            }
        }

        public void Unsubscribe(int idPlayer)
        {
            try
            {
                invitationManager.Unsubscribe(idPlayer);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error en Unsubscribe para jugador {idPlayer}");
            }
        }

        public async Task SendInvitationAsync(int idSender, string senderNickname, int idReceiver, string roomCode)
        {
            try
            {
                await invitationManager.SendInvitationAsync(idSender, senderNickname, idReceiver, roomCode);
            }
            catch (InvalidOperationException ex)
            {
                var faultData = new ServiceFaultDto(ServiceErrorType.OperationFailed, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.Message));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error crítico enviando invitación de {idSender} a {idReceiver}");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, Lang.ErrorInvitationFailed);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason("Internal Server Error"));
            }
        }
    }
}
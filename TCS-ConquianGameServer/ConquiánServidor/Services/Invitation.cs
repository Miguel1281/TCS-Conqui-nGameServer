using Autofac;
using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using System;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class Invitation : IInvitationService
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IInvitationManager invitationManager;

        public Invitation()
        {
            Bootstrapper.Init();
            this.invitationManager = Bootstrapper.Container.Resolve<IInvitationManager>();
        }

        public Invitation(IInvitationManager invitationManager)
        {
            this.invitationManager = invitationManager;
        }
        public void Subscribe(int idPlayer)
        {
            try
            {
                var currentCallback = OperationContext.Current.GetCallbackChannel<IInvitationCallback>();
                invitationManager.Subscribe(idPlayer, currentCallback);
                Logger.Debug($"Player {idPlayer} subscribed to invitation service");
            }
            catch (BusinessLogicException ex)
            {
                Logger.Warn(ex, $"Business logic error subscribing player {idPlayer}. ErrorType: {ex.ErrorType}");
                var fault = new ServiceFaultDto(ex.ErrorType, ex.Message);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.Message));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error subscribing player {idPlayer}. Type: {ex.GetType().Name}");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, "Internal server error");
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason("Internal server error"));
            }
        }

        public void Unsubscribe(int idPlayer)
        {
            try
            {
                invitationManager.Unsubscribe(idPlayer);
                Logger.Debug($"Player {idPlayer} unsubscribed from invitation service");
            }
            catch (BusinessLogicException ex)
            {
                Logger.Warn(ex, $"Business logic error unsubscribing player {idPlayer}. ErrorType: {ex.ErrorType}");
                var fault = new ServiceFaultDto(ex.ErrorType, ex.Message);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.Message));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error unsubscribing player {idPlayer}. Type: {ex.GetType().Name}");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, "Internal server error");
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason("Internal server error"));
            }
        }

        public async Task SendInvitationAsync(InvitationSenderDto sender, int idReceiver, string roomCode)
        {
            try
            {
                await invitationManager.SendInvitationAsync(sender, idReceiver, roomCode);
                Logger.Info($"Invitation sent from {sender?.IdPlayer} to {idReceiver} for room {roomCode}");
            }
            catch (BusinessLogicException ex)
            {
                Logger.Debug($"Business logic error sending invitation. ErrorType: {ex.ErrorType}");
                var fault = new ServiceFaultDto(ex.ErrorType, ex.Message);
                throw new FaultException<ServiceFaultDto>(fault, new FaultReason(ex.Message));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error sending invitation from {sender?.IdPlayer} to {idReceiver}. Type: {ex.GetType().Name}");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, "Internal server error");
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason("Internal server error"));
            }
        }
    }
}
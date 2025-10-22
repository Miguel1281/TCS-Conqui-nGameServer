using ConquiánServidor.BusinessLogic;
using ConquiánServidor.Contracts.ServiceContracts;
using System.Collections.Concurrent;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class Invitation : IInvitationService
    {
        private readonly InvitationManager manager = InvitationManager.Instance;

        private int currentPlayerId;
        private IInvitationCallback currentCallback;

        public void Subscribe(int idPlayer)
        {
            this.currentPlayerId = idPlayer;
            this.currentCallback = OperationContext.Current.GetCallbackChannel<IInvitationCallback>();
            manager.Subscribe(idPlayer, this.currentCallback);
        }

        public void Unsubscribe(int idPlayer)
        {
            manager.Unsubscribe(idPlayer);
            this.currentPlayerId = 0;
            this.currentCallback = null;
        }

        public Task<bool> SendInvitationAsync(int idSender, string senderNickname, int idReceiver, string roomCode)
        {
            return manager.SendInvitationAsync(idSender, senderNickname, idReceiver, roomCode);
        }
    }
}
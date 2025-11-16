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
        public void Subscribe(int idPlayer)
        {
            var currentCallback = OperationContext.Current.GetCallbackChannel<IInvitationCallback>();
            manager.Subscribe(idPlayer, currentCallback);
        }

        public void Unsubscribe(int idPlayer)
        {
            manager.Unsubscribe(idPlayer);
        }

        public Task<bool> SendInvitationAsync(int idSender, string senderNickname, int idReceiver, string roomCode)
        {
            return manager.SendInvitationAsync(idSender, senderNickname, idReceiver, roomCode);
        }
    }
}
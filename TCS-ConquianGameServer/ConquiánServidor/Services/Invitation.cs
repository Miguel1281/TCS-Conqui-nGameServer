using ConquiánServidor.Contracts.ServiceContracts;
using System.Collections.Concurrent;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class Invitation : IInvitationService
    {
        private static readonly ConcurrentDictionary<int, IInvitationCallback> onlinePlayers =
            new ConcurrentDictionary<int, IInvitationCallback>();

        private int currentPlayerId;
        private IInvitationCallback currentCallback;

        public void Subscribe(int idPlayer)
        {
            this.currentPlayerId = idPlayer;
            this.currentCallback = OperationContext.Current.GetCallbackChannel<IInvitationCallback>();
            onlinePlayers.TryAdd(idPlayer, this.currentCallback);
        }

        public void Unsubscribe(int idPlayer)
        {
            onlinePlayers.TryRemove(idPlayer, out _);
            this.currentPlayerId = 0;
            this.currentCallback = null;
        }

        public Task<bool> SendInvitationAsync(int idSender, string senderNickname, int idReceiver, string roomCode)
        {
            if (onlinePlayers.TryGetValue(idReceiver, out IInvitationCallback receiverCallback))
            {
                try
                {
                    receiverCallback.OnInvitationReceived(senderNickname, roomCode);
                    return Task.FromResult(true);
                }
                catch (CommunicationException)
                {
                    onlinePlayers.TryRemove(idReceiver, out _);
                    return Task.FromResult(false);
                }
            }
            return Task.FromResult(false);
        }
    }
}
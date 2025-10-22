using ConquiánServidor.Contracts.ServiceContracts;
using System;
using System.Collections.Concurrent;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic
{
    public class InvitationManager
    {
        private static readonly Lazy<InvitationManager> instance =
            new Lazy<InvitationManager>(() => new InvitationManager());

        private readonly ConcurrentDictionary<int, IInvitationCallback> onlinePlayers =
            new ConcurrentDictionary<int, IInvitationCallback>();

        private InvitationManager() { }

        public static InvitationManager Instance => instance.Value;

        public void Subscribe(int idPlayer, IInvitationCallback callback)
        {
            onlinePlayers.TryAdd(idPlayer, callback);
        }

        public void Unsubscribe(int idPlayer)
        {
            onlinePlayers.TryRemove(idPlayer, out _);
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
                catch (Exception)
                {
                    onlinePlayers.TryRemove(idReceiver, out _);
                    return Task.FromResult(false);
                }
            }
            return Task.FromResult(false); 
        }
    }
}
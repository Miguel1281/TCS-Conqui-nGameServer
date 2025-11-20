using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.Properties.Langs; // Importante
using NLog;
using System;
using System.Collections.Concurrent;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic
{
    public class InvitationManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly Lazy<InvitationManager> instance =
            new Lazy<InvitationManager>(() => new InvitationManager());

        private readonly ConcurrentDictionary<int, IInvitationCallback> onlinePlayers =
            new ConcurrentDictionary<int, IInvitationCallback>();

        private InvitationManager() { }

        public static InvitationManager Instance => instance.Value;

        public void Subscribe(int idPlayer, IInvitationCallback callback)
        {
            onlinePlayers.AddOrUpdate(idPlayer, callback, (key, oldValue) => callback);
            Logger.Info(string.Format(Lang.LogInvitationSubscribed, idPlayer));
        }

        public void Unsubscribe(int idPlayer)
        {
            if (onlinePlayers.TryRemove(idPlayer, out _))
            {
                Logger.Info(string.Format(Lang.LogInvitationUnsubscribed, idPlayer));
            }
        }

        public async Task SendInvitationAsync(int idSender, string senderNickname, int idReceiver, string roomCode)
        {
            if (onlinePlayers.TryGetValue(idReceiver, out IInvitationCallback receiverCallback))
            {
                try
                {
                    receiverCallback.OnInvitationReceived(senderNickname, roomCode);

                    Logger.Info(string.Format(Lang.LogInvitationSent, senderNickname, idReceiver, roomCode));
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, string.Format(Lang.LogInvitationDeliveryFailed, idReceiver));
                    onlinePlayers.TryRemove(idReceiver, out _);
                    throw new InvalidOperationException(Lang.ErrorPlayerOffline);
                }
            }
            else
            {
                Logger.Warn(string.Format(Lang.LogInvitationDeliveryFailed, idReceiver) + " (No encontrado en diccionario)");
                throw new InvalidOperationException(Lang.ErrorPlayerOffline);
            }

            await Task.CompletedTask;
        }
    }
}
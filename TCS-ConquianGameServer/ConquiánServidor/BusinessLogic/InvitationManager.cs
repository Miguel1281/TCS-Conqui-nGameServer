using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.Properties.Langs;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic
{
    public class InvitationManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly ConcurrentDictionary<int, IInvitationCallback> onlinePlayers =
            new ConcurrentDictionary<int, IInvitationCallback>();

        public InvitationManager()
        {
        }

        public void Subscribe(int idPlayer, IInvitationCallback callback)
        {
            onlinePlayers.AddOrUpdate(idPlayer, callback, (key, oldValue) => callback);
            Logger.Info($"Player ID {idPlayer} subscribed to invitation service.");
        }

        public void Unsubscribe(int idPlayer)
        {
            if (onlinePlayers.TryRemove(idPlayer, out _))
            {
                Logger.Info($"Player ID {idPlayer} unsubscribed from invitation service.");
            }
        }

        public async Task SendInvitationAsync(int idSender, string senderNickname, int idReceiver, string roomCode)
        {
            Logger.Info($"Invitation attempt: Sender ID {idSender} -> Receiver ID {idReceiver} for Room Code: {roomCode}");

            if (onlinePlayers.TryGetValue(idReceiver, out IInvitationCallback receiverCallback))
            {
                try
                {
                    receiverCallback.OnInvitationReceived(senderNickname, roomCode);

                    Logger.Info($"Invitation successfully delivered to Receiver ID: {idReceiver}");
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, $"Invitation delivery failed for Receiver ID: {idReceiver}. Removing from active list.");
                    onlinePlayers.TryRemove(idReceiver, out _);
                    throw new BusinessLogicException(ServiceErrorType.OperationFailed);
                }
            }
            else
            {
                Logger.Warn($"Invitation failed: Receiver ID {idReceiver} is not online/subscribed.");
                throw new BusinessLogicException(ServiceErrorType.OperationFailed);
            }

            await Task.CompletedTask;
        }
    }
}
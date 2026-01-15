using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using NLog;
using System;
using System.Collections.Concurrent;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic.Lobby
{
    public class InvitationManager:IInvitationManager 
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly ConcurrentDictionary<int, IInvitationCallback> onlinePlayers = new ConcurrentDictionary<int, IInvitationCallback>();
        private readonly IPresenceManager presenceManager;

        public InvitationManager(IPresenceManager presenceManager)
        {
            this.presenceManager = presenceManager;
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

            if (presenceManager.IsPlayerInGame(idReceiver))
            {
                Logger.Warn($"Invitation blocked: Receiver ID {idReceiver} is currently IN GAME.");

                throw new BusinessLogicException(ServiceErrorType.UserInGame);
            }

            if (presenceManager.IsPlayerInLobby(idReceiver))
            {
                Logger.Warn($"Invitation blocked: Receiver ID {idReceiver} is currently IN LOBBY.");
                throw new BusinessLogicException(ServiceErrorType.UserInLobby);
            }

            if (!onlinePlayers.TryGetValue(idReceiver, out IInvitationCallback receiverCallback))
            {
                Logger.Warn($"Invitation failed: Receiver ID {idReceiver} is not online/subscribed.");
                throw new BusinessLogicException(ServiceErrorType.UserOffline);
            }

            try
            {
                receiverCallback.OnInvitationReceived(senderNickname, roomCode);
                Logger.Info($"Invitation successfully delivered to Receiver ID: {idReceiver}");
            }
            catch (CommunicationException ex)
            {
                Logger.Warn(ex, $"Communication error when delivering invitation to Receiver ID: {idReceiver}. Removing from active list.");
                onlinePlayers.TryRemove(idReceiver, out _);
                throw new BusinessLogicException(ServiceErrorType.CommunicationError);
            }
            catch (TimeoutException ex)
            {
                Logger.Warn(ex, $"Timeout when delivering invitation to Receiver ID: {idReceiver}. Removing from active list.");
                onlinePlayers.TryRemove(idReceiver, out _);
                throw new BusinessLogicException(ServiceErrorType.CommunicationError);
            }
            catch (ObjectDisposedException ex)
            {
                Logger.Warn(ex, $"Channel disposed when delivering invitation to Receiver ID: {idReceiver}. Removing from active list.");
                onlinePlayers.TryRemove(idReceiver, out _);
                throw new BusinessLogicException(ServiceErrorType.UserOffline);
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error(ex, $"Invalid operation when delivering invitation to Receiver ID: {idReceiver}. Removing from active list.");
                onlinePlayers.TryRemove(idReceiver, out _);
                throw new BusinessLogicException(ServiceErrorType.OperationFailed);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error when delivering invitation to Receiver ID: {idReceiver}. Exception type: {ex.GetType().Name}");
                onlinePlayers.TryRemove(idReceiver, out _);
                throw new BusinessLogicException(ServiceErrorType.ServerInternalError);
            }

            await Task.CompletedTask;
        }
    }
}
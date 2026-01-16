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

        public async Task SendInvitationAsync(InvitationSenderDto sender, int idReceiver, string roomCode)
        {
            ValidateInvitationRequest(sender, idReceiver, roomCode);

            Logger.Info($"Invitation attempt: Sender ID {sender.IdPlayer} -> Receiver ID {idReceiver} for Room Code: {roomCode}");

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

            await DeliverInvitationAsync(receiverCallback, sender.Nickname, roomCode, idReceiver);
        }

        private void ValidateInvitationRequest(InvitationSenderDto sender, int idReceiver, string roomCode)
        {
            if (sender == null)
            {
                Logger.Error("SendInvitationAsync called with null sender");
                throw new ArgumentNullException(nameof(sender), "Sender information is required");
            }

            if (sender.IdPlayer <= 0)
            {
                Logger.Error($"Invalid sender ID: {sender.IdPlayer}");
                throw new BusinessLogicException(ServiceErrorType.ValidationFailed, "Invalid sender ID");
            }

            if (string.IsNullOrWhiteSpace(sender.Nickname))
            {
                Logger.Error($"Sender ID {sender.IdPlayer} has null or empty nickname");
                throw new BusinessLogicException(ServiceErrorType.ValidationFailed, "Sender nickname is required");
            }

            if (idReceiver <= 0)
            {
                Logger.Error($"Invalid receiver ID: {idReceiver}");
                throw new BusinessLogicException(ServiceErrorType.ValidationFailed, "Invalid receiver ID");
            }

            if (string.IsNullOrWhiteSpace(roomCode))
            {
                Logger.Error($"Invitation from {sender.IdPlayer} to {idReceiver} has null or empty room code");
                throw new BusinessLogicException(ServiceErrorType.ValidationFailed, "Room code is required");
            }

            if (sender.IdPlayer == idReceiver)
            {
                Logger.Warn($"Player {sender.IdPlayer} attempted to send invitation to themselves");
                throw new BusinessLogicException(ServiceErrorType.ValidationFailed, "Cannot send invitation to yourself");
            }
        }

        private async Task DeliverInvitationAsync(IInvitationCallback receiverCallback, string senderNickname, string roomCode, int idReceiver)
        {
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
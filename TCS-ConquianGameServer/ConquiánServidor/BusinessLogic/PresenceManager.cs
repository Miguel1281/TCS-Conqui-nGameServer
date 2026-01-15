using Autofac;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.Enums;
using ConquiánServidor.Contracts.ServiceContracts;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic
{
    public class PresenceManager : IPresenceManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<int, IPresenceCallback> onlineSubscribers = new Dictionary<int, IPresenceCallback>();
        private readonly ConcurrentDictionary<int, PlayerStatus> playerStatuses = new ConcurrentDictionary<int, PlayerStatus>();

        private readonly object lockObj = new object();
        private readonly ILifetimeScope lifetimeScope;

        public PresenceManager(ILifetimeScope scope)
        {
            this.lifetimeScope = scope;
        }

        public async void DisconnectUser(int idPlayer)
        {
            try
            {
                using (var scope = this.lifetimeScope.BeginLifetimeScope())
                {
                    var lobbyLogic = scope.Resolve<ILobbyLogic>();
                    var lobbySessionManager = scope.Resolve<ILobbySessionManager>();
                    var gameSessionManager = scope.Resolve<IGameSessionManager>();

                    await RemovePlayerFromLobbyAsync(idPlayer, lobbyLogic, lobbySessionManager);

                    ClearPlayerGameSessions(idPlayer, gameSessionManager);
                }
            }
            catch (ObjectDisposedException ex)
            {
                Logger.Error(ex, $"Lifetime scope disposed while disconnecting player {idPlayer}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Critical error during player disconnection {idPlayer}. Type: {ex.GetType().Name}");
            }

            Unsubscribe(idPlayer);

            await NotifyStatusChange(idPlayer, (int)PlayerStatus.Offline);

            Logger.Info($"Player {idPlayer} disconnected and successfully marked as Offline.");
        }

        private async Task RemovePlayerFromLobbyAsync(int idPlayer, ILobbyLogic lobbyLogic, ILobbySessionManager lobbySessionManager)
        {
            try
            {
                string roomCode = lobbySessionManager.GetLobbyCodeForPlayer(idPlayer);
                if (!string.IsNullOrEmpty(roomCode))
                {
                    await lobbyLogic.LeaveLobbyAsync(roomCode, idPlayer);
                    Logger.Info($"Player {idPlayer} successfully removed from lobby {roomCode}");
                }
            }
            catch (FaultException<ServiceFaultDto> ex)
            {
                if (ex.Detail.ErrorType == ServiceErrorType.NotFound || 
                    ex.Detail.ErrorType == ServiceErrorType.LobbyNotFound)
                {
                    Logger.Debug($"Player {idPlayer} was not in any lobby. Error: {ex.Detail.ErrorType}");
                }
                else
                {
                    Logger.Warn(ex, $"Service fault removing player {idPlayer} from lobby. Error: {ex.Detail.ErrorType}");
                }
            }
            catch (BusinessLogicException ex)
            {
                Logger.Warn(ex, $"Business logic error removing player {idPlayer} from lobby. ErrorType: {ex.ErrorType}");
            }
            catch (CommunicationException ex)
            {
                Logger.Error(ex, $"Communication error removing player {idPlayer} from lobby");
            }
            catch (TimeoutException ex)
            {
                Logger.Error(ex, $"Timeout removing player {idPlayer} from lobby");
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error(ex, $"Invalid operation removing player {idPlayer} from lobby");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error removing player {idPlayer} from lobby. Type: {ex.GetType().Name}");
            }
        }

        private void ClearPlayerGameSessions(int idPlayer, IGameSessionManager gameSessionManager)
        {
            try
            {
                gameSessionManager.CheckAndClearActiveSessions(idPlayer);
                Logger.Info($"Game sessions cleared for player {idPlayer}");
            }
            catch (FaultException<ServiceFaultDto> ex)
            {
                if (ex.Detail.ErrorType == ServiceErrorType.GameNotFound)
                {
                    Logger.Debug($"Player {idPlayer} had no active game sessions");
                }
                else
                {
                    Logger.Warn(ex, $"Service fault clearing game sessions for player {idPlayer}. Error: {ex.Detail.ErrorType}");
                }
            }
            catch (BusinessLogicException ex)
            {
                Logger.Warn(ex, $"Business logic error clearing game sessions for player {idPlayer}. ErrorType: {ex.ErrorType}");
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error(ex, $"Invalid operation clearing game sessions for player {idPlayer}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error clearing game sessions for player {idPlayer}. Type: {ex.GetType().Name}");
            }
        }

        public virtual bool IsPlayerOnline(int idPlayer)
        {
            bool isOnline = false;

            if (playerStatuses.TryGetValue(idPlayer, out PlayerStatus status) &&
                (status == PlayerStatus.Online || status == PlayerStatus.InGame))
            {
                isOnline = true;
            }
            else
            {
                lock (lockObj)
                {
                    isOnline = onlineSubscribers.ContainsKey(idPlayer);
                }
            }

            return isOnline;
        }

        public void Subscribe(int idPlayer, IPresenceCallback callback)
        {
            if (callback == null)
            {
                Logger.Error($"Cannot subscribe Player ID {idPlayer}: callback is null");
                throw new ArgumentNullException(nameof(callback), "Callback cannot be null");
            }

            lock (lockObj)
            {
                onlineSubscribers[idPlayer] = callback;
            }

            playerStatuses.AddOrUpdate(idPlayer, PlayerStatus.Online, (key, oldVal) => PlayerStatus.Online);
            Logger.Info($"Player {idPlayer} subscribed to presence service with status Online");
        }

        public void Unsubscribe(int idPlayer)
        {
            lock (lockObj)
            {
                if (onlineSubscribers.Remove(idPlayer))
                {
                    Logger.Info($"Player {idPlayer} unsubscribed from presence service");
                }
            }

            playerStatuses.TryRemove(idPlayer, out _);
        }

        public virtual async Task NotifyStatusChange(int changedPlayerId, int newStatusId)
        {
            UpdatePlayerStatus(changedPlayerId, (PlayerStatus)newStatusId);

            var friends = await FetchFriendsSafeAsync(changedPlayerId);
            if (friends == null || friends.Count == 0)
            {
                Logger.Debug($"No friends to notify for player {changedPlayerId}");
                return;
            }

            var callbacks = GetActiveCallbacks(friends);

            foreach (var callback in callbacks)
            {
                NotifySingleFriendSafe(callback, changedPlayerId, newStatusId);
            }

            Logger.Info($"Status change notification sent for player {changedPlayerId} to {callbacks.Count} friends");
        }

        private void UpdatePlayerStatus(int playerId, PlayerStatus status)
        {
            if (status == PlayerStatus.Offline)
            {
                playerStatuses.TryRemove(playerId, out _);
                Logger.Debug($"Player {playerId} status removed (Offline)");
            }
            else
            {
                playerStatuses.AddOrUpdate(playerId, status, (key, oldVal) => status);
                Logger.Debug($"Player {playerId} status updated to {status}");
            }
        }

        private async Task<List<PlayerDto>> FetchFriendsSafeAsync(int playerId)
        {
            try
            {
                using (var scope = this.lifetimeScope.BeginLifetimeScope())
                {
                    var friendshipLogic = scope.Resolve<IFriendshipLogic>();
                    var friends = await friendshipLogic.GetFriendsAsync(playerId);
                    var dtos = friends;
                    if (dtos != null)
                    {
                        return dtos;
                    }

                    return new List<PlayerDto>();
                }
            }
            catch (FaultException<ServiceFaultDto> ex)
            {
                Logger.Warn(ex, $"Service fault fetching friends for player {playerId}. Error: {ex.Detail.ErrorType}");
                return new List<PlayerDto>();
            }
            catch (BusinessLogicException ex)
            {
                Logger.Warn(ex, $"Business logic error fetching friends for player {playerId}. ErrorType: {ex.ErrorType}");
                return new List<PlayerDto>();
            }
            catch (SqlException ex)
            {
                Logger.Error(ex, $"Database error fetching friends for player {playerId}. SqlError: {ex.Number}");
                return new List<PlayerDto>();
            }
            catch (TimeoutException ex)
            {
                Logger.Error(ex, $"Timeout fetching friends for player {playerId}");
                return new List<PlayerDto>();
            }
            catch (CommunicationException ex)
            {
                Logger.Error(ex, $"Communication error fetching friends for player {playerId}");
                return new List<PlayerDto>();
            }
            catch (ObjectDisposedException ex)
            {
                Logger.Error(ex, $"Lifetime scope disposed while fetching friends for player {playerId}");
                return new List<PlayerDto>();
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error(ex, $"Invalid operation fetching friends for player {playerId}");
                return new List<PlayerDto>();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error fetching friends for player {playerId}. Type: {ex.GetType().Name}");
                return new List<PlayerDto>();
            }
        }

        private List<IPresenceCallback> GetActiveCallbacks(List<PlayerDto> friends)
        {
            var activeCallbacks = new List<IPresenceCallback>();
            
            lock (lockObj)
            {
                foreach (var friend in friends)
                {
                    if (onlineSubscribers.TryGetValue(friend.idPlayer, out IPresenceCallback callback))
                    {
                        activeCallbacks.Add(callback);
                    }
                }
            }
            
            return activeCallbacks;
        }

        private static void NotifySingleFriendSafe(IPresenceCallback callback, int playerId, int statusId)
        {
            try
            {
                var commObj = callback as ICommunicationObject;
                if (commObj != null && commObj.State == CommunicationState.Opened)
                {
                    callback.OnFriendStatusChanged(playerId, statusId);
                }
                else
                {
                    Logger.Debug($"Callback channel not in Opened state for status notification. State: {commObj?.State}");
                }
            }
            catch (CommunicationException ex)
            {
                Logger.Warn(ex, $"Communication failure notifying player about status change of {playerId}");
            }
            catch (TimeoutException ex)
            {
                Logger.Warn(ex, $"Timeout notifying player about status change of {playerId}");
            }
            catch (ObjectDisposedException ex)
            {
                Logger.Warn(ex, $"Channel disposed when notifying player about status change of {playerId}");
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error(ex, $"Invalid operation notifying player about status change of {playerId}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error during status change notification of {playerId}. Type: {ex.GetType().Name}");
            }
        }

        public void NotifyNewFriendRequest(int targetUserId)
        {
            IPresenceCallback callback = null;

            lock (lockObj)
            {
                if (!onlineSubscribers.TryGetValue(targetUserId, out callback))
                {
                    Logger.Debug($"User {targetUserId} is not online to receive friend request notification");
                    return;
                }
            }

            try
            {
                var commObj = callback as ICommunicationObject;
                if (commObj != null && commObj.State == CommunicationState.Opened)
                {
                    callback.OnFriendRequestReceived();
                    Logger.Info($"Friend request notification sent to user {targetUserId}");
                }
                else
                {
                    Logger.Debug($"Callback channel not in Opened state for user {targetUserId}. State: {commObj?.State}");
                }
            }
            catch (CommunicationException ex)
            {
                Logger.Warn(ex, $"Communication error notifying friend request to user {targetUserId}");
                RemoveInactiveSubscriber(targetUserId);
            }
            catch (TimeoutException ex)
            {
                Logger.Warn(ex, $"Timeout notifying friend request to user {targetUserId}");
                RemoveInactiveSubscriber(targetUserId);
            }
            catch (ObjectDisposedException ex)
            {
                Logger.Warn(ex, $"Channel disposed when notifying friend request to user {targetUserId}");
                RemoveInactiveSubscriber(targetUserId);
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error(ex, $"Invalid operation notifying friend request to user {targetUserId}");
                RemoveInactiveSubscriber(targetUserId);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error notifying friend request to user {targetUserId}. Type: {ex.GetType().Name}");
            }
        }

        public void NotifyFriendListUpdate(int targetUserId)
        {
            IPresenceCallback callback = null;

            lock (lockObj)
            {
                if (!onlineSubscribers.TryGetValue(targetUserId, out callback))
                {
                    Logger.Debug($"User {targetUserId} is not online to receive friend list update notification");
                    return;
                }
            }

            try
            {
                var commObj = callback as ICommunicationObject;
                if (commObj != null && commObj.State == CommunicationState.Opened)
                {
                    callback.OnFriendListUpdated();
                    Logger.Info($"Friend list update notification sent to user {targetUserId}");
                }
                else
                {
                    Logger.Debug($"Callback channel not in Opened state for user {targetUserId}. State: {commObj?.State}");
                }
            }
            catch (CommunicationException ex)
            {
                Logger.Warn(ex, $"Communication error notifying friend list update to user {targetUserId}");
                RemoveInactiveSubscriber(targetUserId);
            }
            catch (TimeoutException ex)
            {
                Logger.Warn(ex, $"Timeout notifying friend list update to user {targetUserId}");
                RemoveInactiveSubscriber(targetUserId);
            }
            catch (ObjectDisposedException ex)
            {
                Logger.Warn(ex, $"Channel disposed when notifying friend list update to user {targetUserId}");
                RemoveInactiveSubscriber(targetUserId);
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error(ex, $"Invalid operation notifying friend list update to user {targetUserId}");
                RemoveInactiveSubscriber(targetUserId);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error notifying friend list update to user {targetUserId}. Type: {ex.GetType().Name}");
            }
        }

        private void RemoveInactiveSubscriber(int userId)
        {
            lock (lockObj)
            {
                if (onlineSubscribers.Remove(userId))
                {
                    Logger.Info($"Removed inactive subscriber: user {userId}");
                }
            }
            playerStatuses.TryRemove(userId, out _);
        }

        public bool IsPlayerInGame(int playerId)
        {
            bool inGame = false;

            if (playerStatuses.TryGetValue(playerId, out PlayerStatus status))
            {
                inGame = (status == PlayerStatus.InGame);
            }

            return inGame;
        }

        public bool IsPlayerInLobby(int playerId)
        {
            bool inLobby = false;

            if (playerStatuses.TryGetValue(playerId, out PlayerStatus status))
            {
                inLobby = (status == PlayerStatus.InLobby);
            }

            return inLobby;
        }
    }
}
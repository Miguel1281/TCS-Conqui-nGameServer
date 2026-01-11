using Autofac;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.Enums;
using ConquiánServidor.Contracts.ServiceContracts;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
            Logger.Info($"Detectada desconexión del jugador {idPlayer}. Limpiando sesión...");

            try
            {
                using (var scope = this.lifetimeScope.BeginLifetimeScope())
                {
                    var lobbyLogic = scope.Resolve<ILobbyLogic>();
                    var lobbySessionManager = scope.Resolve<ILobbySessionManager>();
                    var gameSessionManager = scope.Resolve<IGameSessionManager>();

                    try
                    {
                        string roomCode = lobbySessionManager.GetLobbyCodeForPlayer(idPlayer);
                        if (!string.IsNullOrEmpty(roomCode))
                        {
                            await lobbyLogic.LeaveLobbyAsync(roomCode, idPlayer);
                        }
                    }
                    catch (Exception exLobby)
                    {
                        Logger.Error(exLobby, $"Error removing the player from the lobby {idPlayer}");
                    }

                    try
                    {
                        gameSessionManager.CheckAndClearActiveSessions(idPlayer);
                    }
                    catch (Exception exGame)
                    {
                        Logger.Error(exGame, $"Error removing the player from the gamer {idPlayer}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"General error clearing player session {idPlayer}");
            }

            Unsubscribe(idPlayer);

            await NotifyStatusChange(idPlayer, (int)PlayerStatus.Offline);

            Logger.Info($"Player {idPlayer} disconnected and successfully marked as Offline.");
        }

        public virtual bool IsPlayerOnline(int idPlayer)
        {
            if (playerStatuses.TryGetValue(idPlayer, out PlayerStatus status) &&
               (status == PlayerStatus.Online || status == PlayerStatus.InGame))
            {
                return true;
            }

            lock (lockObj)
            {
                return onlineSubscribers.ContainsKey(idPlayer);
            }
        }

        public void Subscribe(int idPlayer, IPresenceCallback callback)
        {
            lock (lockObj)
            {
                onlineSubscribers[idPlayer] = callback;
            }

            playerStatuses.AddOrUpdate(idPlayer, PlayerStatus.Online, (key, oldVal) => PlayerStatus.Online);
        }

        public void Unsubscribe(int idPlayer)
        {
            lock (lockObj)
            {
                onlineSubscribers.Remove(idPlayer);
            }

            playerStatuses.TryRemove(idPlayer, out _);
        }


        public virtual async Task NotifyStatusChange(int changedPlayerId, int newStatusId)
        {
            UpdatePlayerStatus(changedPlayerId, (PlayerStatus)newStatusId);

            var friends = await FetchFriendsSafeAsync(changedPlayerId);
            if (friends == null) return;

            var callbacks = GetActiveCallbacks(friends);

            foreach (var callback in callbacks)
            {
                NotifySingleFriendSafe(callback, changedPlayerId, newStatusId);
            }
        }

        private void UpdatePlayerStatus(int playerId, PlayerStatus status)
        {
            if (status == PlayerStatus.Offline)
            {
                playerStatuses.TryRemove(playerId, out _);
            }
            else
            {
                playerStatuses.AddOrUpdate(playerId, status, (key, oldVal) => status);
            }
        }

        private async Task<List<PlayerDto>> FetchFriendsSafeAsync(int playerId)
        {
            try
            {
                using (var scope = this.lifetimeScope.BeginLifetimeScope())
                {
                    var friendshipLogic = scope.Resolve<IFriendshipLogic>();
                    return await friendshipLogic.GetFriendsAsync(playerId);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "error fetching friends for presence notification");
                return null;
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
                var commObj = callback as System.ServiceModel.ICommunicationObject;
                if (commObj != null && commObj.State == System.ServiceModel.CommunicationState.Opened)
                {
                    callback.OnFriendStatusChanged(playerId, statusId);
                }
            }
            catch (System.ServiceModel.CommunicationException ex)
            {
                Logger.Warn(ex, "communication failure notifying player");
            }
            catch (TimeoutException ex)
            {
                Logger.Warn(ex, "timeout notifying player");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "unexpected error during status change notification");
            }
        }

        public void NotifyNewFriendRequest(int targetUserId)
        {
            IPresenceCallback callback = null;

            lock (lockObj)
            {
                onlineSubscribers.TryGetValue(targetUserId, out callback);
            }

            if (callback != null)
            {
                try
                {
                    callback.OnFriendRequestReceived();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Error notifying request to {targetUserId}");
                }
            }
        }

        public void NotifyFriendListUpdate(int targetUserId)
        {
            IPresenceCallback callback = null;

            lock (lockObj)
            {
                onlineSubscribers.TryGetValue(targetUserId, out callback);
            }

            if (callback != null)
            {
                try
                {
                    callback.OnFriendListUpdated();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Error notifying list update to {targetUserId}");
                }
            }
        }
        public bool IsPlayerInGame(int playerId)
        {
            if (playerStatuses.TryGetValue(playerId, out PlayerStatus status))
            {
                return status == PlayerStatus.InGame;
            }
            return false;
        }
    }
}
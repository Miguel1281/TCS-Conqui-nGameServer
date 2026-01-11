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
            Logger.Info($"Detectada desconexión del jugador {idPlayer}. Limpiando sesión...");;

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
                        Logger.Error($"Error sacando del lobby al jugador {idPlayer}: {exLobby.Message}");
                    }

                    try
                    {
                        gameSessionManager.CheckAndClearActiveSessions(idPlayer);
                    }
                    catch (Exception exGame)
                    {
                        Logger.Error($"Error sacando de la partida al jugador {idPlayer}: {exGame.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error general limpiando sesión del jugador {idPlayer}");
            }

            Unsubscribe(idPlayer);

            await NotifyStatusChange(idPlayer, (int)PlayerStatus.Offline);

            Logger.Info($"Jugador {idPlayer} desconectado y marcado como Offline exitosamente.");
        }

        public virtual bool IsPlayerOnline(int idPlayer)
        {
            if (playerStatuses.TryGetValue(idPlayer, out PlayerStatus status))
            {
                if (status == PlayerStatus.Online || status == PlayerStatus.InGame)
                {
                    return true;
                }
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
            var newStatus = (PlayerStatus)newStatusId;
            if (newStatus == PlayerStatus.Offline)
            {
                playerStatuses.TryRemove(changedPlayerId, out _);
            }
            else
            {
                playerStatuses.AddOrUpdate(changedPlayerId, newStatus, (key, oldVal) => newStatus);
            }

            List<PlayerDto> friends;
            try
            {
                using (var scope = this.lifetimeScope.BeginLifetimeScope())
                {
                    var friendshipLogic = scope.Resolve<IFriendshipLogic>();
                    friends = await friendshipLogic.GetFriendsAsync(changedPlayerId);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error fetching friends for presence notification: {ex.Message}");
                return;
            }

            var callbacksToNotify = new List<IPresenceCallback>();
            var friendIds = friends.Select(friend => friend.idPlayer);

            lock (lockObj)
            {
                foreach (var friendId in friendIds)
                {
                    if (onlineSubscribers.TryGetValue(friendId, out IPresenceCallback callback))
                    {
                        callbacksToNotify.Add(callback);
                    }
                }
            } 

            foreach (var callback in callbacksToNotify)
            {
                try
                {
                    var commObj = callback as System.ServiceModel.ICommunicationObject;
                    if (commObj != null && commObj.State == System.ServiceModel.CommunicationState.Opened)
                    {
                        callback.OnFriendStatusChanged(changedPlayerId, newStatusId);
                    }
                }
                catch (Exception)
                {
                }
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
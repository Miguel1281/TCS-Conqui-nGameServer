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
using System.Timers;

namespace ConquiánServidor.BusinessLogic
{
    public class PresenceManager : IPresenceManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<int, IPresenceCallback> onlineSubscribers = new Dictionary<int, IPresenceCallback>();
        private readonly ConcurrentDictionary<int, DateTime> lastHeartbeats = new ConcurrentDictionary<int, DateTime>();
        private readonly ConcurrentDictionary<int, PlayerStatus> playerStatuses = new ConcurrentDictionary<int, PlayerStatus>();

        private readonly object lockObj = new object();
        private readonly ILifetimeScope lifetimeScope;

        public PresenceManager(ILifetimeScope scope)
        {
            this.lifetimeScope = scope;
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
            List<PlayerDto> friends;
            var newStatus = (PlayerStatus)newStatusId;
            if (newStatus == PlayerStatus.Offline)
            {
                playerStatuses.TryRemove(changedPlayerId, out _);
            }
            else
            {
                playerStatuses.AddOrUpdate(changedPlayerId, newStatus, (key, oldVal) => newStatus);
            }

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

            var deadSubscribers = new List<int>();
            var friendIds = friends.Select(friend => friend.idPlayer);

            lock (lockObj)
            {
                foreach (var friendId in friendIds)
                {
                    if (onlineSubscribers.TryGetValue(friendId, out IPresenceCallback callback))
                    {
                        try
                        {
                            var commObj = callback as System.ServiceModel.ICommunicationObject;
                            if (commObj != null && commObj.State == System.ServiceModel.CommunicationState.Opened)
                            {
                                callback.OnFriendStatusChanged(changedPlayerId, newStatusId);
                            }
                            else
                            {
                                deadSubscribers.Add(friendId);
                            }
                        }
                        catch (Exception)
                        {
                            deadSubscribers.Add(friendId);
                        }
                    }
                }

                foreach (var deadId in deadSubscribers.Distinct())
                {
                    onlineSubscribers.Remove(deadId);
                }
            }
        }

        public void NotifyNewFriendRequest(int targetUserId)
        {
            lock (lockObj)
            {
                if (onlineSubscribers.TryGetValue(targetUserId, out IPresenceCallback callback))
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
        }

        public void NotifyFriendListUpdate(int targetUserId)
        {
            lock (lockObj)
            {
                if (onlineSubscribers.TryGetValue(targetUserId, out IPresenceCallback callback))
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
using Autofac;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.Contracts.DataContracts;
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

        private readonly object lockObj = new object();
        private readonly ILifetimeScope lifetimeScope;

        private readonly System.Timers.Timer heartbeatChecker;

        private const int HEARTBEAT_TIMEOUT_SECONDS = 15;
        private const int CHECK_INTERVAL_MS = 5000;

        public PresenceManager(ILifetimeScope scope)
        {
            this.lifetimeScope = scope;

            heartbeatChecker = new System.Timers.Timer(CHECK_INTERVAL_MS);
            heartbeatChecker.Elapsed += CheckInactiveUsers;
            heartbeatChecker.AutoReset = true;
            heartbeatChecker.Start();
        }

        public virtual bool IsPlayerOnline(int idPlayer)
        {
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

            lastHeartbeats.AddOrUpdate(idPlayer, DateTime.UtcNow, (key, oldVal) => DateTime.UtcNow);
        }

        public void Unsubscribe(int idPlayer)
        {
            lock (lockObj)
            {
                onlineSubscribers.Remove(idPlayer);
            }

            lastHeartbeats.TryRemove(idPlayer, out _);
        }

        public void ReceivePing(int idPlayer)
        {
            if (lastHeartbeats.ContainsKey(idPlayer))
            {
                lastHeartbeats[idPlayer] = DateTime.UtcNow;
            }
        }

        private void CheckInactiveUsers(object sender, ElapsedEventArgs e)
        {
            var now = DateTime.UtcNow;
            var timeoutLimit = TimeSpan.FromSeconds(HEARTBEAT_TIMEOUT_SECONDS);

            var inactivePlayers = lastHeartbeats.Where(kvp => (now - kvp.Value) > timeoutLimit).ToList();

            foreach (var entry in inactivePlayers)
            {
                int playerId = entry.Key;
                Logger.Warn($"Heartbeat timeout for Player {playerId}. Disconnecting forcingly.");

                Unsubscribe(playerId);

                Task.Run(async () =>
                {
                    int offlineStatusId = 2; 
                    await NotifyStatusChange(playerId, offlineStatusId);
                });
            }
        }

        public virtual async Task NotifyStatusChange(int changedPlayerId, int newStatusId)
        {
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
                    lastHeartbeats.TryRemove(deadId, out _);
                }
            }
        }
    } 
} 
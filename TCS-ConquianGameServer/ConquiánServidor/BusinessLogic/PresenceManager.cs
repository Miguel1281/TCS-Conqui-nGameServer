using Autofac;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic
{
    public class PresenceManager:IPresenceManager
    {
        private readonly Dictionary<int, IPresenceCallback> onlineSubscribers = new Dictionary<int, IPresenceCallback>();
        private readonly object lockObj = new object();
        private readonly ILifetimeScope lifetimeScope;

        public PresenceManager(ILifetimeScope scope)
        {
            this.lifetimeScope = scope;
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
        }

        public void Unsubscribe(int idPlayer)
        {
            lock (lockObj)
            {
                onlineSubscribers.Remove(idPlayer);
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
                Console.WriteLine($"Error al obtener amigos para notificación de presencia: {ex.Message}");
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
                            callback.OnFriendStatusChanged(changedPlayerId, newStatusId);
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
    }
}
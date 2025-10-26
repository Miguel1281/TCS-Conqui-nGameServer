using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.DataAccess.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic
{
    public class PresenceManager
    {
        private static readonly PresenceManager instance = new PresenceManager();
        private readonly Dictionary<int, IPresenceCallback> onlineSubscribers = new Dictionary<int, IPresenceCallback>();
        private readonly object lockObj = new object();
        private readonly FriendshipLogic friendshipLogic;

        public static PresenceManager Instance => instance;

        private PresenceManager()
        {
            var dbContext = new ConquiánDBEntities();
            IFriendshipRepository friendshipRepository = new FriendshipRepository(dbContext);
            IPlayerRepository playerRepository = new PlayerRepository(dbContext);
            friendshipLogic = new FriendshipLogic(friendshipRepository, playerRepository);
        }

        public bool IsPlayerOnline(int idPlayer)
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

        public async Task NotifyStatusChange(int changedPlayerId, int newStatusId)
        {
            List<PlayerDto> friends;
            try
            {
                friends = await friendshipLogic.GetFriendsAsync(changedPlayerId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener amigos para notificación de presencia: {ex.Message}");
                return;
            }

            var deadSubscribers = new List<int>();

            lock (lockObj)
            {
                foreach (var friend in friends)
                {
                    if (onlineSubscribers.TryGetValue(friend.idPlayer, out IPresenceCallback callback))
                    {
                        try
                        {
                            callback.OnFriendStatusChanged(changedPlayerId, newStatusId);
                        }
                        catch (Exception)
                        {
                            deadSubscribers.Add(friend.idPlayer);
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
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.DataAccess.Abstractions;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace ConquiánServidor.DataAccess.Repositories
{
    public class FriendshipRepository : IFriendshipRepository
    {
        private readonly ConquiánDBEntities context;

        public FriendshipRepository(ConquiánDBEntities context)
        {
            this.context = context;
        }

        public async Task<List<Player>> GetFriendsAsync(int idPlayer)
        {
            return await context.Friendship
                .Where(f => (f.idOrigen == idPlayer || f.idDestino == idPlayer) && f.idStatus == 1)
                .Select(f => f.idOrigen == idPlayer ? f.Player : f.Player1) 
                .ToListAsync();
        }

        public async Task<Friendship> GetPendingRequestByIdAsync(int idFriendship)
        {
            return await context.Friendship
                .FirstOrDefaultAsync(f => f.idFriendship == idFriendship && f.idStatus == 3); 
        }
        public async Task<List<Friendship>> GetFriendRequestsAsync(int idPlayer)
        {
            return await context.Friendship
                .Where(f => f.idDestino == idPlayer && f.idStatus == 3)
                .Include(f => f.Player1) 
                .ToListAsync();
        }
        public async Task<Friendship> GetExistingRelationshipAsync(int idPlayer, int idFriend)
        {
            return await context.Friendship
                .FirstOrDefaultAsync(f => (f.idOrigen == idPlayer && f.idDestino == idFriend) || (f.idOrigen == idFriend && f.idDestino == idPlayer));
        }

        public async Task<Friendship> GetPendingRequestAsync(int senderId, int receiverId)
        {
            return await context.Friendship
                .FirstOrDefaultAsync(f => f.idOrigen == senderId && f.idDestino == receiverId && f.idStatus == 3);
        }

        public async Task<Friendship> GetAcceptedFriendshipAsync(int idPlayer, int idFriend)
        {
            return await context.Friendship
                .FirstOrDefaultAsync(f => ((f.idOrigen == idPlayer && f.idDestino == idFriend) || (f.idOrigen == idFriend && f.idDestino == idPlayer)) && f.idStatus == 1);
        }

        public void AddFriendship(Friendship friendship)
        {
            context.Friendship.Add(friendship);
        }

        public void RemoveFriendship(Friendship friendship)
        {
            context.Friendship.Remove(friendship);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await context.SaveChangesAsync();
        }
    }
}
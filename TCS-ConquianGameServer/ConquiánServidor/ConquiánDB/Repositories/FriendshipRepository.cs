using ConquiánServidor.ConquiánDB.Abstractions;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace ConquiánServidor.ConquiánDB.Repositories
{
    public class FriendshipRepository : IFriendshipRepository
    {
        private const int FRIENDSHIP_STATUS_ACCEPTED = 1;
        private const int FRIENDSHIP_STATUS_PENDING = 3;

        private readonly ConquiánDBEntities context;

        public FriendshipRepository(ConquiánDBEntities context)
        {
            this.context = context;
        }

        public async Task<List<Player>> GetFriendsAsync(int idPlayer)
        {
            var friends = await context.Friendship
                .Where(f => (f.idOrigen == idPlayer || f.idDestino == idPlayer)
                         && f.idStatus == FRIENDSHIP_STATUS_ACCEPTED)
                .Select(f => f.idOrigen == idPlayer ? f.Player : f.Player1)
                .ToListAsync();

            return friends;
        }

        public async Task<Friendship> GetPendingRequestByIdAsync(int idFriendship)
        {
            var pendingRequest = await context.Friendship
                .FirstOrDefaultAsync(f => f.idFriendship == idFriendship
                                       && f.idStatus == FRIENDSHIP_STATUS_PENDING);

            return pendingRequest;
        }

        public async Task<List<Friendship>> GetFriendRequestsAsync(int idPlayer)
        {
            var friendRequests = await context.Friendship
                .Where(f => f.idDestino == idPlayer
                         && f.idStatus == FRIENDSHIP_STATUS_PENDING)
                .Include(f => f.Player1)
                .ToListAsync();

            return friendRequests;
        }

        public async Task<Friendship> GetExistingRelationshipAsync(int idPlayer, int idFriend)
        {
            var existingRelationship = await context.Friendship
                .FirstOrDefaultAsync(f => (f.idOrigen == idPlayer && f.idDestino == idFriend)
                                       || (f.idOrigen == idFriend && f.idDestino == idPlayer));

            return existingRelationship;
        }

        public async Task<Friendship> GetPendingRequestAsync(int receiverId, int senderId)
        {
            var pendingRequest = await context.Friendship
                .FirstOrDefaultAsync(f => f.idOrigen == receiverId
                                       && f.idDestino == senderId
                                       && f.idStatus == FRIENDSHIP_STATUS_PENDING);

            return pendingRequest;
        }

        public async Task<Friendship> GetAcceptedFriendshipAsync(int idPlayer, int idFriend)
        {
            var acceptedFriendship = await context.Friendship
                .FirstOrDefaultAsync(f => ((f.idOrigen == idPlayer && f.idDestino == idFriend)
                                        || (f.idOrigen == idFriend && f.idDestino == idPlayer))
                                        && f.idStatus == FRIENDSHIP_STATUS_ACCEPTED);

            return acceptedFriendship;
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
            int changesSaved = await context.SaveChangesAsync();
            return changesSaved;
        }
    }
}
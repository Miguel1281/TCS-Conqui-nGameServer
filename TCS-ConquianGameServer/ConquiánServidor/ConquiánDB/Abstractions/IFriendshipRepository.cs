using ConquiánServidor.ConquiánDB;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConquiánServidor.DataAccess.Abstractions
{
    public interface IFriendshipRepository
    {
        Task<List<Player>> GetFriendsAsync(int idPlayer);
        Task<List<Friendship>> GetFriendRequestsAsync(int idPlayer);
        Task<Friendship> GetExistingRelationshipAsync(int idPlayer, int idFriend);
        Task<Friendship> GetPendingRequestAsync(int senderId, int receiverId);
        Task<Friendship> GetAcceptedFriendshipAsync(int idPlayer, int idFriend);
        Task<Friendship> GetPendingRequestByIdAsync(int idFriendship); 
        void AddFriendship(Friendship friendship);
        void RemoveFriendship(Friendship friendship);
        Task<int> SaveChangesAsync();
    }
}
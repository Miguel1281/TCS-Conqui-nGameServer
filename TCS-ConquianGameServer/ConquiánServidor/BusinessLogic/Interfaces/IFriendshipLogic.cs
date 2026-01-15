using ConquiánServidor.Contracts.DataContracts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic.Interfaces
{
    public interface IFriendshipLogic
    {
        Task DeleteFriendAsync(int idPlayer, int idFriend);
        Task<List<FriendRequestDto>> GetFriendRequestsAsync(int idPlayer);
        Task<List<PlayerDto>> GetFriendsAsync(int idPlayer);
        Task<PlayerDto> GetPlayerByNicknameAsync(string nickname, int idPlayer);
        Task SendFriendRequestAsync(int idPlayer, int idFriend);
        Task UpdateFriendRequestStatusAsync(int idFriendship, int newStatus);
    }
}

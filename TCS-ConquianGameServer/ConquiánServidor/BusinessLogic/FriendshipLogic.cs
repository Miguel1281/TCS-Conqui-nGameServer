using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.DataAccess.Abstractions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic
{
    public class FriendshipLogic
    {
        private readonly IFriendshipRepository friendshipRepository;
        private readonly IPlayerRepository playerRepository;

        public FriendshipLogic(IFriendshipRepository friendshipRepository, IPlayerRepository playerRepository)
        {
            this.friendshipRepository = friendshipRepository;
            this.playerRepository = playerRepository;
        }

        public async Task<List<PlayerDto>> GetFriendsAsync(int idPlayer)
        {
            var friends = await friendshipRepository.GetFriendsAsync(idPlayer);

            return friends.Select(p => new PlayerDto
            {
                idPlayer = p.idPlayer,
                nickname = p.nickname,
                pathPhoto = p.pathPhoto,
                idStatus = (int)p.IdStatus,
                level = p.level
            }).ToList();
        }

        public async Task<List<FriendRequestDto>> GetFriendRequestsAsync(int idPlayer)
        {
            var requests = await friendshipRepository.GetFriendRequestsAsync(idPlayer);

            return requests.Select(f => new FriendRequestDto
            {
                IdFriendship = f.idFriendship,
                Nickname = f.Player1.nickname
            }).ToList();
        }

        public async Task<PlayerDto> GetPlayerByNicknameAsync(string nickname, int idPlayer)
        {
            PlayerDto playerDto = new PlayerDto(); 
            var player = await playerRepository.GetPlayerByNicknameAsync(nickname);

            if (player != null && player.idPlayer != idPlayer)
            {
                playerDto = new PlayerDto
                {
                    idPlayer = player.idPlayer,
                    nickname = player.nickname,
                    pathPhoto = player.pathPhoto
                };
            }
            return playerDto; 
        }

        public async Task<bool> SendFriendRequestAsync(int idPlayer, int idFriend)
        {
            bool success = false; 
            var existingFriendship = await friendshipRepository.GetExistingRelationshipAsync(idPlayer, idFriend);

            if (existingFriendship == null)
            {
                var newRequest = new Friendship
                {
                    idOrigen = idPlayer,
                    idDestino = idFriend,
                    idStatus = 3
                };
                friendshipRepository.AddFriendship(newRequest);
                await friendshipRepository.SaveChangesAsync();
                success = true;
            }
            return success; 
        }

        public async Task<bool> UpdateFriendRequestStatusAsync(int idFriendship, int newStatus)
        {
            bool success = false; 
            var request = await friendshipRepository.GetPendingRequestByIdAsync(idFriendship);

            if (request != null)
            {
                if (newStatus == 1)
                {
                    request.idStatus = 1;
                }
                else
                {
                    friendshipRepository.RemoveFriendship(request);
                }
                await friendshipRepository.SaveChangesAsync();
                success = true;
            }
            return success; 
        }

        public async Task<bool> DeleteFriendAsync(int idPlayer, int idFriend)
        {
            bool success = false; 
            var friendship = await friendshipRepository.GetAcceptedFriendshipAsync(idPlayer, idFriend);

            if (friendship != null)
            {
                friendshipRepository.RemoveFriendship(friendship);
                await friendshipRepository.SaveChangesAsync();
                success = true;
            }
            return success; 
        }
    }
}
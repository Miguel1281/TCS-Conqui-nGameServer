using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.DataAccess.Abstractions;
using System;
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
            var friendDtos = new List<PlayerDto>();

            foreach (var p in friends)
            {
                bool isOnline = PresenceManager.Instance.IsPlayerOnline(p.idPlayer);

                friendDtos.Add(new PlayerDto
                {
                    idPlayer = p.idPlayer,
                    nickname = p.nickname,
                    pathPhoto = p.pathPhoto,
                    idStatus = isOnline ? 1 : 2, 
                    level = p.level
                });
            }

            return friendDtos;
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
            var player = await playerRepository.GetPlayerByNicknameAsync(nickname);

            if (player == null || player.idPlayer == idPlayer)
            {
                throw new KeyNotFoundException();
            }

            return new PlayerDto
            {
                idPlayer = player.idPlayer,
                nickname = player.nickname,
                pathPhoto = player.pathPhoto
            };
        }

        public async Task SendFriendRequestAsync(int idPlayer, int idFriend)
        {
            var existingFriendship = await friendshipRepository.GetExistingRelationshipAsync(idPlayer, idFriend);

            if (existingFriendship != null)
            {
                throw new InvalidOperationException("Ya existe una solicitud pendiente o una amistad con este jugador.");
            }

            var newRequest = new Friendship
            {
                idOrigen = idPlayer,
                idDestino = idFriend,
                idStatus = 3
            };

            friendshipRepository.AddFriendship(newRequest);
            await friendshipRepository.SaveChangesAsync();
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

        public async Task DeleteFriendAsync(int idPlayer, int idFriend)
        {
            var friendship = await friendshipRepository.GetAcceptedFriendshipAsync(idPlayer, idFriend);

            if (friendship == null)
            {
                throw new KeyNotFoundException("No se encontró la relación de amistad para eliminar.");
            }

            friendshipRepository.RemoveFriendship(friendship);
            await friendshipRepository.SaveChangesAsync();
        }
    }
}
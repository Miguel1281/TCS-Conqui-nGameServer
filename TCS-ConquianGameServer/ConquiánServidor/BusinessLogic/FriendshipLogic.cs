using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.DataAccess.Abstractions;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic
{
    public class FriendshipLogic
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IFriendshipRepository friendshipRepository;
        private readonly IPlayerRepository playerRepository;

        public FriendshipLogic(IFriendshipRepository friendshipRepository, IPlayerRepository playerRepository)
        {
            this.friendshipRepository = friendshipRepository;
            this.playerRepository = playerRepository;
        }

        public async Task<List<PlayerDto>> GetFriendsAsync(int idPlayer)
        {
            Logger.Info($"Fetching friends list for Player ID: {idPlayer}");

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

            Logger.Info($"Friends list retrieved for Player ID: {idPlayer}. Count: {friendDtos.Count}");
            return friendDtos;
        }

        public async Task<List<FriendRequestDto>> GetFriendRequestsAsync(int idPlayer)
        {
            Logger.Info($"Fetching friend requests for Player ID: {idPlayer}");

            var requests = await friendshipRepository.GetFriendRequestsAsync(idPlayer);

            Logger.Info($"Friend requests retrieved for Player ID: {idPlayer}. Count: {requests.Count}");

            return requests.Select(f => new FriendRequestDto
            {
                IdFriendship = f.idFriendship,
                Nickname = f.Player1.nickname
            }).ToList();
        }

        public async Task<PlayerDto> GetPlayerByNicknameAsync(string nickname, int idPlayer)
        {
            Logger.Info($"Search player by nickname initiated by Requesting Player ID: {idPlayer}");

            var player = await playerRepository.GetPlayerByNicknameAsync(nickname);

            if (player == null || player.idPlayer == idPlayer)
            {
                Logger.Warn($"Player search failed: Nickname not found or matches requester ID: {idPlayer}");
                throw new KeyNotFoundException();
            }

            Logger.Info($"Player search successful. Found Player ID: {player.idPlayer}");

            return new PlayerDto
            {
                idPlayer = player.idPlayer,
                nickname = player.nickname,
                pathPhoto = player.pathPhoto
            };
        }

        public async Task SendFriendRequestAsync(int idPlayer, int idFriend)
        {
            Logger.Info($"Friend request attempt: Player ID {idPlayer} -> Target ID {idFriend}");

            var existingFriendship = await friendshipRepository.GetExistingRelationshipAsync(idPlayer, idFriend);

            if (existingFriendship != null)
            {
                Logger.Warn($"Friend request failed: Relationship already exists between Player ID {idPlayer} and Target ID {idFriend}");
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

            Logger.Info($"Friend request sent successfully: Player ID {idPlayer} -> Target ID {idFriend}");
        }

        public async Task<bool> UpdateFriendRequestStatusAsync(int idFriendship, int newStatus)
        {
            Logger.Info($"Updating friend request status. Friendship ID: {idFriendship}, New Status: {newStatus}");

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
                Logger.Info($"Friend request status updated successfully for Friendship ID: {idFriendship}");
            }
            else
            {
                Logger.Warn($"Update friend request failed: Friendship ID {idFriendship} not found.");
            }

            return success;
        }

        public async Task DeleteFriendAsync(int idPlayer, int idFriend)
        {
            Logger.Info($"Friend deletion attempt: Player ID {idPlayer} removing Friend ID {idFriend}");

            var friendship = await friendshipRepository.GetAcceptedFriendshipAsync(idPlayer, idFriend);

            if (friendship == null)
            {
                Logger.Warn($"Friend deletion failed: Friendship not found between Player ID {idPlayer} and Friend ID {idFriend}");
                throw new KeyNotFoundException("No se encontró la relación de amistad para eliminar.");
            }

            friendshipRepository.RemoveFriendship(friendship);
            await friendshipRepository.SaveChangesAsync();

            Logger.Info($"Friend deleted successfully: Player ID {idPlayer} and Friend ID {idFriend}");
        }
    }
}
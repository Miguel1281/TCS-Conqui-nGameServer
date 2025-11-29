using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.Enums;
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
        private readonly PresenceManager presenceManager;

        public FriendshipLogic(IFriendshipRepository friendshipRepository, IPlayerRepository playerRepository, PresenceManager presenceManager)
        {
            this.friendshipRepository = friendshipRepository;
            this.playerRepository = playerRepository;
            this.presenceManager = presenceManager;
        }

        public async Task<List<PlayerDto>> GetFriendsAsync(int idPlayer)
        {
            Logger.Info($"Fetching friends list for Player ID: {idPlayer}");

            var friends = await friendshipRepository.GetFriendsAsync(idPlayer);
            var friendDtos = new List<PlayerDto>();

            foreach (var p in friends)
            {
                bool isOnline = this.presenceManager.IsPlayerOnline(p.idPlayer);

                friendDtos.Add(new PlayerDto
                {
                    idPlayer = p.idPlayer,
                    nickname = p.nickname,
                    pathPhoto = p.pathPhoto,
                    idStatus = isOnline ? (int)PlayerStatus.Online : (int)PlayerStatus.Offline,
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
                throw new BusinessLogicException(ServiceErrorType.UserNotFound);
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
                throw new BusinessLogicException(ServiceErrorType.DuplicateRecord);
            }

            var newRequest = new Friendship
            {
                idOrigen = idPlayer,
                idDestino = idFriend,
                idStatus = (int)FriendshipStatus.Pending
            };

            friendshipRepository.AddFriendship(newRequest);
            await friendshipRepository.SaveChangesAsync();

            Logger.Info($"Friend request sent successfully: Player ID {idPlayer} -> Target ID {idFriend}");
        }

        public async Task UpdateFriendRequestStatusAsync(int idFriendship, int newStatus)
        {
            Logger.Info($"Updating friend request status. Friendship ID: {idFriendship}, New Status: {newStatus}");

            var request = await friendshipRepository.GetPendingRequestByIdAsync(idFriendship);

            if (request == null)
            {
                Logger.Warn($"Update friend request failed: Friendship ID {idFriendship} not found.");
                throw new BusinessLogicException(ServiceErrorType.NotFound);
            }

            if (newStatus == (int)FriendshipStatus.Accepted)
            {
                request.idStatus = (int)FriendshipStatus.Accepted;
            }
            else
            {
                friendshipRepository.RemoveFriendship(request);
            }

            await friendshipRepository.SaveChangesAsync();
            Logger.Info($"Friend request status updated successfully for Friendship ID: {idFriendship}");
        }

        public async Task DeleteFriendAsync(int idPlayer, int idFriend)
        {
            Logger.Info($"Friend deletion attempt: Player ID {idPlayer} removing Friend ID {idFriend}");

            var friendship = await friendshipRepository.GetAcceptedFriendshipAsync(idPlayer, idFriend);

            if (friendship == null)
            {
                Logger.Warn($"Friend deletion failed: Friendship not found between Player ID {idPlayer} and Friend ID {idFriend}");
                throw new BusinessLogicException(ServiceErrorType.NotFound);
            }

            friendshipRepository.RemoveFriendship(friendship);
            await friendshipRepository.SaveChangesAsync();

            Logger.Info($"Friend deleted successfully: Player ID {idPlayer} and Friend ID {idFriend}");
        }
    }
}
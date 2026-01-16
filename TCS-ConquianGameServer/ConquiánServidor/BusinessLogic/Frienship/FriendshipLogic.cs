using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.Enums;
using System.Data.Entity.Infrastructure;
using ConquiánServidor.ConquiánDB.Abstractions;
using NLog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic.Frienship
{
    public class FriendshipLogic : IFriendshipLogic
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IFriendshipRepository friendshipRepository;
        private readonly IPlayerRepository playerRepository;
        private readonly IPresenceManager presenceManager;

        public FriendshipLogic(IFriendshipRepository friendshipRepository, IPlayerRepository playerRepository, IPresenceManager presenceManager)
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
                PlayerStatus status = PlayerStatus.Offline;

                if (this.presenceManager.IsPlayerInGame(p.idPlayer))
                {
                    status = PlayerStatus.InGame;
                }
                else if (this.presenceManager.IsPlayerInLobby(p.idPlayer))
                {
                    status = PlayerStatus.InLobby;
                }
                else if (this.presenceManager.IsPlayerOnline(p.idPlayer))
                {
                    status = PlayerStatus.Online;
                }

                friendDtos.Add(new PlayerDto
                {
                    idPlayer = p.idPlayer,
                    nickname = p.nickname,
                    pathPhoto = p.pathPhoto,
                    Status = status,
                    idLevel = p.idLevel
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
            var player = await playerRepository.GetPlayerByNicknameAsync(nickname);

            if (player == null || player.idPlayer == idPlayer)
            {
                Logger.Warn($"Player search failed: Nickname not found or matches requester ID: {idPlayer}");
                throw new BusinessLogicException(ServiceErrorType.UserNotFound);
            }

            PlayerStatus status = PlayerStatus.Offline;

            if (this.presenceManager.IsPlayerInGame(player.idPlayer))
            {
                status = PlayerStatus.InGame;
            }
            else if (this.presenceManager.IsPlayerInLobby(player.idPlayer))
            {
                status = PlayerStatus.InLobby;
            }
            else if (this.presenceManager.IsPlayerOnline(player.idPlayer))
            {
                status = PlayerStatus.Online;
            }

            Logger.Info($"Player search successful. Found Player ID: {player.idPlayer}");

            string rankName = player.LevelRules?.RankName;

            return new PlayerDto
            {
                idPlayer = player.idPlayer,
                nickname = player.nickname,
                pathPhoto = player.pathPhoto,
                idLevel = player.idLevel,
                RankName = rankName,
                Status = status 
            };
        }

        public async Task SendFriendRequestAsync(int idPlayer, int idFriend)
        {
            Logger.Info($"Friend request attempt: Player ID {idPlayer} -> Target ID {idFriend}");

            var existingFriendship = await friendshipRepository.GetExistingRelationshipAsync(idPlayer, idFriend);

            if (existingFriendship != null)
            {
                if (existingFriendship.idStatus == (int)FriendshipStatus.Pending && existingFriendship.idOrigen == idFriend)
                {
                    await UpdateFriendRequestStatusAsync(existingFriendship.idFriendship, (int)FriendshipStatus.Accepted);
                    return; 
                }
                Logger.Warn($"Friend request failed: Relationship already exists between Player ID {idPlayer} and Target ID {idFriend}");
                throw new BusinessLogicException(ServiceErrorType.ExistingRequest);
            }

            var newRequest = new Friendship
            {
                idOrigen = idPlayer,
                idDestino = idFriend,
                idStatus = (int)FriendshipStatus.Pending
            };

            try
            {
                friendshipRepository.AddFriendship(newRequest);
                await friendshipRepository.SaveChangesAsync();

                presenceManager.NotifyNewFriendRequest(idFriend);
                Logger.Info($"Friend request sent successfully: Player ID {idPlayer} -> Target ID {idFriend}");
            }
            catch (DbUpdateException ex)
            {
                Logger.Warn(ex, "concurrency error handling friend request");
                throw new BusinessLogicException(ServiceErrorType.ExistingRequest);
            }
        }

        public async Task UpdateFriendRequestStatusAsync(int idFriendship, int newStatus)
        {
            var request = await friendshipRepository.GetPendingRequestByIdAsync(idFriendship);

            if (request == null)
            {
                Logger.Warn($"Update friend request failed: Friendship ID {idFriendship} not found.");
                return;
            }

            int senderId = request.idOrigen ?? 0;
            int receiverId = request.idDestino ?? 0;

            if (newStatus == (int)FriendshipStatus.Accepted)
            {
                var existingAccepted = await friendshipRepository.GetAcceptedFriendshipAsync(senderId, receiverId);

                if (existingAccepted != null)
                {
                    friendshipRepository.RemoveFriendship(request);
                }
                else
                {
                    request.idStatus = (int)FriendshipStatus.Accepted;

                    var mutualRequest = await friendshipRepository.GetPendingRequestAsync(receiverId, senderId);
                    if (mutualRequest != null)
                    {
                        Logger.Info($"Found mutual pending request ID {mutualRequest.idFriendship}. Removing it to prevent duplicates.");
                        friendshipRepository.RemoveFriendship(mutualRequest);
                    }
                }
            }
            else
            {
                friendshipRepository.RemoveFriendship(request);
            }

            try
            {
                await friendshipRepository.SaveChangesAsync();

                presenceManager.NotifyFriendListUpdate(senderId);
                presenceManager.NotifyFriendListUpdate(receiverId);

                Logger.Info($"Friend request status updated successfully for Friendship ID: {idFriendship}");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                Logger.Warn(ex, "Concurrency exception handled during UpdateFriendRequestStatus.");
            }
        }

        public async Task DeleteFriendAsync(int idPlayer, int idFriend)
        {
            var friendship = await friendshipRepository.GetAcceptedFriendshipAsync(idPlayer, idFriend);

            if (friendship == null)
            {
                Logger.Info($"Friendship between {idPlayer} and {idFriend} already deleted or not found. Treating as success.");
                return;
            }

            try
            {
                friendshipRepository.RemoveFriendship(friendship);
                await friendshipRepository.SaveChangesAsync();

                presenceManager.NotifyFriendListUpdate(idPlayer);
                presenceManager.NotifyFriendListUpdate(idFriend);

                Logger.Info($"Friend deleted successfully: Player ID {idPlayer} and Friend ID {idFriend}");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                Logger.Warn(ex, "Concurrency exception ignored during friend deletion - record already deleted.");
            }
        }
    }
}
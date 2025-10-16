using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Services
{
    public class FriendList : IFriendList
    {
        public async Task<PlayerDto> GetPlayerByNicknameAsync(string nickname, int idCurrentUser)
        {
            try
            {
                using (var context = new ConquiánDBEntities())
                {
                    var dbPlayer = await context.Player.FirstOrDefaultAsync(p => p.nickname == nickname);

                    if (dbPlayer != null && dbPlayer.idPlayer != idCurrentUser)
                    {
                        var areFriends = await context.Friendship.AnyAsync(f =>
                            ((f.idOrigen == idCurrentUser && f.idDestino == dbPlayer.idPlayer) ||
                             (f.idOrigen == dbPlayer.idPlayer && f.idDestino == idCurrentUser))
                            && f.idStatus == 1); 

                        if (!areFriends)
                        {
                            return new PlayerDto
                            {
                                idPlayer = dbPlayer.idPlayer,
                                nickname = dbPlayer.nickname,
                                level = dbPlayer.level
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new FaultException("Error al recuperar la información del jugador.");
            }
            return null;
        }

        public async Task<List<PlayerDto>> GetFriendsAsync(int idPlayer)
        {
            try
            {
                using (var context = new ConquiánDBEntities())
                {
                    var friendIds = await context.Friendship
                        .Where(f => (f.idOrigen == idPlayer || f.idDestino == idPlayer) && f.idStatus == 1)
                        .Select(f => f.idOrigen == idPlayer ? f.idDestino : f.idOrigen)
                        .ToListAsync();

                    var friends = await context.Player
                        .Where(p => friendIds.Contains(p.idPlayer))
                        .Select(p => new PlayerDto
                        {
                            idPlayer = p.idPlayer,
                            nickname = p.nickname,
                            level = p.level
                        }).ToListAsync();

                    return friends;
                }
            }
            catch (Exception ex)
            {
                throw new FaultException("Error al recuperar la lista de amigos.");
            }
        }

        public async Task<bool> SendFriendRequestAsync(int idSender, int idReceiver)
        {
            try
            {
                using (var context = new ConquiánDBEntities())
                {
                    var existingRequest = await context.Friendship.FirstOrDefaultAsync(f =>
                        (f.idOrigen == idSender && f.idDestino == idReceiver) ||
                        (f.idOrigen == idReceiver && f.idDestino == idSender));

                    if (existingRequest != null)
                    {
                        return false; 
                    }

                    var newFriendship = new Friendship
                    {
                        idOrigen = idSender,
                        idDestino = idReceiver,
                        idStatus = 3 
                    };

                    context.Friendship.Add(newFriendship);
                    await context.SaveChangesAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new FaultException("Error al enviar la solicitud de amistad.");
            }
        }
    }
}
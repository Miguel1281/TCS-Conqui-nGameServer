using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConquiánServidor.Services
{
    public class Login : ILogin
    {
         public async Task<PlayerDto> AuthenticatePlayerAsync(string playerEmail, string playerPassword)
         {
            using (var context = new ConquiánDBEntities())
            {
                var playerFromDb = await context.Player.FirstOrDefaultAsync(p => p.email == playerEmail);

                if (playerFromDb != null && PasswordHasher.verifyPassword(playerPassword, playerFromDb.password))
                {
                    return new PlayerDto
                    {
                        idPlayer = playerFromDb.idPlayer,
                        nickname = playerFromDb.nickname,
                        pathPhoto = playerFromDb.pathPhoto,
                    };
                }
            } 
            return null;
         }
    }
}

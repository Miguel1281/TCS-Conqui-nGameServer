using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts;
using ConquiánServidor.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConquiánServidor.Services
{
    public class Login : ILogin
    {
        public bool SignIn(string playerEmail, string playerPassword)
        {
            try
            {
                using (var context = new ConquiánDBEntities())
                {
                    var player = context.Player.FirstOrDefault(p => p.email == playerEmail);

                    if (player != null)
                    {
                        return PasswordHasher.verifyPassword(playerPassword, player.password);
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al iniciar sesión: " + ex.Message);
                return false;
            }
        }
    }
}

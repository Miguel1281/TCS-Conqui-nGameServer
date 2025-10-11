using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts;
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
                    var player = context.Player.FirstOrDefault(p => p.email == playerEmail && p.password == playerPassword);
                    return player != null;
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

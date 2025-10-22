using ConquiánServidor.BusinessLogic;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.DataAccess.Repositories;
using ConquiánServidor.ConquiánDB;
using System.Threading.Tasks;

namespace ConquiánServidor.Services
{
    public class Login : ILogin
    {
        private readonly AuthenticationLogic authLogic;

        public Login()
        {
            var dbContext = new ConquiánDBEntities();
            IPlayerRepository playerRepository = new PlayerRepository(dbContext);
            authLogic = new AuthenticationLogic(playerRepository);
        }

        public async Task<PlayerDto> AuthenticatePlayerAsync(string playerEmail, string playerPassword)
        {
            return await authLogic.AuthenticatePlayerAsync(playerEmail, playerPassword);
        }

        public async Task SignOutPlayerAsync(int idPlayer)
        {
            await authLogic.SignOutPlayerAsync(idPlayer);
        }
    }
}
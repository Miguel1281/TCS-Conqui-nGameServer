using ConquiánServidor.BusinessLogic;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.DataAccess.Repositories;
using ConquiánServidor.ConquiánDB;
using System.Threading.Tasks;

namespace ConquiánServidor.Services
{
    public class SignUp : ISignUp
    {
        private readonly AuthenticationLogic authLogic;

        public SignUp()
        {
            var dbContext = new ConquiánDBEntities();
            IPlayerRepository playerRepository = new PlayerRepository(dbContext);
            authLogic = new AuthenticationLogic(playerRepository);
        }

        public async Task<bool> RegisterPlayerAsync(PlayerDto finalPlayerData)
        {
            return await authLogic.RegisterPlayerAsync(finalPlayerData);
        }

        public async Task<string> SendVerificationCodeAsync(string email)
        {
            return await authLogic.SendVerificationCodeAsync(email);
        }

        public async Task<bool> VerifyCodeAsync(string email, string code)
        {
            return await authLogic.VerifyCodeAsync(email, code);
        }
    }
}
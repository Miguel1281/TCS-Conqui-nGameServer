using ConquiánServidor.BusinessLogic;
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.DataAccess.Repositories;
using ConquiánServidor.Utilities.Email;
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
            IEmailService emailService = new EmailService();
            authLogic = new AuthenticationLogic(playerRepository, emailService);
        }

        public async Task<bool> RegisterPlayerAsync(PlayerDto newPlayer)
        {
            return await authLogic.RegisterPlayerAsync(newPlayer);
        }

        public async Task<string> SendVerificationCodeAsync(string email)
        {
            return await authLogic.SendVerificationCodeAsync(email);
        }

        public async Task<bool> VerifyCodeAsync(string email, string code)
        {
            return await authLogic.VerifyCodeAsync(email, code);
        }

        public async Task<bool> CancelRegistrationAsync(string email)
        {
            return await authLogic.DeleteTemporaryPlayerAsync(email);
        }
    }
}
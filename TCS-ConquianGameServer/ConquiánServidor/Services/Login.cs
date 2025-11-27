using ConquiánServidor.BusinessLogic;
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.DataAccess.Repositories;
using ConquiánServidor.Utilities.Email;
using ConquiánServidor.Utilities.Messages;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Services
{
    public class Login : ILogin
    {
        private readonly AuthenticationLogic authLogic;
        private readonly IPlayerRepository playerRepository;
        private readonly IMessageResolver messageResolver;

        public Login()
        {
            var dbContext = new ConquiánDBEntities();
            this.playerRepository = new PlayerRepository(dbContext);
            IEmailService emailService = new EmailService();
            this.messageResolver = new ResourceMessageResolver();
            this.authLogic = new AuthenticationLogic(this.playerRepository, emailService, this.messageResolver);
        }

        public async Task<PlayerDto> AuthenticatePlayerAsync(string email, string password)
        {
            var player = await playerRepository.GetPlayerByEmailAsync(email);
            if (player != null && PresenceManager.Instance.IsPlayerOnline(player.idPlayer))
            {
                string errorMsg = messageResolver.GetMessage(ServiceErrorType.SessionActive);

                var faultData = new ServiceFaultDto(
                    ServiceErrorType.SessionActive,
                    errorMsg
                 );

                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(errorMsg));
            }
            return await authLogic.AuthenticatePlayerAsync(email, password);
        }

        public async Task SignOutPlayerAsync(int idPlayer)
        {
            await authLogic.SignOutPlayerAsync(idPlayer);
        }
    }
}
using ConquiánServidor.BusinessLogic;
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.FaultContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.DataAccess.Repositories;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Services
{
    public class Login : ILogin
    {
        private readonly AuthenticationLogic authLogic;
        private readonly IPlayerRepository playerRepository;

        public Login()
        {
            var dbContext = new ConquiánDBEntities();
            IPlayerRepository playerRepository = new PlayerRepository(dbContext);
            this.playerRepository = playerRepository;
            authLogic = new AuthenticationLogic(playerRepository);
        }

        public async Task<PlayerDto> AuthenticatePlayerAsync(string playerEmail, string playerPassword)
        {
 
            var player = await playerRepository.GetPlayerByEmailAsync(playerEmail);

            if (player != null)
            {
                if (PresenceManager.Instance.IsPlayerOnline(player.idPlayer)) 
                {
                    SessionActiveFault faultDetail = new SessionActiveFault(
                        "Ya cuenta con una sesión activa, por favor cierre la sesión activa para poder abrir una nueva"
                    );
                    throw new FaultException<SessionActiveFault>(faultDetail, new FaultReason(faultDetail.Message));
                }
            }
            return await authLogic.AuthenticatePlayerAsync(playerEmail, playerPassword); 
        }

        public async Task SignOutPlayerAsync(int idPlayer)
        {
            await authLogic.SignOutPlayerAsync(idPlayer);
        }
    }
}
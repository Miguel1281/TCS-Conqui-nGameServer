using ConquiánServidor.BusinessLogic;
using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.DataAccess.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ConquiánServidor.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class PasswordRecoveryService : IPasswordRecovery
    {
        private readonly AuthenticationLogic authLogic;

        public PasswordRecoveryService()
        {
            var dbContext = new ConquiánDBEntities();
            IPlayerRepository playerRepository = new PlayerRepository(dbContext);
            this.authLogic = new AuthenticationLogic(playerRepository);
        }

        public async Task<bool> RequestPasswordRecoveryAsync(string email)
        {
            try
            {
                return await authLogic.HandlePasswordRecoveryRequestAsync(email);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en RequestPasswordRecoveryAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ValidateRecoveryTokenAsync(string email, string token)
        {
            try
            {
                return await authLogic.HandleTokenValidationAsync(email, token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en ValidateRecoveryTokenAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
        {
            try
            {
                return await authLogic.HandlePasswordResetAsync(email, token, newPassword);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en ResetPasswordAsync: {ex.Message}");
                return false;
            }
        }
    }
}

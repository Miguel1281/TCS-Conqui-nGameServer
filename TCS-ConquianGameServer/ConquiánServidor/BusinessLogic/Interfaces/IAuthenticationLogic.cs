using ConquiánServidor.Contracts.DataContracts;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic.Interfaces
{
    public interface IAuthenticationLogic
    {
        Task<PlayerDto> AuthenticatePlayerAsync(string playerEmail, string playerPassword);
        Task SignOutPlayerAsync(int idPlayer);
        Task RegisterPlayerAsync(PlayerDto finalPlayerData);
        Task<string> GenerateAndStoreRecoveryTokenAsync(string email);
        Task<string> SendVerificationCodeAsync(string email);
        Task VerifyCodeAsync(string email, string code);
        Task HandlePasswordRecoveryRequestAsync(string email);
        Task HandleTokenValidationAsync(string email, string token);
        Task HandlePasswordResetAsync(string email, string token, string newPassword);
        Task DeleteTemporaryPlayerAsync(string email);
    }
}
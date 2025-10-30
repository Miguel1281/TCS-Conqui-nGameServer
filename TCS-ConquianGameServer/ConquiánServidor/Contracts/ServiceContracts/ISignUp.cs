using ConquiánServidor.Contracts.DataContracts;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Contracts.ServiceContracts
{
    [ServiceContract]
    public interface ISignUp
    {
        [OperationContract]
        Task<bool> RegisterPlayerAsync(PlayerDto newPlayer);

        [OperationContract]
        Task<string> SendVerificationCodeAsync(string email);

        [OperationContract]
        Task<bool> VerifyCodeAsync(string email, string code);

        [OperationContract]
        Task<bool> CancelRegistrationAsync(string email);
    }
}

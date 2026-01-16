using ConquiánServidor.Contracts.DataContracts;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Contracts.ServiceContracts
{
    [ServiceContract]
    public interface ISignUp
    {
        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        Task RegisterPlayerAsync(PlayerDto newPlayer); 

        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        Task<string> SendVerificationCodeAsync(string email); 

        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        Task VerifyCodeAsync(string email, string code);

        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        Task CancelRegistrationAsync(string email);
    }
}

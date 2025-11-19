using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.FaultContracts;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Contracts.ServiceContracts
{
    [ServiceContract]
    internal interface ILogin
    {
        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        Task<PlayerDto> AuthenticatePlayerAsync(string email, string password);

        [OperationContract]
        Task SignOutPlayerAsync(int idPlayer);
    }
}

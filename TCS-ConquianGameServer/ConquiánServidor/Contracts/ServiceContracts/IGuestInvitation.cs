using ConquiánServidor.Contracts.DataContracts;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Contracts.ServiceContracts
{
    [ServiceContract]
    public interface IGuestInvitation
    {
        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        Task SendGuestInviteAsync(string roomCode, string email);
    }
}
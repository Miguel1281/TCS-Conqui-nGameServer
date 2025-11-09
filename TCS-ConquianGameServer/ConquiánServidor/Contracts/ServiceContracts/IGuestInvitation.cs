using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Contracts.ServiceContracts
{
    [ServiceContract]
    public interface IGuestInvitation
    {
        [OperationContract]
        Task<bool> SendGuestInviteAsync(string roomCode, string email);
    }
}
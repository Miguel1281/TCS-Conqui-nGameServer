using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Contracts.ServiceContracts
{
    [ServiceContract(CallbackContract = typeof(IInvitationCallback))]
    public interface IInvitationService
    {
        [OperationContract(IsOneWay = true)]
        void Subscribe(int idPlayer);

        [OperationContract(IsOneWay = true)]
        void Unsubscribe(int idPlayer);

        [OperationContract]
        Task<bool> SendInvitationAsync(int idSender, string senderNickname, int idReceiver, string roomCode);
    }
}
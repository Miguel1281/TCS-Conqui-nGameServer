using ConquiánServidor.Contracts.DataContracts;
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
        [FaultContract(typeof(ServiceFaultDto))] 
        Task SendInvitationAsync(int idSender, string senderNickname, int idReceiver, string roomCode);
    }
}
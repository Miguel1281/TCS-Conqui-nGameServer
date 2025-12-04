using ConquiánServidor.Contracts.ServiceContracts;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic.Interfaces
{
    public interface IInvitationManager
    {
        void Subscribe(int idPlayer, IInvitationCallback callback);
        void Unsubscribe(int idPlayer);
        Task SendInvitationAsync(int idSender, string senderNickname, int idReceiver, string roomCode);
    }
}
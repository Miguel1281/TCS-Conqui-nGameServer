using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic.Interfaces
{
    public interface IGuestInvitationLogic
    {
        Task SendGuestInviteAsync(string roomCode, string email);
    }
}

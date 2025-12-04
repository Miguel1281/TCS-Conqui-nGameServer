using static ConquiánServidor.BusinessLogic.GuestInvitationManager; 

namespace ConquiánServidor.BusinessLogic.Interfaces
{
    public interface IGuestInvitationManager
    {
        InviteResult ValidateInvitation(string email, string roomCode);
    }
}
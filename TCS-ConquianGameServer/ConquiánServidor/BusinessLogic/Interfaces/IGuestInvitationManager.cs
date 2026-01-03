using static ConquiánServidor.BusinessLogic.GuestInvitationManager; 

namespace ConquiánServidor.BusinessLogic.Interfaces
{
    public interface IGuestInvitationManager
    {
        void AddInvitation(string email, string roomCode);
        InviteResult ValidateInvitation(string email, string roomCode);
    }
}
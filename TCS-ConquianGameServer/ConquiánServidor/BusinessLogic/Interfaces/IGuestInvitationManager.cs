using ConquiánServidor.BusinessLogic.Guest;

namespace ConquiánServidor.BusinessLogic.Interfaces
{
    public interface IGuestInvitationManager
    {
        void AddInvitation(string email, string roomCode);
        GuestInvitationManager.InviteResult ValidateInvitation(string email, string roomCode);
    }
}
using ConquiánServidor.Contracts.ServiceContracts;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic.Interfaces
{
    public interface IPresenceManager
    {
        bool IsPlayerOnline(int idPlayer);
        void Subscribe(int idPlayer, IPresenceCallback callback);
        void Unsubscribe(int idPlayer);
        Task NotifyStatusChange(int changedPlayerId, int newStatusId);
        void NotifyNewFriendRequest(int targetUserId);
        void NotifyFriendListUpdate(int targetUserId);
        bool IsPlayerInGame(int playerId); 
    }
}
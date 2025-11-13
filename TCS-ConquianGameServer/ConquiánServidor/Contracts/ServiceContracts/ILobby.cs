using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.FaultContracts;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Contracts.ServiceContracts
{
    [ServiceContract(CallbackContract = typeof(ILobbyCallback))]
    public interface ILobby
    {
        [OperationContract]
        Task<LobbyDto> GetLobbyStateAsync(string roomCode);

        [OperationContract]
        Task<string> CreateLobbyAsync(int idHostPlayer);

        [OperationContract]
        Task<bool> JoinAndSubscribeAsync(string roomCode, int idPlayer);

        [OperationContract]
        [FaultContract(typeof(GuestInviteUsedFault))]
        [FaultContract(typeof(RegisteredUserAsGuestFault))] 
        Task<PlayerDto> JoinAndSubscribeAsGuestAsync(string email, string roomCode);

        [OperationContract(IsOneWay = true)]
        void LeaveAndUnsubscribe(string roomCode, int idPlayer);

        [OperationContract]
        Task SendMessageAsync(string roomCode, MessageDto message);

        [OperationContract]
        Task SelectGamemodeAsync(string roomCode, int idGamemode);

        [OperationContract(IsOneWay = true)]
        void StartGame(string roomCode);
    }
}

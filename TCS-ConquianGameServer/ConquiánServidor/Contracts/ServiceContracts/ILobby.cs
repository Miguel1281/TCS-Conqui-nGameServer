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
        [FaultContract(typeof(ServiceFaultDto))]
        Task<LobbyDto> GetLobbyStateAsync(string roomCode);

        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        Task<string> CreateLobbyAsync(int idHostPlayer);

        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        Task<bool> JoinAndSubscribeAsync(string roomCode, int idPlayer);

        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        [FaultContract(typeof(GuestInviteUsedFault))]
        [FaultContract(typeof(RegisteredUserAsGuestFault))]
        Task<PlayerDto> JoinAndSubscribeAsGuestAsync(string email, string roomCode);

        [OperationContract(IsOneWay = true)]
        void LeaveAndUnsubscribe(string roomCode, int idPlayer);

        [OperationContract]
        Task SendMessageAsync(string roomCode, MessageDto message);

        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        Task SelectGamemodeAsync(string roomCode, int idGamemode);

        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        Task StartGameAsync(string roomCode);

        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        Task KickPlayerAsync(string roomCode, int idRequestingPlayer, int idPlayerToKick);
    }
}
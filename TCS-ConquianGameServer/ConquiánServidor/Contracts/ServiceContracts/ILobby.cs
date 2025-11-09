using ConquiánServidor.Contracts.DataContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
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

        [OperationContract(IsOneWay = true)]
        void LeaveAndUnsubscribe(string roomCode, int idPlayer);

        [OperationContract]
        Task SendMessageAsync(string roomCode, MessageDto message);

        [OperationContract]
        Task SelectGamemodeAsync(string roomCode, int idGamemode);

        [OperationContract]
        Task StartGameAsync(string roomCode);
    }
}

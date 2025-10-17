using ConquiánServidor.Contracts.DataContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ConquiánServidor.Contracts.ServiceContracts
{
    [ServiceContract]
    public interface ILobby
    {
        [OperationContract]
        Task<LobbyDto> GetLobbyStateAsync(string roomCode);

        [OperationContract]
        Task<string> CreateLobbyAsync(int idHostPlayer);

        [OperationContract]
        Task<bool> JoinLobbyAsync(string roomCode, int idPlayer);

        [OperationContract]
        Task LeaveLobbyAsync(string roomCode, int idPlayer);

        [OperationContract]
        Task SendMessageAsync(string roomCode, MessageDto message);

        [OperationContract]
        Task<List<MessageDto>> GetChatMessagesAsync(string roomCode);
    }
}

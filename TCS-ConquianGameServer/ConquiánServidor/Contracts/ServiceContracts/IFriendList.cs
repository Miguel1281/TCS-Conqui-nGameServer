using ConquiánServidor.Contracts.DataContracts;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Contracts.ServiceContracts
{
    [ServiceContract]
    public interface IFriendList
    {
        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        Task<PlayerDto> GetPlayerByNicknameAsync(string nickname, int idCurrentUser);

        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        Task<List<PlayerDto>> GetFriendsAsync(int idPlayer);

        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        Task SendFriendRequestAsync(int idSender, int idReceiver);

        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        Task<List<FriendRequestDto>> GetFriendRequestsAsync(int idPlayer);

        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        Task UpdateFriendRequestStatusAsync(int idFriendship, int idStatus);

        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        Task DeleteFriendAsync(int idPlayer, int idFriend);
    }
}
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
        Task<PlayerDto> GetPlayerByNicknameAsync(string nickname, int idCurrentUser);

        [OperationContract]
        Task<List<PlayerDto>> GetFriendsAsync(int idPlayer);

        [OperationContract]
        Task<bool> SendFriendRequestAsync(int idSender, int idReceiver);
    }
}
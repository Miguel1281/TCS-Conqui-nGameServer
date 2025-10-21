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
    public interface ILobbyCallback
    {
        [OperationContract(IsOneWay = true)]
        void PlayerJoined(PlayerDto newPlayer);

        [OperationContract(IsOneWay = true)]
        void PlayerLeft(int idPlayer);

        [OperationContract(IsOneWay = true)]
        void HostLeft();

        [OperationContract(IsOneWay = true)]
        void MessageReceived(MessageDto message);
    }
}

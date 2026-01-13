using ConquiánServidor.Contracts.DataContracts;
using System.ServiceModel;

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

        [OperationContract(IsOneWay = true)]
        void NotifyGamemodeChanged(int newGamemodeId);

        [OperationContract(IsOneWay = true)]
        void NotifyGameStarting();

        [OperationContract(IsOneWay = true)]
        void YouWereKicked();

        [OperationContract]
        bool Ping();
    }
}

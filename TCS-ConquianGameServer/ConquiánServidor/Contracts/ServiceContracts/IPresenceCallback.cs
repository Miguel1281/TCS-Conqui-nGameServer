using System.ServiceModel;

namespace ConquiánServidor.Contracts.ServiceContracts
{
    [ServiceContract]
    public interface IPresenceCallback
    {
        [OperationContract(IsOneWay = true)]
        void OnFriendStatusChanged(int friendId, int newStatusId);

        [OperationContract(IsOneWay = true)]
        void OnFriendRequestReceived();

        [OperationContract(IsOneWay = true)]
        void OnFriendListUpdated();
    }
}

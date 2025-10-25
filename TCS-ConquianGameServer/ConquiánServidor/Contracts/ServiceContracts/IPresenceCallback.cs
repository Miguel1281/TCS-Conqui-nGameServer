using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ConquiánServidor.Contracts.ServiceContracts
{
    [ServiceContract]
    public interface IPresenceCallback
    {
        [OperationContract(IsOneWay = true)]
        void OnFriendStatusChanged(int friendId, int newStatusId);
    }
}

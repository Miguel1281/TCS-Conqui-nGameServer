using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ConquiánServidor.Contracts.ServiceContracts
{
    [ServiceContract(CallbackContract = typeof(IPresenceCallback))]
    public interface IPresence
    {
        [OperationContract(IsOneWay = true)]
        void Subscribe(int idPlayer);

        [OperationContract(IsOneWay = true)]
        void Unsubscribe(int idPlayer);
    }
}

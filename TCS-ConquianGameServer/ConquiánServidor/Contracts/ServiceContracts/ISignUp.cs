using ConquiánServidor.ConquiánDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ConquiánServidor.Contracts
{
    [ServiceContract]
    public interface ISignUp
    {
        [OperationContract]
        bool RegisterPlayer(Player newPlayer);

        [OperationContract]
        string SendVerificationCode(string email);
    }
}

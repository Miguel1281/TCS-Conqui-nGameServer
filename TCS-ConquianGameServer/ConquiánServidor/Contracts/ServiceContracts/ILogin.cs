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
    internal interface ILogin
    {
        [OperationContract]
        bool SignIn(string mail, string password);
    }
}

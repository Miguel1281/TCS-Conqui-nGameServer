using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.FaultContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ConquiánServidor.Contracts.ServiceContracts
{
    [ServiceContract]
    internal interface ILogin
    {
        [OperationContract]
        [FaultContract(typeof(SessionActiveFault))]
        Task<PlayerDto> AuthenticatePlayerAsync(string email, string password);

        [OperationContract]
        Task SignOutPlayerAsync(int idPlayer);
    }
}

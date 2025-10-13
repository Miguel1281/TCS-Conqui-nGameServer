using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.DataContracts;
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
        Task<PlayerDto> AuthenticatePlayerAsync(string email, string password);
    }
}

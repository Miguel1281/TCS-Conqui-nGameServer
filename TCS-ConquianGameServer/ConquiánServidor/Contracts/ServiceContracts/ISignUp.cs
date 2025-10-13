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
        Task<bool> RegisterPlayerAsync(Player newPlayer);

        [OperationContract]
        Task<string> SendVerificationCodeAsync(string email);

        [OperationContract]
        Task<bool> VerifyCodeAsync(string email, string code);
    }
}

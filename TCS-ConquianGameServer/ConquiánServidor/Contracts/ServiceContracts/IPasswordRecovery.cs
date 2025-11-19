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
    public interface IPasswordRecovery
    {
        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        Task<bool> RequestPasswordRecoveryAsync(string email, int mode);

        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        Task<bool> ValidateRecoveryTokenAsync(string email, string token);

        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
    }
}

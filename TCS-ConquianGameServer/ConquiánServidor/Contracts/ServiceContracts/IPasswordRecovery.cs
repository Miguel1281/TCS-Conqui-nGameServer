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
        Task<bool> RequestPasswordRecoveryAsync(string email, int mode);

        [OperationContract]
        Task<bool> ValidateRecoveryTokenAsync(string email, string token);

        [OperationContract]
        Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
    }
}

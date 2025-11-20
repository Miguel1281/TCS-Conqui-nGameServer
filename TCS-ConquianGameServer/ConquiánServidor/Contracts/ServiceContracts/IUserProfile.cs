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
    public interface IUserProfile
    {
        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        Task<PlayerDto> GetPlayerByIdAsync(int idPlayer);

        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        Task UpdatePlayerAsync(PlayerDto playerDto);

        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        Task<List<SocialDto>> GetPlayerSocialsAsync(int idPlayer);

        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        Task UpdatePlayerSocialsAsync(int idPlayer, List<SocialDto> socials);

        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        Task UpdateProfilePictureAsync(int idPlayer, string newPath);
    }
}
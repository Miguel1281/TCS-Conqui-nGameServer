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
        Task<PlayerDto> GetPlayerByIdAsync(int idPlayer);

        [OperationContract]
        Task<bool> UpdatePlayerAsync(PlayerDto playerDto);

        [OperationContract]
        Task<List<SocialDto>> GetPlayerSocialsAsync(int idPlayer);

        [OperationContract]
        Task<bool> UpdatePlayerSocialsAsync(int idPlayer, List<SocialDto> socials);

        [OperationContract]
        Task<bool> UpdateProfilePictureAsync(int idPlayer, string newPath);
    }
}
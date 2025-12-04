using ConquiánServidor.Contracts.DataContracts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic.Interfaces
{
    public interface IUserProfileLogic
    {
        Task<PlayerDto> GetPlayerByIdAsync(int idPlayer);
        Task<List<SocialDto>> GetPlayerSocialsAsync(int idPlayer);
        Task UpdatePlayerAsync(PlayerDto playerDto);
        Task UpdatePlayerSocialsAsync(int idPlayer, List<SocialDto> socialDtos);
        Task UpdateProfilePictureAsync(int idPlayer, string newPath);
    }
}
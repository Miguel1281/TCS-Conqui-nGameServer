using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConquiánServidor.ConquiánDB.Abstractions
{
    public interface ISocialRepository
    {
        Task<List<Social>> GetSocialsByPlayerIdAsync(int idPlayer);
        Task<bool> DoesPlayerExistAsync(int idPlayer);
        void RemoveSocialsRange(IEnumerable<Social> socials);
        void AddSocial(Social social);
        Task<int> SaveChangesAsync();
    }
}
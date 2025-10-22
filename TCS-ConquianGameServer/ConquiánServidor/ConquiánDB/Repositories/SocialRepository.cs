using ConquiánServidor.ConquiánDB;
using ConquiánServidor.DataAccess.Abstractions;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace ConquiánServidor.DataAccess.Repositories
{
    public class SocialRepository : ISocialRepository
    {
        private readonly ConquiánDBEntities context;

        public SocialRepository(ConquiánDBEntities context)
        {
            this.context = context;
        }

        public async Task<bool> DoesPlayerExistAsync(int idPlayer)
        {
            return await context.Player.AnyAsync(p => p.idPlayer == idPlayer);
        }

        public async Task<List<Social>> GetSocialsByPlayerIdAsync(int idPlayer)
        {
            context.Configuration.LazyLoadingEnabled = false;
            return await context.Social.Where(s => s.idPlayer == idPlayer).ToListAsync();
        }

        public void AddSocial(Social social)
        {
            context.Social.Add(social);
        }

        public void RemoveSocialsRange(IEnumerable<Social> socials)
        {
            context.Social.RemoveRange(socials);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await context.SaveChangesAsync();
        }
    }
}
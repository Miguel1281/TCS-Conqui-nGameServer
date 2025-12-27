using ConquiánServidor.ConquiánDB.Abstractions;
using System.Threading.Tasks;

namespace ConquiánServidor.ConquiánDB.Repositories
{
    public class GameRepository : IGameRepository
    {
        private readonly ConquiánDBEntities context;

        public GameRepository(ConquiánDBEntities context)
        {
            this.context = context;
        }

        public async Task AddGameAsync(Game game)
        {
            context.Game.Add(game);
            await context.SaveChangesAsync();
        }
    }
}

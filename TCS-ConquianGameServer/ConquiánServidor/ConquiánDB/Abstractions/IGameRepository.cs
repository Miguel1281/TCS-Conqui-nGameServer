using System.Threading.Tasks;

namespace ConquiánServidor.ConquiánDB.Abstractions
{
    public interface IGameRepository
    {
        Task AddGameAsync(Game game);
    }
}

using ConquiánServidor.ConquiánDB.Abstractions;
using System.Data.Entity;
using System.Threading.Tasks;

namespace ConquiánServidor.ConquiánDB.Repositories
{
    public class LobbyRepository : ILobbyRepository
    {
        private readonly ConquiánDBEntities context;

        public LobbyRepository(ConquiánDBEntities context)
        {
            this.context = context;
        }

        public void AddLobby(Lobby lobby)
        {
            context.Lobby.Add(lobby);
        }

        public async Task<bool> DoesRoomCodeExistAsync(string roomCode)
        {
            return await context.Lobby.AnyAsync(l => l.roomCode == roomCode);
        }

        public async Task<Lobby> GetLobbyByRoomCodeAsync(string roomCode)
        {
            return await context.Lobby
                .Include(l => l.Player)
                .Include(l => l.StatusLobby)
                .Include(l => l.Gamemode)
                .FirstOrDefaultAsync(l => l.roomCode == roomCode);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await context.SaveChangesAsync();
        }
    }
}
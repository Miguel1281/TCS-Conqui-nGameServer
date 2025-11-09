using ConquiánServidor.BusinessLogic.Game;
using ConquiánServidor.Contracts.DataContracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic
{
    public class GameSessionManager
    {
        private readonly ConcurrentDictionary<string, ConquianGame> activeGames;

        private static readonly Lazy<GameSessionManager> instance =
            new Lazy<GameSessionManager>(() => new GameSessionManager());

        public static GameSessionManager Instance => instance.Value;

        private GameSessionManager()
        {
            activeGames = new ConcurrentDictionary<string, ConquianGame>();
        }

        public ConquianGame CreateGame(string roomCode, int gamemodeId, List<PlayerDto> players)
        {
            var newGame = new ConquianGame(roomCode, gamemodeId, players);
            activeGames.TryAdd(roomCode, newGame);
            return newGame;
        }

        public ConquianGame GetGame(string roomCode)
        {
            activeGames.TryGetValue(roomCode, out ConquianGame game);
            return game;
        }

        public void EndGame(string roomCode)
        {
            activeGames.TryRemove(roomCode, out _);
        }
    }
}

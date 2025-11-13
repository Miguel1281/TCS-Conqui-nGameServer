using ConquiánServidor.BusinessLogic.Game;
using ConquiánServidor.Contracts.DataContracts;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ConquiánServidor.BusinessLogic
{
    public class GameSessionManager
    {
        private static readonly GameSessionManager instance = new GameSessionManager();

        private readonly ConcurrentDictionary<string, ConquianGame> games =
            new ConcurrentDictionary<string, ConquianGame>();

        private GameSessionManager() { }

        public static GameSessionManager Instance => instance;

        public void CreateGame(string roomCode, int gamemodeId, List<PlayerDto> players)
        {
            var newGame = new ConquianGame(roomCode, gamemodeId, players);
            games.TryAdd(roomCode, newGame);
        }

        public ConquianGame GetGame(string roomCode)
        {
            games.TryGetValue(roomCode, out var game);
            return game;
        }

        public void RemoveGame(string roomCode)
        {
            games.TryRemove(roomCode, out _);
        }
    }
}
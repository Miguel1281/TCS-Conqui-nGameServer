using ConquiánServidor.BusinessLogic.Game;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.Contracts.DataContracts;
using NLog;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ConquiánServidor.BusinessLogic
{
    public class GameSessionManager:IGameSessionManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly ConcurrentDictionary<string, ConquianGame> games =
            new ConcurrentDictionary<string, ConquianGame>();

        public GameSessionManager() 
        {
        }

        public void CreateGame(string roomCode, int gamemodeId, List<PlayerDto> players)
        {
            Logger.Info($"Attempting to create game session. Room Code: {roomCode}, Gamemode: {gamemodeId}");

            var newGame = new ConquianGame(roomCode, gamemodeId, players);

            if (games.TryAdd(roomCode, newGame))
            {
                Logger.Info($"Game session created successfully for Room Code: {roomCode}. Total active games: {games.Count}");
            }
            else
            {
                Logger.Warn($"Failed to create game session: Room Code {roomCode} already exists.");
            }
        }

        public ConquianGame GetGame(string roomCode)
        {
            if (games.TryGetValue(roomCode, out var game))
            {
                return game;
            }
            Logger.Warn($"Game session lookup failed: Room Code {roomCode} not found.");
            return null;
        }

        public void RemoveGame(string roomCode)
        {
            if (games.TryRemove(roomCode, out _))
            {
                Logger.Info($"Game session removed successfully for Room Code: {roomCode}. Remaining games: {games.Count}");
            }
            else
            {
                Logger.Warn($"Attempt to remove game session failed: Room Code {roomCode} not found.");
            }
        }
    }
}
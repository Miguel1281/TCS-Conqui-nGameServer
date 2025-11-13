using ConquiánServidor.BusinessLogic;
using ConquiánServidor.BusinessLogic.Game;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using System.Collections.Concurrent;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class Game : IGame
    {
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<int, IGameCallback>> gameCallbacks =
            new ConcurrentDictionary<string, ConcurrentDictionary<int, IGameCallback>>();

        public async Task<GameStateDto> JoinGameAsync(string roomCode, int playerId)
        {
            var callback = OperationContext.Current.GetCallbackChannel<IGameCallback>();

            var game = GameSessionManager.Instance.GetGame(roomCode);
            if (game == null)
            {
                return null;
            }

            gameCallbacks.TryAdd(roomCode, new ConcurrentDictionary<int, IGameCallback>());
            gameCallbacks[roomCode][playerId] = callback;

            var gameState = BuildGameStateForPlayer(game, playerId);

            // TODO: Notificar al oponente que te has unido.
            // Puedes implementar esto más tarde.

            return await Task.FromResult(gameState);
        }

        private GameStateDto BuildGameStateForPlayer(ConquianGame game, int playerId)
        {
            var playerHandDto = game.PlayerHands[playerId]
                .Select(card => new CardDto
                {
                    Id = card.Id,
                    Suit = card.Suit,
                    Rank = card.Rank,
                    ImagePath = card.ImagePath
                }).ToList();

            var topDiscardCard = game.DiscardPile.Last();
            var topDiscardDto = new CardDto
            {
                Id = topDiscardCard.Id,
                Suit = topDiscardCard.Suit,
                Rank = topDiscardCard.Rank,
                ImagePath = topDiscardCard.ImagePath
            };

            var opponentDto = game.Players.First(p => p.idPlayer != playerId);

            int currentTurnPlayerId = game.Players.First().idPlayer;

            return new GameStateDto
            {
                PlayerHand = playerHandDto,
                TopDiscardCard = topDiscardDto,
                Opponent = opponentDto,
                CurrentTurnPlayerId = currentTurnPlayerId
            };
        }
    }
}
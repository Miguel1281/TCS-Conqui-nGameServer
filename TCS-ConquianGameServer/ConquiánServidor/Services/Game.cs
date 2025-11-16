using ConquiánServidor.BusinessLogic;
using ConquiánServidor.BusinessLogic.Game;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using System;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class Game : IGame
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public async Task<GameStateDto> JoinGameAsync(string roomCode, int playerId)
        {
            var callback = OperationContext.Current.GetCallbackChannel<IGameCallback>();

            var game = GameSessionManager.Instance.GetGame(roomCode);
            if (game == null)
            {
                return null;
            }

            game.RegisterPlayerCallback(playerId, callback);

            var gameState = BuildGameStateForPlayer(game, playerId);

            return await Task.FromResult(gameState);
        }

        public void PlayCards(string roomCode, int playerId, string[] cardIds)
        {
            try
            {
                var game = GameSessionManager.Instance.GetGame(roomCode);
                if (game != null)
                {
                    game.ProcessPlayerMove(playerId, cardIds.ToList());
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error processing PlayCards for player {playerId} in room {roomCode}");
            }
        }
        public void DrawFromDeck(string roomCode, int playerId)
        {
            try
            {
                var game = GameSessionManager.Instance.GetGame(roomCode);
                if (game != null)
                {
                    game.DrawFromDeck(playerId);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error processing DrawFromDeck for player {playerId} in room {roomCode}");
            }
        }

        public async Task<CardDto> DrawFromDiscardAsync(string roomCode, int playerId)
        {
            try
            {
                var game = GameSessionManager.Instance.GetGame(roomCode);
                if (game != null)
                {
                    CardDto card = game.DrawFromDiscard(playerId);
                    return await Task.FromResult(card);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error processing DrawFromDiscardAsync for player {playerId} in room {roomCode}");
            }
            return null;
        }

        public void DiscardCard(string roomCode, int playerId, string cardId)
        {
            try
            {
                var game = GameSessionManager.Instance.GetGame(roomCode);
                if (game != null)
                {
                    game.DiscardCard(playerId, cardId);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error processing DiscardCard for player {playerId} in room {roomCode}");
            }
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

            CardDto topDiscardDto = null;
            if (game.DiscardPile.Count > 0) 
            {
                var topDiscardCard = game.DiscardPile[game.DiscardPile.Count - 1];

                topDiscardDto = new CardDto
                {
                    Id = topDiscardCard.Id,
                    Suit = topDiscardCard.Suit,
                    Rank = topDiscardCard.Rank,
                    ImagePath = topDiscardCard.ImagePath
                };
            }

            var opponentDto = game.Players.First(p => p.idPlayer != playerId);

            int currentTurnPlayerId = game.GetCurrentTurnPlayerId();
            int opponentCards = game.PlayerHands[opponentDto.idPlayer].Count;
            int totalSeconds = game.GetInitialTimeInSeconds();

            return new GameStateDto
            {
                PlayerHand = playerHandDto,
                TopDiscardCard = topDiscardDto,
                Opponent = opponentDto,
                CurrentTurnPlayerId = currentTurnPlayerId,
                OpponentCardCount = opponentCards,
                TotalGameSeconds = totalSeconds
            };
        }
    }
}
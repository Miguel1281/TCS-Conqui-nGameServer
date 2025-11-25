using ConquiánServidor.BusinessLogic;
using ConquiánServidor.BusinessLogic.Game;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.Properties.Langs;
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
            try
            {
                var game = GameSessionManager.Instance.GetGame(roomCode);
                if (game == null)
                {
                    throw new InvalidOperationException(Lang.ErrorGameNotFound);
                }

                var callback = OperationContext.Current.GetCallbackChannel<IGameCallback>();
                game.RegisterPlayerCallback(playerId, callback);

                var gameState = BuildGameStateForPlayer(game, playerId);

                Logger.Info(string.Format(Lang.LogGameJoinSuccess, playerId, roomCode));

                return await Task.FromResult(gameState);
            }
            catch (InvalidOperationException ex)
            {
                Logger.Warn(ex, $"Error controlado en JoinGameAsync: {ex.Message}");
                var faultData = new ServiceFaultDto(ServiceErrorType.OperationFailed, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.Message));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error crítico en JoinGameAsync room {roomCode} player {playerId}");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, Lang.ErrorGameAction);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason("Internal Server Error"));
            }
        }

        public async Task PlayCardsAsync(string roomCode, int playerId, string[] cardIds)
        {
            try
            {
                var game = GameSessionManager.Instance.GetGame(roomCode);
                if (game == null)
                {
                    throw new InvalidOperationException(Lang.ErrorGameNotFound);
                }

                game.ProcessPlayerMove(playerId, cardIds.ToList());

                await Task.CompletedTask;
            }
            catch (InvalidOperationException ex)
            {
                var faultData = new ServiceFaultDto(ServiceErrorType.OperationFailed, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.Message));
            }
            catch (ArgumentException ex)
            {
                var faultData = new ServiceFaultDto(ServiceErrorType.OperationFailed, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.Message));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error en PlayCards para jugador {playerId} en sala {roomCode}");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, Lang.ErrorGameAction);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason("Error procesando jugada"));
            }
        }

        public async Task DrawFromDeckAsync(string roomCode, int playerId)
        {
            try
            {
                var game = GameSessionManager.Instance.GetGame(roomCode);
                if (game == null)
                {
                    throw new InvalidOperationException(Lang.ErrorGameNotFound);
                }

                game.DrawFromDeck(playerId);

                await Task.CompletedTask;
            }
            catch (InvalidOperationException ex)
            {
                var faultData = new ServiceFaultDto(ServiceErrorType.OperationFailed, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.Message));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error en DrawFromDeck para jugador {playerId} en sala {roomCode}");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, Lang.ErrorGameAction);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason("Error tomando carta"));
            }
        }

        public async Task PassTurnAsync(string roomCode, int playerId)
        {
            try
            {
                var game = GameSessionManager.Instance.GetGame(roomCode);
                if (game == null)
                {
                    throw new InvalidOperationException(Lang.ErrorGameNotFound);
                }

                game.PassTurn(playerId);

                await Task.CompletedTask;
            }
            catch (InvalidOperationException ex)
            {
                var faultData = new ServiceFaultDto(ServiceErrorType.OperationFailed, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.Message));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error en PassTurnAsync para jugador {playerId} en sala {roomCode}");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, Lang.ErrorGameAction);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason("Error pasando turno"));
            }
        }

        public async Task DiscardCardAsync(string roomCode, int playerId, string cardId)
        {
            try
            {
                var game = GameSessionManager.Instance.GetGame(roomCode);
                if (game == null)
                {
                    throw new InvalidOperationException(Lang.ErrorGameNotFound);
                }

                game.DiscardCard(playerId, cardId);

                await Task.CompletedTask;
            }
            catch (InvalidOperationException ex)
            {
                var faultData = new ServiceFaultDto(ServiceErrorType.OperationFailed, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.Message));
            }
            catch (ArgumentException ex)
            {
                var faultData = new ServiceFaultDto(ServiceErrorType.OperationFailed, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.Message));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error en DiscardCard para jugador {playerId} en sala {roomCode}");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, Lang.ErrorGameAction);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason("Error descartando carta"));
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

        public void LeaveGame(string roomCode, int playerId)
        {
            try
            {
                var game = GameSessionManager.Instance.GetGame(roomCode);
                if (game != null)
                {
                    game.NotifyGameEndedByAbandonment(playerId);

                    GameSessionManager.Instance.RemoveGame(roomCode);
                    Logger.Info($"Partida {roomCode} terminada por abandono del jugador {playerId}.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error en LeaveGame para la sala {roomCode}");
            }
        }
    }
}
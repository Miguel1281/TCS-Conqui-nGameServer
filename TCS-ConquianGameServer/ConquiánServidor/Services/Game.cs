using Autofac;
using ConquiánServidor.BusinessLogic;
using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.BusinessLogic.Game;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.DataAccess.Abstractions;
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
        private readonly GameSessionManager gameSessionManager;
        private readonly ILifetimeScope lifetimeScope;

        private const string INTERNAL_SERVER_ERROR_REASON = "Internal server error";

        public Game()
        {
            Bootstrapper.Init();
            this.gameSessionManager = Bootstrapper.Container.Resolve<GameSessionManager>();
            this.lifetimeScope = Bootstrapper.Container.Resolve<ILifetimeScope>();
        }

        public Game(GameSessionManager gameSessionManager, ILifetimeScope scope)
        {
            this.gameSessionManager = gameSessionManager;
            this.lifetimeScope = scope;
        }

        public async Task<GameStateDto> JoinGameAsync(string roomCode, int playerId)
        {
            try
            {
                var game = this.gameSessionManager.GetGame(roomCode);
                if (game == null)
                {
                    throw new BusinessLogicException(ServiceErrorType.NotFound);
                }

                game.OnGameFinished -= HandleGameFinished;
                game.OnGameFinished += HandleGameFinished;
                var callback = OperationContext.Current.GetCallbackChannel<IGameCallback>();
                game.RegisterPlayerCallback(playerId, callback);

                var gameState = BuildGameStateForPlayer(game, playerId);

                Logger.Info(string.Format(Lang.LogGameJoinSuccess, playerId, roomCode));

                return await Task.FromResult(gameState);
            }
            catch (BusinessLogicException ex)
            {
                var faultData = new ServiceFaultDto(ex.ErrorType, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.Message));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Critical error in JoinGameAsync room {roomCode} player {playerId}");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, ServiceErrorType.OperationFailed.ToString());
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(INTERNAL_SERVER_ERROR_REASON));
            }
        }

        public async Task PlayCardsAsync(string roomCode, int playerId, string[] cardIds)
        {
            try
            {
                var game = this.gameSessionManager.GetGame(roomCode);
                if (game == null)
                {
                    throw new InvalidOperationException(Lang.ErrorGameNotFound);
                }

                game.ProcessPlayerMove(playerId, cardIds.ToList());

                await Task.CompletedTask;
            }
            catch (BusinessLogicException ex)
            {
                var faultData = new ServiceFaultDto(ex.ErrorType, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.Message));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error in PlayCards for player {playerId} in room {roomCode}");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, ServiceErrorType.OperationFailed.ToString());
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(INTERNAL_SERVER_ERROR_REASON));
            }
        }

        private void HandleGameFinished(GameResultDto result)
        {
            if (!result.IsDraw && result.WinnerId > 0 && result.PointsWon > 0)
            {
                try
                {
                    using (var scope = this.lifetimeScope.BeginLifetimeScope())
                    {
                        var playerRepository = scope.Resolve<IPlayerRepository>();
                        var task = playerRepository.UpdatePlayerPointsAsync(result.WinnerId, result.PointsWon);
                        task.Wait();
                    }

                    Logger.Info($"Points updated for winner {result.WinnerId}");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error updating points in DB");
                }
            }
        }

        public async Task DrawFromDeckAsync(string roomCode, int playerId)
        {
            try
            {
                var game = this.gameSessionManager.GetGame(roomCode);
                if (game == null)
                {
                    throw new InvalidOperationException(Lang.ErrorGameNotFound);
                }

                game.DrawFromDeck(playerId);

                await Task.CompletedTask;
            }
            catch (BusinessLogicException ex)
            {
                var faultData = new ServiceFaultDto(ex.ErrorType, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.Message));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error in DrawFromDeck for player {playerId} in room {roomCode}");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, ServiceErrorType.OperationFailed.ToString());
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(INTERNAL_SERVER_ERROR_REASON));
            }
        }

        public async Task PassTurnAsync(string roomCode, int playerId)
        {
            try
            {
                var game = this.gameSessionManager.GetGame(roomCode);
                if (game == null)
                {
                    throw new InvalidOperationException(Lang.ErrorGameNotFound);
                }

                game.PassTurn(playerId);

                await Task.CompletedTask;
            }
            catch (BusinessLogicException ex)
            {
                var faultData = new ServiceFaultDto(ex.ErrorType, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.Message));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error in PassTurnAsync for player {playerId} in room {roomCode}");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, ServiceErrorType.OperationFailed.ToString());
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(INTERNAL_SERVER_ERROR_REASON));
            }
        }

        public async Task DiscardCardAsync(string roomCode, int playerId, string cardId)
        {
            try
            {
                var game = this.gameSessionManager.GetGame(roomCode);
                if (game == null)
                {
                    throw new InvalidOperationException(Lang.ErrorGameNotFound);
                }

                game.DiscardCard(playerId, cardId);

                await Task.CompletedTask;
            }
            catch (BusinessLogicException ex)
            {
                var faultData = new ServiceFaultDto(ex.ErrorType, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.Message));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error in DiscardCard for player {playerId} in room {roomCode}");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, ServiceErrorType.OperationFailed.ToString());
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(INTERNAL_SERVER_ERROR_REASON));
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
                var game = this.gameSessionManager.GetGame(roomCode);
                if (game != null)
                {
                    game.NotifyGameEndedByAbandonment(playerId);

                    this.gameSessionManager.RemoveGame(roomCode);
                    Logger.Info($"Game {roomCode} ended due to player {playerId} leaving.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error in LeaveGame for room {roomCode}");
            }
        }

        public async Task SwapDrawnCardAsync(string roomCode, int playerId, string cardIdToDiscard)
        {
            try
            {
                var game = this.gameSessionManager.GetGame(roomCode);
                if (game == null)
                {
                    throw new InvalidOperationException(Lang.ErrorGameNotFound);
                }

                game.SwapDrawnCard(playerId, cardIdToDiscard);

                await Task.CompletedTask;
            }
            catch (BusinessLogicException ex)
            {
                var faultData = new ServiceFaultDto(ex.ErrorType, ex.Message);
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(ex.Message));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error in SwapDrawnCardAsync for player {playerId} in room {roomCode}");
                var faultData = new ServiceFaultDto(ServiceErrorType.ServerInternalError, ServiceErrorType.OperationFailed.ToString());
                throw new FaultException<ServiceFaultDto>(faultData, new FaultReason(INTERNAL_SERVER_ERROR_REASON));
            }
        }
    }
}
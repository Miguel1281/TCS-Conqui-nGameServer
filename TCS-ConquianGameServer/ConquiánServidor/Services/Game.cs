using Autofac;
using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.BusinessLogic.Game;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.ConquiánDB.Abstractions;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.Enums;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.Properties.Langs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class Game : IGame
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IGameSessionManager gameSessionManager;
        private readonly ILifetimeScope lifetimeScope;
        private readonly IPresenceManager presenceManager;

        private const string INTERNAL_SERVER_ERROR_REASON = "internal server error";

        public Game()
        {
            Bootstrapper.Init();
            this.presenceManager = Bootstrapper.Container.Resolve<IPresenceManager>(); 
            this.gameSessionManager = Bootstrapper.Container.Resolve<IGameSessionManager>();
            this.lifetimeScope = Bootstrapper.Container.Resolve<ILifetimeScope>();
        }

        public Game(IGameSessionManager gameSessionManager, ILifetimeScope scope)
        {
            this.gameSessionManager = gameSessionManager;
            this.lifetimeScope = scope;
        }

        public async Task<GameStateDto> JoinGameAsync(string roomCode, int playerId)
        {
            try
            {
                var game = GetGameOrThrow(roomCode);

                game.OnGameFinished -= HandleGameFinished;
                game.OnGameFinished += HandleGameFinished;
                var callback = OperationContext.Current.GetCallbackChannel<IGameCallback>();
                game.RegisterPlayerCallback(playerId, callback);

                var gameState = BuildGameStateForPlayer(game, playerId);

                Logger.Info(string.Format(Lang.LogGameJoinSuccess, playerId, roomCode));

                return await Task.FromResult(gameState);
            }
            catch (Exception ex)
            {
                throw HandleException(ex, $"critical error in JoinGameAsync room {roomCode} player {playerId}");
            }
        }

        public async Task PlayCardsAsync(string roomCode, int playerId, string[] cardIds)
        {
            try
            {
                var game = GetGameOrThrow(roomCode);
                game.ProcessPlayerMove(playerId, cardIds.ToList());
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                throw HandleException(ex, $"error in PlayCards for player {playerId} in room {roomCode}");
            }
        }

        public async Task DrawFromDeckAsync(string roomCode, int playerId)
        {
            try
            {
                var game = GetGameOrThrow(roomCode);
                game.DrawFromDeck(playerId);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                throw HandleException(ex, $"error in DrawFromDeck for player {playerId} in room {roomCode}");
            }
        }

        public async Task PassTurnAsync(string roomCode, int playerId)
        {
            try
            {
                var game = GetGameOrThrow(roomCode);
                game.PassTurn(playerId);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                throw HandleException(ex, $"error in PassTurnAsync for player {playerId} in room {roomCode}");
            }
        }

        public async Task DiscardCardAsync(string roomCode, int playerId, string cardId)
        {
            try
            {
                var game = GetGameOrThrow(roomCode);
                game.DiscardCard(playerId, cardId);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                throw HandleException(ex, $"error in DiscardCard for player {playerId} in room {roomCode}");
            }
        }

        public async Task SwapDrawnCardAsync(string roomCode, int playerId, string cardIdToDiscard)
        {
            try
            {
                var game = GetGameOrThrow(roomCode);
                game.SwapDrawnCard(playerId, cardIdToDiscard);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                throw HandleException(ex, $"error in SwapDrawnCardAsync for player {playerId} in room {roomCode}");
            }
        }

        public void LeaveGame(string roomCode, int playerId)
        {
            try
            {
                var game = this.gameSessionManager.GetGame(roomCode);
                if (game != null)
                {
                    var playersToNotify = game.Players.Select(p => p.idPlayer).ToList();

                    game.NotifyGameEndedByAbandonment(playerId);
                    this.gameSessionManager.RemoveGame(roomCode);
                    Logger.Info($"game {roomCode} ended due to player {playerId} leaving.");

                    foreach (var id in playersToNotify)
                    {
                        Task.Run(async () =>
                        {
                            try
                            {
                                await this.presenceManager.NotifyStatusChange(id, (int)PlayerStatus.Online);
                            }
                            catch (Exception ex)
                            {
                                Logger.Warn(ex, $"Error updating status for player {id} in LeaveGame");
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"error in LeaveGame for room {roomCode}");
            }
        }

        public void ReportAFK(string roomCode, int playerId)
        {
            try
            {
                var game = this.gameSessionManager.GetGame(roomCode);
                if (game != null)
                {
                    game.ProcessAFK(playerId);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"error reporting AFK for player {playerId} in room {roomCode}");
            }
        }


        private ConquianGame GetGameOrThrow(string roomCode)
        {
            var game = this.gameSessionManager.GetGame(roomCode);
            if (game == null)
            {
                throw new BusinessLogicException(ServiceErrorType.NotFound, Lang.ErrorGameNotFound);
            }
            return game;
        }

        private static Exception HandleException(Exception ex, string logMessage)
        {
            if (ex is BusinessLogicException logicEx)
            {
                var faultData = new ServiceFaultDto(logicEx.ErrorType, logicEx.Message);
                return new FaultException<ServiceFaultDto>(faultData, new FaultReason(logicEx.Message));
            }

            Logger.Error(ex, logMessage);
            var internalFault = new ServiceFaultDto(ServiceErrorType.ServerInternalError, ServiceErrorType.OperationFailed.ToString());
            return new FaultException<ServiceFaultDto>(internalFault, new FaultReason(INTERNAL_SERVER_ERROR_REASON));
        }

        private async void HandleGameFinished(GameResultDto result)
        {
            using (var scope = this.lifetimeScope.BeginLifetimeScope())
            {
                bool databaseError = false;

                if (!result.IsDraw && result.WinnerId > 0)
                {
                    try
                    {
                        var playerRepo = scope.Resolve<IPlayerRepository>();
                        result.PointsWon = await playerRepo.UpdatePlayerPointsAsync(result.WinnerId);
                    }
                    catch (Exception ex) 
                    { 
                        Logger.Error(ex, "error updating points."); 
                        databaseError = true;
                    }
                }

                try
                {
                    var gameRepo = scope.Resolve<IGameRepository>();
                    var dbGame = new ConquiánServidor.ConquiánDB.Game
                    {
                        gameTime = result.DurationSeconds,
                        datePlayed = DateTime.Now,
                        idGamemode = result.GamemodeId,
                        GamePlayer = new List<ConquiánServidor.ConquiánDB.GamePlayer>()
                    };

                    AddPlayerToGame(dbGame, result.Player1Id, result.WinnerId, result.PointsWon);
                    AddPlayerToGame(dbGame, result.Player2Id, result.WinnerId, result.PointsWon);

                    await gameRepo.AddGameAsync(dbGame);
                }
                catch (Exception ex) 
                { 
                    Logger.Error(ex, "error saving history."); 
                    databaseError = true;
                }

                result.ErrorSavingToDatabase = databaseError;

                try
                {
                    var instance = this.gameSessionManager.GetGame(result.RoomCode);
                    instance?.BroadcastGameResult(result);
                }
                catch (Exception ex) 
                { 
                    Logger.Error(ex, "error broadcasting result."); 
                }
            }
        }

        private void AddPlayerToGame(ConquiánServidor.ConquiánDB.Game game, int playerId, int winnerId, int points)
        {
            if (playerId <= 0)
            {
                return;
            }

            game.GamePlayer.Add(new ConquiánServidor.ConquiánDB.GamePlayer
            {
                idPlayer = playerId,
                score = (winnerId == playerId) ? points : 0,
                isWinner = (winnerId == playerId)
            });
        }

        private GameStateDto BuildGameStateForPlayer(ConquianGame game, int playerId)
        {
            var hand = game.PlayerHands[playerId].Select(c => new CardDto
            {
                Id = c.Id,
                Suit = c.Suit,
                Rank = c.Rank,
                ImagePath = c.ImagePath
            }).ToList();

            CardDto topDiscard = null;
            if (game.DiscardPile.Count > 0)
            {
                var c = game.DiscardPile[game.DiscardPile.Count - 1];
                topDiscard = new CardDto { Id = c.Id, Suit = c.Suit, Rank = c.Rank, ImagePath = c.ImagePath };
            }

            var opponent = game.Players.First(p => p.idPlayer != playerId);
            return new GameStateDto
            {
                PlayerHand = hand,
                TopDiscardCard = topDiscard,
                Opponent = opponent,
                CurrentTurnPlayerId = game.GetCurrentTurnPlayerId(),
                OpponentCardCount = game.PlayerHands[opponent.idPlayer].Count,
                TotalGameSeconds = game.GetInitialTimeInSeconds()
            };
        }
    }
}
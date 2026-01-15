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
using System.Data.SqlClient;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ConquiánServidor.Services
{
    internal class GamePlayerData
    {
        public int PlayerId { get; set; }
        public int WinnerId { get; set; }
        public int Points { get; set; }
        public bool IsWinner => PlayerId == WinnerId;
        public int Score => IsWinner ? Points : 0;
    }

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
                if (game == null)
                {
                    Logger.Debug($"Game {roomCode} not found when player {playerId} tried to leave");
                    return;
                }

                var playersToNotify = game.Players.Select(p => p.idPlayer).ToList();

                game.NotifyGameEndedByAbandonment(playerId);
                this.gameSessionManager.RemoveGame(roomCode);
                Logger.Info($"Game {roomCode} ended due to player {playerId} leaving.");

                foreach (var id in playersToNotify)
                {
                    Task.Run(async () => await UpdatePlayerStatusSafe(id));
                }
            }
            catch (BusinessLogicException ex)
            {
                Logger.Warn(ex, $"Business logic error in LeaveGame for room {roomCode}, player {playerId}. ErrorType: {ex.ErrorType}");
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error(ex, $"Invalid operation in LeaveGame for room {roomCode}, player {playerId}");
            }
            catch (ArgumentException ex)
            {
                Logger.Error(ex, $"Invalid argument in LeaveGame for room {roomCode}, player {playerId}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error in LeaveGame for room {roomCode}, player {playerId}. Type: {ex.GetType().Name}");
            }
        }

        public void ReportAFK(string roomCode, int playerId)
        {
            try
            {
                var game = this.gameSessionManager.GetGame(roomCode);
                if (game == null)
                {
                    Logger.Debug($"Game {roomCode} not found when reporting AFK for player {playerId}");
                    return;
                }

                game.ProcessAFK(playerId);
                Logger.Info($"AFK processed for player {playerId} in room {roomCode}");
            }
            catch (BusinessLogicException ex)
            {
                Logger.Warn(ex, $"Business logic error reporting AFK for player {playerId} in room {roomCode}. ErrorType: {ex.ErrorType}");
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error(ex, $"Invalid operation reporting AFK for player {playerId} in room {roomCode}");
            }
            catch (ArgumentException ex)
            {
                Logger.Error(ex, $"Invalid argument reporting AFK for player {playerId} in room {roomCode}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error reporting AFK for player {playerId} in room {roomCode}. Type: {ex.GetType().Name}");
            }
        }

        private async Task UpdatePlayerStatusSafe(int playerId)
        {
            try
            {
                await this.presenceManager.NotifyStatusChange(playerId, (int)PlayerStatus.Online);
                Logger.Debug($"Status updated to Online for player {playerId}");
            }
            catch (BusinessLogicException ex)
            {
                Logger.Warn(ex, $"Business logic error updating status for player {playerId}. ErrorType: {ex.ErrorType}");
            }
            catch (ArgumentException ex)
            {
                Logger.Warn(ex, $"Invalid argument updating status for player {playerId}");
            }
            catch (InvalidOperationException ex)
            {
                Logger.Warn(ex, $"Invalid operation updating status for player {playerId}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error updating status for player {playerId}. Type: {ex.GetType().Name}");
            }
        }

        private GameLogic GetGameOrThrow(string roomCode)
        {
            var game = this.gameSessionManager.GetGame(roomCode);
            if (game == null)
            {
                throw new BusinessLogicException(ServiceErrorType.NotFound, Lang.ErrorGameNotFound);
            }
            return game;
        }

        private async void HandleGameFinished(GameResultDto result)
        {
            using (var scope = this.lifetimeScope.BeginLifetimeScope())
            {
                bool databaseError = false;

                if (!result.IsDraw && result.WinnerId > 0)
                {
                    databaseError = !await UpdateWinnerPointsSafe(scope, result);
                }

                if (!await SaveGameHistorySafe(scope, result))
                {
                    databaseError = true;
                }

                result.ErrorSavingToDatabase = databaseError;

                BroadcastGameResultSafe(result);
            }
        }

        private async Task<bool> UpdateWinnerPointsSafe(ILifetimeScope scope, GameResultDto result)
        {
            try
            {
                var playerRepo = scope.Resolve<IPlayerRepository>();
                result.PointsWon = await playerRepo.UpdatePlayerPointsAsync(result.WinnerId);
                Logger.Info($"Points updated successfully for winner {result.WinnerId}: +{result.PointsWon}");
                return true;
            }
            catch (SqlException ex)
            {
                Logger.Error(ex, $"Database error updating points for player {result.WinnerId}. SqlError: {ex.Number}");
                return false;
            }
            catch (TimeoutException ex)
            {
                Logger.Error(ex, $"Timeout updating points for player {result.WinnerId}");
                return false;
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error(ex, $"Invalid operation updating points for player {result.WinnerId}");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error updating points for player {result.WinnerId}. Type: {ex.GetType().Name}");
                return false;
            }
        }

        private async Task<bool> SaveGameHistorySafe(ILifetimeScope scope, GameResultDto result)
        {
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

                var player1Data = new GamePlayerData
                {
                    PlayerId = result.Player1Id,
                    WinnerId = result.WinnerId,
                    Points = result.PointsWon
                };

                var player2Data = new GamePlayerData
                {
                    PlayerId = result.Player2Id,
                    WinnerId = result.WinnerId,
                    Points = result.PointsWon
                };


                AddPlayerToGame(dbGame, player1Data);
                AddPlayerToGame(dbGame, player2Data);

                await gameRepo.AddGameAsync(dbGame);
                Logger.Info($"Game history saved successfully for room {result.RoomCode}");
                return true;
            }
            catch (SqlException ex)
            {
                Logger.Error(ex, $"Database error saving game history for room {result.RoomCode}. SqlError: {ex.Number}");
                return false;
            }
            catch (TimeoutException ex)
            {
                Logger.Error(ex, $"Timeout saving game history for room {result.RoomCode}");
                return false;
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error(ex, $"Invalid operation saving game history for room {result.RoomCode}");
                return false;
            }
            catch (ArgumentException ex)
            {
                Logger.Error(ex, $"Invalid argument saving game history for room {result.RoomCode}");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error saving game history for room {result.RoomCode}. Type: {ex.GetType().Name}");
                return false;
            }
        }

        private void BroadcastGameResultSafe(GameResultDto result)
        {
            try
            {
                var instance = this.gameSessionManager.GetGame(result.RoomCode);
                if (instance == null)
                {
                    Logger.Debug($"Game instance not found for room {result.RoomCode} when broadcasting result");
                    return;
                }

                instance.BroadcastGameResult(result);
                Logger.Info($"Game result broadcasted successfully for room {result.RoomCode}");
            }
            catch (CommunicationException ex)
            {
                Logger.Error(ex, $"Communication error broadcasting result for room {result.RoomCode}");
            }
            catch (TimeoutException ex)
            {
                Logger.Error(ex, $"Timeout broadcasting result for room {result.RoomCode}");
            }
            catch (ObjectDisposedException ex)
            {
                Logger.Error(ex, $"Channel disposed while broadcasting result for room {result.RoomCode}");
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error(ex, $"Invalid operation broadcasting result for room {result.RoomCode}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error broadcasting result for room {result.RoomCode}. Type: {ex.GetType().Name}");
            }
        }

        private void AddPlayerToGame(ConquiánServidor.ConquiánDB.Game game, GamePlayerData playerData)
        {
            if (playerData.PlayerId <= 0)
            {
                Logger.Debug($"Skipping player with invalid ID: {playerData.PlayerId}");
                return;
            }

            game.GamePlayer.Add(new ConquiánServidor.ConquiánDB.GamePlayer
            {
                idPlayer = playerData.PlayerId,
                score = playerData.Score,
                isWinner = playerData.IsWinner
            });

            Logger.Debug($"Player {playerData.PlayerId} added to game record. Winner: {playerData.IsWinner}, Score: {playerData.Score}");
        }

        private GameStateDto BuildGameStateForPlayer(GameLogic game, int playerId)
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
    }
}
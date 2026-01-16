using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.BusinessLogic.Validation;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Timers;

namespace ConquiánServidor.BusinessLogic.Game
{
    public class GameLogic
    {
        private const int GAMEMODE_CLASSIC = 1;

        private const int TIMER_INTERVAL_MS = 1000;
        private const int TIME_LIMIT_SHORT_SECONDS = 600;
        private const int TIME_LIMIT_LONG_SECONDS = 1200;

        private const int HAND_SIZE_CLASSIC = 6;
        private const int HAND_SIZE_EXTENDED = 8;

        private const int MELDS_TO_WIN_CLASSIC = 2;
        private const int MELDS_TO_WIN_EXTENDED = 3;

        private const int PLAYER_1_INDEX = 0;
        private const int PLAYER_2_INDEX = 1;

        private const string AFK_REASON_SELF = "AFKGameEndedSelf";
        private const string AFK_REASON_RIVAL = "AFKGameEndedRival";

        private static readonly string[] DECK_SUITS = { "Oros", "Copas", "Espadas", "Bastos" };
        private static readonly int[] DECK_RANKS = { 1, 2, 3, 4, 5, 6, 7, 10, 11, 12 };

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public string RoomCode { get; private set; }
        public int GamemodeId { get; private set; }

        private Timer gameTimer;
        private int remainingSeconds;
        private int currentTurnPlayerId;
        private bool mustDiscardToFinishTurn;
        private int? playerReviewingDiscardId;
        private bool hasHostPassedInitialDiscard;
        private bool isCardDrawnFromDeck;
        private bool isGameEnded = false;
        private readonly object endLock = new object();

        private readonly ConcurrentDictionary<int, IGameCallback> playerCallbacks;
        public List<PlayerDto> Players { get; private set; }
        private List<CardsGame> Deck { get; set; }
        public Dictionary<int, List<CardsGame>> PlayerHands { get; private set; }
        public List<CardsGame> StockPile { get; private set; }
        public List<CardsGame> DiscardPile { get; private set; }
        public Dictionary<int, List<List<CardsGame>>> PlayerMelds { get; private set; }

        public event Action<GameResultDto> OnGameFinished;


        public GameLogic(string roomCode, int gamemodeId, List<PlayerDto> players)
        {
            RoomCode = roomCode;
            GamemodeId = gamemodeId;
            Players = players;
            playerCallbacks = new ConcurrentDictionary<int, IGameCallback>();

            currentTurnPlayerId = Players[0].idPlayer;
            playerReviewingDiscardId = currentTurnPlayerId;

            hasHostPassedInitialDiscard = false;
            isCardDrawnFromDeck = false;
            mustDiscardToFinishTurn = false;

            PlayerHands = new Dictionary<int, List<CardsGame>>();
            PlayerMelds = new Dictionary<int, List<List<CardsGame>>>();
            PlayerHands = players.ToDictionary(player => player.idPlayer, player => new List<CardsGame>());
            PlayerMelds = players.ToDictionary(player => player.idPlayer, player => new List<List<CardsGame>>());

            InitializeGame();
            Logger.Info($"Game initialized for Room Code: {RoomCode}. Gamemode: {GamemodeId}");
        }

        private void InitializeGame()
        {
            CreateDeck();
            ShuffleDeck();
            DealHands();
            SetupPiles();
        }

        private void CreateDeck()
        {
            Deck = new List<CardsGame>();

            foreach (var suit in DECK_SUITS)
            {
                foreach (var rank in DECK_RANKS)
                {
                    Deck.Add(new CardsGame(suit, rank));
                }
            }
        }

        private static int GetSecureRandomInt(int max)
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] data = new byte[4];
                rng.GetBytes(data);
                int generatedValue = BitConverter.ToInt32(data, 0) & int.MaxValue;
                return generatedValue % max;
            }
        }

        private void ShuffleDeck()
        {
            int n = Deck.Count;
            while (n > 1)
            {
                n--;
                int k = GetSecureRandomInt(n + 1);
                CardsGame value = Deck[k];
                Deck[k] = Deck[n];
                Deck[n] = value;
            }
        }

        private void DealHands()
        {
            int cardsToDeal;
            if ((GamemodeId == GAMEMODE_CLASSIC))
            {
                cardsToDeal = HAND_SIZE_CLASSIC;
            }
            else
            {
                cardsToDeal = HAND_SIZE_EXTENDED;
            }

            for (int i = 0; i < cardsToDeal; i++)
            {
                foreach (var player in Players)
                {
                    var card = Deck[0];
                    Deck.RemoveAt(0);
                    PlayerHands[player.idPlayer].Add(card);
                }
            }
        }

        private void SetupPiles()
        {
            StockPile = Deck;
            DiscardPile = new List<CardsGame>();
            var firstDiscard = StockPile[0];
            StockPile.RemoveAt(0);
            DiscardPile.Add(firstDiscard);
        }

        public void RegisterPlayerCallback(int playerId, IGameCallback callback)
        {
            playerCallbacks[playerId] = callback;
            Logger.Info($"Player ID {playerId} registered callback for Room Code: {RoomCode}");
        }

        public int GetInitialTimeInSeconds()
        {
            if ((GamemodeId == GAMEMODE_CLASSIC))
            {
                return TIME_LIMIT_SHORT_SECONDS;
            }
            else
            {
                return TIME_LIMIT_LONG_SECONDS;
            }
        }

        public void StartGameTimer()
        {
            remainingSeconds = GetInitialTimeInSeconds();
            gameTimer = new Timer(TIMER_INTERVAL_MS);
            gameTimer.Elapsed += OnTimerTick;
            gameTimer.AutoReset = true;
            gameTimer.Start();
        }

        private void OnTimerTick(object sender, ElapsedEventArgs e)
        {
            remainingSeconds--;

            if (remainingSeconds <= 0)
            {
                Logger.Info($"Game timeout reached for Room Code: {RoomCode}. Stopping game.");
                StopGame();
                DetermineWinnerByPoints();
            }

            BroadcastTime(remainingSeconds, 0, currentTurnPlayerId);
        }

        private void ChangeTurn()
        {
            hasHostPassedInitialDiscard = true;
            mustDiscardToFinishTurn = false;
            var currentPlayerIndex = Players.FindIndex(p =>
            {
                return p.idPlayer == currentTurnPlayerId;
            });
            var nextPlayerIndex = (currentPlayerIndex + 1) % Players.Count;
            currentTurnPlayerId = Players[nextPlayerIndex].idPlayer;
            playerReviewingDiscardId = currentTurnPlayerId;
            isCardDrawnFromDeck = false;
        }

        private void BroadcastTime(int gameSeconds, int turnSeconds, int currentPlayerId)
        {
            foreach (var kvp in playerCallbacks)
            {
                int pid = kvp.Key;
                var cb = kvp.Value;

                Task.Run(() =>
                {
                    try
                    {
                        cb.OnTimeUpdated(gameSeconds, turnSeconds, currentPlayerId);
                    }
                    catch (System.ServiceModel.CommunicationException ex)
                    {
                        Logger.Info(ex, $"Error de comunicación con jugador {pid}. Finalizando partida.");
                        Task.Run(() => ProcessAFK(pid));
                    }
                    catch (TimeoutException ex)
                    {
                        Logger.Info(ex, $"Tiempo de conexión agotado para jugador {pid}. Finalizando partida.");
                        Task.Run(() => ProcessAFK(pid));
                    }
                    catch (ObjectDisposedException ex)
                    {
                        Logger.Info(ex, $"Canal cerrado para jugador {pid}. Finalizando partida.");
                        Task.Run(() => ProcessAFK(pid));
                    }
                    catch (Exception ex)
                    {
                        Logger.Info(ex, $"Tiempo de conexión agotado para jugador {pid}. Finalizando partida.");
                        Task.Run(() => ProcessAFK(pid));
                    }
                });
            }
        }

        public int GetCurrentTurnPlayerId()
        {
            return currentTurnPlayerId;
        }

        public void StopGame()
        {
            if (gameTimer != null)
            {
                gameTimer.Stop();
                gameTimer.Elapsed -= OnTimerTick;
                gameTimer.Dispose();
                Logger.Info($"Game stopped for Room Code: {RoomCode}");
            }
        }

        public void ProcessPlayerMove(int playerId, List<string> cardIds)
        {
            GameValidator.ValidateTurnOwner(playerId, currentTurnPlayerId);
            GameValidator.ValidateActionAllowed(mustDiscardToFinishTurn);
            GameValidator.ValidateMoveInputs(cardIds);

            if (!PlayerHands.TryGetValue(playerId, out List<CardsGame> hand))
            {
                throw new InvalidOperationException(ServiceErrorType.OperationFailed.ToString());
            }

            var moveContext = BuildMoveContext(playerId, cardIds, hand);

            ValidateMeldRules(moveContext);

            ExecuteMeld(playerId, hand, moveContext);

            var notificationContext = new MeldNotificationContext
            {
                PlayerId = playerId,
                HandCount = hand.Count,
                FullMeld = moveContext.FullMeld,
                UsingDiscardCard = moveContext.UsingDiscardCard
            };

            NotifyAndCheckGameStatus(notificationContext);
        }

        private bool CheckWinCondition(int playerId)
        {
            int meldsCount = PlayerMelds[playerId].Count;
            bool playerHasReachedWinningMelds = HasPlayerWon(meldsCount);

            if (playerHasReachedWinningMelds)
            {
                FinishGame(playerId, false);
            }

            return playerHasReachedWinningMelds;
        }

        private bool HasPlayerWon(int meldsCount)
        {
            if (GamemodeId == GAMEMODE_CLASSIC)
            {
                return meldsCount >= MELDS_TO_WIN_CLASSIC;
            }

            return meldsCount >= MELDS_TO_WIN_EXTENDED;
        }

        private void BroadcastDiscardUpdate()
        {
            CardDto topCardDto = null;
            if (DiscardPile.Count > 0)
            {
                var c = DiscardPile[DiscardPile.Count - 1];
                topCardDto = new CardDto 
                { 
                    Id = c.Id,
                    Suit = c.Suit,
                    Rank = c.Rank,
                    ImagePath = c.ImagePath 
                };
            }

            Broadcast((callback) =>
            {
                callback.NotifyOpponentDiscarded(topCardDto);
            });
        }

        public void PassTurn(int playerId)
        {
            if (playerId != currentTurnPlayerId)
            {
                return;
            }

            GameValidator.ValidateActionAllowed(mustDiscardToFinishTurn);

            if (!hasHostPassedInitialDiscard && playerId == Players[0].idPlayer && playerReviewingDiscardId == playerId)
            {
                hasHostPassedInitialDiscard = true;
                ChangeTurn();
                playerReviewingDiscardId = currentTurnPlayerId;
                return;
            }

            if (playerReviewingDiscardId == playerId && !isCardDrawnFromDeck)
            {
                playerReviewingDiscardId = null;
                return;
            }

            if (isCardDrawnFromDeck)
            {
                ChangeTurn();
            }
        }

        public void DrawFromDeck(int playerId)
        {
            try
            {
                var context = new DrawValidationContext
                {
                    PlayerId = playerId,
                    CurrentTurnPlayerId = currentTurnPlayerId,
                    IsCardDrawnFromDeck = isCardDrawnFromDeck,
                    MustDiscardToFinishTurn = mustDiscardToFinishTurn,
                    PlayerReviewingDiscardId = playerReviewingDiscardId,
                    StockCount = StockPile.Count
                };

                GameValidator.ValidateDraw(context);
            }
            catch (BusinessLogicException ex) when (ex.ErrorType == ServiceErrorType.DeckEmpty)
            {
                DetermineWinnerByPoints();
                throw;
            }

            var card = StockPile[0];
            StockPile.RemoveAt(0);
            DiscardPile.Add(card);

            isCardDrawnFromDeck = true;
            playerReviewingDiscardId = playerId;

            var cardDto = new CardDto
            {
                Id = card.Id,
                Suit = card.Suit,
                Rank = card.Rank,
                ImagePath = card.ImagePath
            };

            Broadcast((callback) =>
            {
                callback.NotifyOpponentDiscarded(cardDto);
            });
        }

        public void DiscardCard(int playerId, string cardId)
        {
            var card = PlayerHands[playerId].FirstOrDefault(c =>
            {
                return c.Id == cardId;
            });

            GameValidator.ValidateDiscard(playerId, currentTurnPlayerId, card);

            PlayerHands[playerId].Remove(card);
            DiscardPile.Add(card);

            var cardDto = new CardDto
            {
                Id = card.Id,
                Suit = card.Suit,
                Rank = card.Rank,
                ImagePath = card.ImagePath
            };

            NotifyOpponent(playerId, (callback) =>
            {
                callback.NotifyOpponentDiscarded(cardDto);
                callback.OnOpponentHandUpdated(PlayerHands[playerId].Count);
            });

            ChangeTurn();
        }

        private void DetermineWinnerByPoints()
        {
            var playerIds = Players.Select(p => p.idPlayer).ToList();
            int p1 = playerIds[PLAYER_1_INDEX];
            int p2 = playerIds[PLAYER_2_INDEX];

            int meldsP1 = PlayerMelds[p1].Count;
            int meldsP2 = PlayerMelds[p2].Count;

            if (meldsP1 > meldsP2)
            {
                FinishGame(p1, false);
            }
            else if (meldsP2 > meldsP1)
            {
                FinishGame(p2, false);
            }
            else
            {
                FinishGame(-1, true);
            }
        }

        private void FinishGame(int winnerId, bool isDraw)
        {
            if (!TryMarkGameAsEnded())
            {
                return;
            }

            StopGame();

            var result = BuildGameResult(winnerId, isDraw);
            OnGameFinished?.Invoke(result);

            Logger.Info($"Game {RoomCode} ended. Winner: {winnerId}. Draw: {isDraw}");
        }

        private bool TryMarkGameAsEnded()
        {
            lock (endLock)
            {
                if (isGameEnded)
                {
                    return false;
                }
                isGameEnded = true;
                return true;
            }
        }

        private GameResultDto BuildGameResult(int winnerId, bool isDraw)
        {
            int loserId = GetLoserId(winnerId, isDraw);
            var (player1, player2) = GetPlayers();
            int duration = CalculateGameDuration();

            return new GameResultDto
            {
                WinnerId = winnerId,
                LoserId = loserId,
                IsDraw = isDraw,
                PointsWon = 0,
                RoomCode = this.RoomCode,
                GamemodeId = GamemodeId,

                Player1Id = player1?.idPlayer ?? -1,
                Player1Name = player1?.nickname,
                Player1Score = GetPlayerScore(player1),
                Player1PathPhoto = player1?.pathPhoto,

                Player2Id = player2?.idPlayer ?? -1,
                Player2Name = player2?.nickname,
                Player2Score = GetPlayerScore(player2),
                Player2PathPhoto = player2?.pathPhoto,

                DurationSeconds = duration
            };
        }

        private int GetLoserId(int winnerId, bool isDraw)
        {
            if (isDraw)
            {
                return -1;
            }

            return Players.FirstOrDefault(p => p.idPlayer != winnerId)?.idPlayer ?? -1;
        }

        private (PlayerDto player1, PlayerDto player2) GetPlayers()
        {
            PlayerDto player1 = Players.Count > 0 ? Players[PLAYER_1_INDEX] : null;
            PlayerDto player2 = Players.Count > 1 ? Players[PLAYER_2_INDEX] : null;
            return (player1, player2);
        }

        private int CalculateGameDuration()
        {
            int duration = GetInitialTimeInSeconds() - remainingSeconds;
            if (duration < 0)
            {
                return 0;
            }
            else
            {
                return duration;
            }
        }

        private int GetPlayerScore(PlayerDto player)
        {
            if (player == null || !PlayerMelds.ContainsKey(player.idPlayer))
            {
                return 0;
            }

            return PlayerMelds[player.idPlayer].Count;
        }

        public void BroadcastGameResult(GameResultDto result)
        {
            Broadcast((callback) =>
            {
                callback.NotifyGameEnded(result);
            });
        }

        private void NotifyOpponent(int actingPlayerId, Action<IGameCallback> action)
        {
            int opponentId = playerCallbacks.Keys.FirstOrDefault(id => id != actingPlayerId);

            if (opponentId != 0 && playerCallbacks.TryGetValue(opponentId, out IGameCallback opponentCallback))
            {
                Task.Run(() =>
                {
                    try
                    {
                        action(opponentCallback);
                    }
                    catch (System.ServiceModel.CommunicationException ex)
                    {
                        Logger.Warn(ex, $"Error de comunicación al notificar oponente ID {opponentId}.");
                        Task.Run(() => ProcessAFK(opponentId));
                    }
                    catch (TimeoutException ex)
                    {
                        Logger.Warn(ex, $"Timeout al notificar oponente ID {opponentId}.");
                        Task.Run(() => ProcessAFK(opponentId));
                    }
                    catch (ObjectDisposedException ex)
                    {
                        Logger.Warn(ex, $"Canal cerrado para oponente ID {opponentId}.");
                        Task.Run(() => ProcessAFK(opponentId));
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, $"Failed to notify opponent ID {opponentId} (Background Task).");
                        Task.Run(() => ProcessAFK(opponentId));
                    }
                });
            }
        }

        private void Broadcast(Action<IGameCallback> action)
        {
            Task.Run(() =>
            {
                foreach (var kvp in playerCallbacks)
                {
                    int pid = kvp.Key;
                    try
                    {
                        action(kvp.Value);
                    }
                    catch (System.ServiceModel.CommunicationException ex)
                    {
                        Logger.Warn(ex, $"Error de comunicación en broadcast para jugador {pid}.");
                        Task.Run(() => ProcessAFK(pid));
                    }
                    catch (TimeoutException ex)
                    {
                        Logger.Warn(ex, $"Timeout en broadcast para jugador {pid}.");
                        Task.Run(() => ProcessAFK(pid));
                    }
                    catch (ObjectDisposedException ex)
                    {
                        Logger.Warn(ex, $"Canal cerrado en broadcast para jugador {pid}.");
                        Task.Run(() => ProcessAFK(pid));
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex,$"Broadcast falló para {pid}.");
                        Task.Run(() => ProcessAFK(pid));
                    }
                }
            });
        }

        public void NotifyGameEndedByAbandonment(int leavingPlayerId)
        {
            StopGame();

            int opponentId = playerCallbacks.Keys.FirstOrDefault(id => id != leavingPlayerId);

            if (opponentId != 0 && playerCallbacks.TryGetValue(opponentId, out IGameCallback callback))
            {
                Task.Run(() =>
                {
                    try
                    {
                        callback.OnOpponentLeft();
                    }
                    catch (System.ServiceModel.CommunicationException ex)
                    {
                        Logger.Error(ex, $"Error de comunicación al notificar salida en Room {RoomCode}");
                    }
                    catch (TimeoutException ex)
                    {
                        Logger.Error(ex, $"Timeout al notificar salida en Room {RoomCode}");
                    }
                    catch (ObjectDisposedException ex)
                    {
                        Logger.Error(ex, $"Canal cerrado al notificar salida en Room {RoomCode}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, $"Error notifying room exit {RoomCode}");
                    }
                });
            }
        }

        public void SwapDrawnCard(int playerId, string cardIdToDiscard)
        {
            if (PlayerHands.TryGetValue(playerId, out List<CardsGame> hand))
            {
                var cardToDiscard = hand.FirstOrDefault(c => c.Id == cardIdToDiscard);

                var context = new SwapValidationContext
                {
                    PlayerId = playerId,
                    CurrentTurnPlayerId = currentTurnPlayerId,
                    IsCardDrawnFromDeck = isCardDrawnFromDeck,
                    PlayerReviewingDiscardId = playerReviewingDiscardId,
                    MustDiscardToFinishTurn = mustDiscardToFinishTurn,
                    CardToDiscard = cardToDiscard,
                    DiscardPileCount = DiscardPile.Count
                };

                GameValidator.ValidateSwap(context);

                var cardToTake = DiscardPile[DiscardPile.Count - 1];

                hand.Add(cardToTake);
                DiscardPile.Remove(cardToTake);

                hand.Remove(cardToDiscard);
                DiscardPile.Add(cardToDiscard);

                var cardDto = new CardDto
                {
                    Id = cardToDiscard.Id,
                    Suit = cardToDiscard.Suit,
                    Rank = cardToDiscard.Rank,
                    ImagePath = cardToDiscard.ImagePath
                };

                NotifyOpponent(playerId, (callback) =>
                {
                    callback.NotifyOpponentDiscarded(cardDto);
                    callback.OnOpponentHandUpdated(hand.Count);
                });

                ChangeTurn();
            }
            else
            {
                throw new InvalidOperationException(ServiceErrorType.OperationFailed.ToString());
            }
        }

        private sealed class MoveContext
        {
            public List<CardsGame> CardsFromHand { get; set; }
            public List<CardsGame> FullMeld { get; set; }
            public CardsGame DiscardCard { get; set; }
            public bool UsingDiscardCard { get; set; }
            public List<string> HandCardIds { get; set; }
        }

        private MoveContext BuildMoveContext(int playerId, List<string> cardIds, List<CardsGame> hand)
        {
            var context = new MoveContext
            {
                UsingDiscardCard = false,
                DiscardCard = null
            };

            if (DiscardPile.Count > 0)
            {
                var topDiscard = DiscardPile[DiscardPile.Count - 1];
                if (cardIds.Contains(topDiscard.Id))
                {
                    GameValidator.ValidateDiscardUsage(playerId, playerReviewingDiscardId, isCardDrawnFromDeck);
                    context.UsingDiscardCard = true;
                    context.DiscardCard = topDiscard;
                }
            }

            if (context.UsingDiscardCard)
            {
                context.HandCardIds = cardIds.Where(id => id != context.DiscardCard.Id).ToList();
            }
            else
            {
                context.HandCardIds = cardIds;
            }

            context.CardsFromHand = hand.Where(card => context.HandCardIds.Contains(card.Id)).ToList();

            context.FullMeld = new List<CardsGame>(context.CardsFromHand);
            if (context.UsingDiscardCard)
            {
                context.FullMeld.Add(context.DiscardCard);
            }

            return context;
        }

        private static void ValidateMeldRules(MoveContext context)
        {
            GameValidator.ValidateCardsInHand(context.CardsFromHand.Count, context.HandCardIds.Count);

            GameValidator.ValidateMeldSize(context.FullMeld.Count);

            if (!GameValidator.IsValidMeldCombination(context.FullMeld))
            {
                throw new BusinessLogicException(ServiceErrorType.InvalidMeld);
            }
        }

        private void ExecuteMeld(int playerId, List<CardsGame> hand, MoveContext context)
        {
            hand.RemoveAll(card => context.HandCardIds.Contains(card.Id));

            if (context.UsingDiscardCard)
            {
                DiscardPile.RemoveAt(DiscardPile.Count - 1);
                playerReviewingDiscardId = null;
            }

            PlayerMelds[playerId].Add(context.FullMeld);
        }

        private void NotifyAndCheckGameStatus(MeldNotificationContext context)
        {
            var cardDtos = context.FullMeld.Select(c => new CardDto
            {
                Id = c.Id,
                Suit = c.Suit,
                Rank = c.Rank,
                ImagePath = c.ImagePath
            }).ToArray();

            NotifyOpponent(context.PlayerId, (callback) =>
            {
                callback.NotifyOpponentMeld(cardDtos);
                callback.OnOpponentHandUpdated(context.HandCount);
            });

            bool gameEnded = CheckWinCondition(context.PlayerId);

            if (context.UsingDiscardCard)
            {
                BroadcastDiscardUpdate();

                if (!gameEnded)
                {
                    mustDiscardToFinishTurn = true;
                }
            }
        }

        public void ProcessAFK(int afkPlayerId)
        {
            if (!TryMarkGameAsEnded())
            {
                return;
            }

            StopGame();
            Logger.Info($"Game ended in Room {RoomCode} due to inactivity of Player {afkPlayerId}.");

            NotifyPlayerAFK(afkPlayerId, AFK_REASON_SELF);
            NotifyRivalAFK(afkPlayerId);
        }

        private void NotifyPlayerAFK(int playerId, string reason)
        {
            if (!playerCallbacks.TryGetValue(playerId, out var callback))
            {
                return;
            }

            SafeNotifyAsync(() => callback.NotifyGameEndedByAFK(reason),
                $"Error al notificar AFK al jugador {playerId}");
        }

        private void NotifyRivalAFK(int afkPlayerId)
        {
            int rivalId = Players.FirstOrDefault(p => p.idPlayer != afkPlayerId)?.idPlayer ?? 0;

            if (rivalId == 0)
            {
                return;
            }

            NotifyPlayerAFK(rivalId, AFK_REASON_RIVAL);
        }

        private void SafeNotifyAsync(Action notifyAction, string errorContext)
        {
            Task.Run(() =>
            {
                try
                {
                    notifyAction();
                }
                catch (System.ServiceModel.CommunicationException ex)
                {
                    Logger.Error(ex, $"{errorContext} - Error de comunicación.");
                }
                catch (TimeoutException ex)
                {
                    Logger.Error(ex, $"{errorContext} - Timeout.");
                }
                catch (ObjectDisposedException ex)
                {
                    Logger.Error(ex, $"{errorContext} - Canal cerrado.");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"{errorContext} - Error inesperado.");
                }
            });
        }

        private sealed class MeldNotificationContext
        {
            public int PlayerId { get; set; }
            public int HandCount { get; set; }
            public List<CardsGame> FullMeld { get; set; }
            public bool UsingDiscardCard { get; set; }
        }
    }
}
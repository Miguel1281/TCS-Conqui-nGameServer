using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.BusinessLogic.Validation;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
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

        private const int NO_PLAYER_ID = -1;
        private const int PLAYER_NOT_FOUND = 0;
        private const int DEFAULT_SCORE = 0;
        private const int MINIMUM_DURATION = 0;
        private const int DEFAULT_POINTS_WON = 0;
        private const int LAST_ELEMENT_INDEX = 1;

        private const string AFK_REASON_SELF = "AFKGameEndedSelf";
        private const string AFK_REASON_RIVAL = "AFKGameEndedRival";

        private static readonly string[] deckSuits = { "Oros", "Copas", "Espadas", "Bastos" };
        private static readonly int[] deckRanks = { 1, 2, 3, 4, 5, 6, 7, 10, 11, 12 };

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

            currentTurnPlayerId = Players[PLAYER_1_INDEX].idPlayer;
            playerReviewingDiscardId = currentTurnPlayerId;

            hasHostPassedInitialDiscard = false;
            isCardDrawnFromDeck = false;
            mustDiscardToFinishTurn = false;

            InitializePlayerCollections(players);
            InitializeGame();

            Logger.Info($"Game initialized for Room Code: {RoomCode}. Gamemode: {GamemodeId}");
        }

        private void InitializePlayerCollections(List<PlayerDto> players)
        {
            PlayerHands = new Dictionary<int, List<CardsGame>>();
            PlayerMelds = new Dictionary<int, List<List<CardsGame>>>();
            PlayerHands = players.ToDictionary(player => player.idPlayer, player => new List<CardsGame>());
            PlayerMelds = players.ToDictionary(player => player.idPlayer, player => new List<List<CardsGame>>());
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

            foreach (var suit in deckSuits)
            {
                foreach (var rank in deckRanks)
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
                int randomValue = generatedValue % max;
                return randomValue;
            }
        }

        private void ShuffleDeck()
        {
            int n = Deck.Count;

            while (n > 1)
            {
                n--;
                int k = GetSecureRandomInt(n + 1);
                SwapDeckCards(k, n);
            }
        }

        private void SwapDeckCards(int firstIndex, int secondIndex)
        {
            CardsGame temporaryCard = Deck[firstIndex];
            Deck[firstIndex] = Deck[secondIndex];
            Deck[secondIndex] = temporaryCard;
        }

        private void DealHands()
        {
            int cardsToDeal = GetCardsToDeadByGameMode();

            for (int i = 0; i < cardsToDeal; i++)
            {
                DealCardToEachPlayer();
            }
        }

        private int GetCardsToDeadByGameMode()
        {
            int cardsToDeal;
            bool isClassicMode = (GamemodeId == GAMEMODE_CLASSIC);

            if (isClassicMode)
            {
                cardsToDeal = HAND_SIZE_CLASSIC;
            }
            else
            {
                cardsToDeal = HAND_SIZE_EXTENDED;
            }

            return cardsToDeal;
        }

        private void DealCardToEachPlayer()
        {
            foreach (var player in Players)
            {
                var card = Deck[PLAYER_1_INDEX];
                Deck.RemoveAt(PLAYER_1_INDEX);
                PlayerHands[player.idPlayer].Add(card);
            }
        }

        private void SetupPiles()
        {
            StockPile = Deck;
            DiscardPile = new List<CardsGame>();

            var firstDiscard = StockPile[PLAYER_1_INDEX];
            StockPile.RemoveAt(PLAYER_1_INDEX);
            DiscardPile.Add(firstDiscard);
        }

        public void RegisterPlayerCallback(int playerId, IGameCallback callback)
        {
            playerCallbacks[playerId] = callback;
            Logger.Info($"Player ID {playerId} registered callback for Room Code: {RoomCode}");
        }

        public int GetInitialTimeInSeconds()
        {
            int timeInSeconds;
            bool isClassicMode = (GamemodeId == GAMEMODE_CLASSIC);

            if (isClassicMode)
            {
                timeInSeconds = TIME_LIMIT_SHORT_SECONDS;
            }
            else
            {
                timeInSeconds = TIME_LIMIT_LONG_SECONDS;
            }

            return timeInSeconds;
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
            bool timeExpired = remainingSeconds <= MINIMUM_DURATION;

            if (timeExpired)
            {
                Logger.Info($"Game timeout reached for Room Code: {RoomCode}. Stopping game.");
                StopGame();
                DetermineWinnerByPoints();
            }

            BroadcastTime(remainingSeconds, MINIMUM_DURATION, currentTurnPlayerId);
        }

        private void ChangeTurn()
        {
            hasHostPassedInitialDiscard = true;
            mustDiscardToFinishTurn = false;

            var currentPlayerIndex = FindPlayerIndex(currentTurnPlayerId);
            var nextPlayerIndex = CalculateNextPlayerIndex(currentPlayerIndex);

            currentTurnPlayerId = Players[nextPlayerIndex].idPlayer;
            playerReviewingDiscardId = currentTurnPlayerId;
            isCardDrawnFromDeck = false;
        }

        private int FindPlayerIndex(int playerId)
        {
            int playerIndex = Players.FindIndex(p => p.idPlayer == playerId);
            return playerIndex;
        }

        private int CalculateNextPlayerIndex(int currentIndex)
        {
            int nextIndex = (currentIndex + 1) % Players.Count;
            return nextIndex;
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
            bool timerExists = (gameTimer != null);

            if (timerExists)
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

            bool handExists = PlayerHands.TryGetValue(playerId, out List<CardsGame> hand);

            if (!handExists)
            {
                throw new InvalidOperationException(ServiceErrorType.OperationFailed.ToString());
            }

            var moveContext = BuildMoveContext(playerId, cardIds, hand);
            ValidateMeldRules(moveContext);
            ExecuteMeld(playerId, hand, moveContext);

            var notificationContext = CreateMeldNotificationContext(playerId, hand, moveContext);
            NotifyAndCheckGameStatus(notificationContext);
        }

        private MeldNotificationContext CreateMeldNotificationContext(
            int playerId,
            List<CardsGame> hand,
            MoveContext moveContext)
        {
            var notificationContext = new MeldNotificationContext
            {
                PlayerId = playerId,
                HandCount = hand.Count,
                FullMeld = moveContext.FullMeld,
                UsingDiscardCard = moveContext.UsingDiscardCard
            };

            return notificationContext;
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
            bool playerWon;
            bool isClassicMode = (GamemodeId == GAMEMODE_CLASSIC);

            if (isClassicMode)
            {
                playerWon = (meldsCount >= MELDS_TO_WIN_CLASSIC);
            }
            else
            {
                playerWon = (meldsCount >= MELDS_TO_WIN_EXTENDED);
            }

            return playerWon;
        }

        private void BroadcastDiscardUpdate()
        {
            CardDto topCardDto = GetTopDiscardCardDto();

            Broadcast((callback) =>
            {
                callback.NotifyOpponentDiscarded(topCardDto);
            });
        }

        private CardDto GetTopDiscardCardDto()
        {
            CardDto topCardDto = null;
            bool discardPileHasCards = (DiscardPile.Count > PLAYER_NOT_FOUND);

            if (discardPileHasCards)
            {
                var lastCardIndex = DiscardPile.Count - LAST_ELEMENT_INDEX;
                var topCard = DiscardPile[lastCardIndex];
                topCardDto = ConvertToCardDto(topCard);
            }

            return topCardDto;
        }

        private CardDto ConvertToCardDto(CardsGame card)
        {
            CardDto cardDto = new CardDto
            {
                Id = card.Id,
                Suit = card.Suit,
                Rank = card.Rank,
                ImagePath = card.ImagePath
            };

            return cardDto;
        }

        public void PassTurn(int playerId)
        {
            bool isNotPlayerTurn = (playerId != currentTurnPlayerId);

            if (isNotPlayerTurn)
            {
                return;
            }

            GameValidator.ValidateActionAllowed(mustDiscardToFinishTurn);

            bool isInitialHostDiscard = IsInitialHostDiscard(playerId);

            if (isInitialHostDiscard)
            {
                HandleInitialHostDiscard();
                return;
            }

            bool isPlayerReviewingAndNotDrawn = IsPlayerReviewingWithoutDraw(playerId);

            if (isPlayerReviewingAndNotDrawn)
            {
                playerReviewingDiscardId = null;
                return;
            }

            if (isCardDrawnFromDeck)
            {
                ChangeTurn();
            }
        }

        private bool IsInitialHostDiscard(int playerId)
        {
            bool isFirstPlayer = (playerId == Players[PLAYER_1_INDEX].idPlayer);
            bool isReviewingPlayer = (playerReviewingDiscardId == playerId);
            bool notPassedYet = !hasHostPassedInitialDiscard;

            bool isInitialDiscard = (notPassedYet && isFirstPlayer && isReviewingPlayer);
            return isInitialDiscard;
        }

        private void HandleInitialHostDiscard()
        {
            hasHostPassedInitialDiscard = true;
            ChangeTurn();
            playerReviewingDiscardId = currentTurnPlayerId;
        }

        private bool IsPlayerReviewingWithoutDraw(int playerId)
        {
            bool isReviewingPlayer = (playerReviewingDiscardId == playerId);
            bool hasNotDrawn = !isCardDrawnFromDeck;

            bool isReviewingWithoutDraw = (isReviewingPlayer && hasNotDrawn);
            return isReviewingWithoutDraw;
        }

        public void DrawFromDeck(int playerId)
        {
            try
            {
                var context = CreateDrawValidationContext(playerId);
                GameValidator.ValidateDraw(context);
            }
            catch (BusinessLogicException ex) when (ex.ErrorType == ServiceErrorType.DeckEmpty)
            {
                DetermineWinnerByPoints();
                throw;
            }

            DrawAndDiscardCard();
            UpdateDrawState(playerId);
            BroadcastDiscardUpdate();
        }

        private DrawValidationContext CreateDrawValidationContext(int playerId)
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

            return context;
        }

        private void DrawAndDiscardCard()
        {
            var card = StockPile[PLAYER_1_INDEX];
            StockPile.RemoveAt(PLAYER_1_INDEX);
            DiscardPile.Add(card);
        }

        private void UpdateDrawState(int playerId)
        {
            isCardDrawnFromDeck = true;
            playerReviewingDiscardId = playerId;
        }

        public void DiscardCard(int playerId, string cardId)
        {
            var card = FindCardInPlayerHand(playerId, cardId);
            GameValidator.ValidateDiscard(playerId, currentTurnPlayerId, card);

            RemoveCardFromHand(playerId, card);
            AddCardToDiscardPile(card);

            var cardDto = ConvertToCardDto(card);
            NotifyOpponentOfDiscard(playerId, cardDto);

            ChangeTurn();
        }

        private CardsGame FindCardInPlayerHand(int playerId, string cardId)
        {
            var card = PlayerHands[playerId].FirstOrDefault(c => c.Id == cardId);
            return card;
        }

        private void RemoveCardFromHand(int playerId, CardsGame card)
        {
            PlayerHands[playerId].Remove(card);
        }

        private void AddCardToDiscardPile(CardsGame card)
        {
            DiscardPile.Add(card);
        }

        private void NotifyOpponentOfDiscard(int playerId, CardDto cardDto)
        {
            NotifyOpponent(playerId, (callback) =>
            {
                callback.NotifyOpponentDiscarded(cardDto);
                callback.OnOpponentHandUpdated(PlayerHands[playerId].Count);
            });
        }

        private void DetermineWinnerByPoints()
        {
            var playerIds = Players.Select(p => p.idPlayer).ToList();
            int player1Id = playerIds[PLAYER_1_INDEX];
            int player2Id = playerIds[PLAYER_2_INDEX];

            int meldsPlayer1 = PlayerMelds[player1Id].Count;
            int meldsPlayer2 = PlayerMelds[player2Id].Count;

            bool player1HasMoreMelds = (meldsPlayer1 > meldsPlayer2);
            bool player2HasMoreMelds = (meldsPlayer2 > meldsPlayer1);

            if (player1HasMoreMelds)
            {
                FinishGame(player1Id, false);
            }
            else if (player2HasMoreMelds)
            {
                FinishGame(player2Id, false);
            }
            else
            {
                FinishGame(NO_PLAYER_ID, true);
            }
        }

        private void FinishGame(int winnerId, bool isDraw)
        {
            bool gameCanEnd = TryMarkGameAsEnded();

            if (!gameCanEnd)
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
            bool successfullyMarked;

            lock (endLock)
            {
                bool alreadyEnded = isGameEnded;

                if (alreadyEnded)
                {
                    successfullyMarked = false;
                }
                else
                {
                    isGameEnded = true;
                    successfullyMarked = true;
                }
            }

            return successfullyMarked;
        }

        private GameResultDto BuildGameResult(int winnerId, bool isDraw)
        {
            int loserId = GetLoserId(winnerId, isDraw);
            var (player1, player2) = GetPlayers();
            int duration = CalculateGameDuration();

            var result = new GameResultDto
            {
                WinnerId = winnerId,
                LoserId = loserId,
                IsDraw = isDraw,
                PointsWon = DEFAULT_POINTS_WON,
                RoomCode = this.RoomCode,
                GamemodeId = GamemodeId,

                Player1Id = GetPlayerIdOrDefault(player1),
                Player1Name = player1?.nickname,
                Player1Score = GetPlayerScore(player1),
                Player1PathPhoto = player1?.pathPhoto,

                Player2Id = GetPlayerIdOrDefault(player2),
                Player2Name = player2?.nickname,
                Player2Score = GetPlayerScore(player2),
                Player2PathPhoto = player2?.pathPhoto,

                DurationSeconds = duration
            };

            return result;
        }

        private int GetPlayerIdOrDefault(PlayerDto player)
        {
            int playerId;
            bool playerExists = (player != null);

            if (playerExists)
            {
                playerId = player.idPlayer;
            }
            else
            {
                playerId = NO_PLAYER_ID;
            }

            return playerId;
        }

        private int GetLoserId(int winnerId, bool isDraw)
        {
            int loserId;

            if (isDraw)
            {
                loserId = NO_PLAYER_ID;
            }
            else
            {
                var loserPlayer = Players.FirstOrDefault(p => p.idPlayer != winnerId);
                loserId = loserPlayer?.idPlayer ?? NO_PLAYER_ID;
            }

            return loserId;
        }

        private (PlayerDto player1, PlayerDto player2) GetPlayers()
        {
            PlayerDto player1 = GetPlayerAtIndex(PLAYER_1_INDEX);
            PlayerDto player2 = GetPlayerAtIndex(PLAYER_2_INDEX);

            return (player1, player2);
        }

        private PlayerDto GetPlayerAtIndex(int index)
        {
            PlayerDto player;
            bool indexIsValid = (Players.Count > index);

            if (indexIsValid)
            {
                player = Players[index];
            }
            else
            {
                player = null;
            }

            return player;
        }

        private int CalculateGameDuration()
        {
            int initialTime = GetInitialTimeInSeconds();
            int elapsedTime = initialTime - remainingSeconds;
            bool durationIsNegative = (elapsedTime < MINIMUM_DURATION);

            int duration;

            if (durationIsNegative)
            {
                duration = MINIMUM_DURATION;
            }
            else
            {
                duration = elapsedTime;
            }

            return duration;
        }

        private int GetPlayerScore(PlayerDto player)
        {
            int score;
            bool playerIsNull = (player == null);
            bool playerHasNoMelds = !PlayerMelds.ContainsKey(player?.idPlayer ?? NO_PLAYER_ID);

            if (playerIsNull || playerHasNoMelds)
            {
                score = DEFAULT_SCORE;
            }
            else
            {
                score = PlayerMelds[player.idPlayer].Count;
            }

            return score;
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
            int opponentId = FindOpponentId(actingPlayerId);
            bool opponentFound = (opponentId != PLAYER_NOT_FOUND);

            if (opponentFound && playerCallbacks.TryGetValue(opponentId, out IGameCallback opponentCallback))
            {
                ExecuteNotificationAsync(opponentId, opponentCallback, action);
            }
        }

        private int FindOpponentId(int actingPlayerId)
        {
            int opponentId = playerCallbacks.Keys.FirstOrDefault(id => id != actingPlayerId);
            return opponentId;
        }

        private void ExecuteNotificationAsync(
            int playerId,
            IGameCallback callback,
            Action<IGameCallback> action)
        {
            Task.Run(() =>
            {
                try
                {
                    action(callback);
                }
                catch (System.ServiceModel.CommunicationException ex)
                {
                    Logger.Warn(ex, $"Error de comunicación al notificar oponente ID {playerId}.");
                    Task.Run(() => ProcessAFK(playerId));
                }
                catch (TimeoutException ex)
                {
                    Logger.Warn(ex, $"Timeout al notificar oponente ID {playerId}.");
                    Task.Run(() => ProcessAFK(playerId));
                }
                catch (ObjectDisposedException ex)
                {
                    Logger.Warn(ex, $"Canal cerrado para oponente ID {playerId}.");
                    Task.Run(() => ProcessAFK(playerId));
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, $"Failed to notify opponent ID {playerId} (Background Task).");
                    Task.Run(() => ProcessAFK(playerId));
                }
            });
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
                        Logger.Warn(ex, $"Broadcast falló para {pid}.");
                        Task.Run(() => ProcessAFK(pid));
                    }
                }
            });
        }

        public void NotifyGameEndedByAbandonment(int leavingPlayerId)
        {
            StopGame();

            int opponentId = FindOpponentId(leavingPlayerId);
            bool opponentFound = (opponentId != PLAYER_NOT_FOUND);

            if (opponentFound && playerCallbacks.TryGetValue(opponentId, out IGameCallback callback))
            {
                NotifyOpponentLeftAsync(callback);
            }
        }

        private void NotifyOpponentLeftAsync(IGameCallback callback)
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

        public void SwapDrawnCard(int playerId, string cardIdToDiscard)
        {
            bool handExists = PlayerHands.TryGetValue(playerId, out List<CardsGame> hand);

            if (!handExists)
            {
                throw new InvalidOperationException(ServiceErrorType.OperationFailed.ToString());
            }

            var cardToDiscard = FindCardInPlayerHand(playerId, cardIdToDiscard);
            var context = CreateSwapValidationContext(playerId, cardToDiscard);

            GameValidator.ValidateSwap(context);

            ExecuteCardSwap(hand, cardToDiscard);

            var cardDto = ConvertToCardDto(cardToDiscard);
            NotifyOpponentOfSwap(playerId, hand, cardDto);

            ChangeTurn();
        }

        private SwapValidationContext CreateSwapValidationContext(int playerId, CardsGame cardToDiscard)
        {
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

            return context;
        }

        private void ExecuteCardSwap(List<CardsGame> hand, CardsGame cardToDiscard)
        {
            int lastDiscardIndex = DiscardPile.Count - LAST_ELEMENT_INDEX;
            var cardToTake = DiscardPile[lastDiscardIndex];

            hand.Add(cardToTake);
            DiscardPile.Remove(cardToTake);

            hand.Remove(cardToDiscard);
            DiscardPile.Add(cardToDiscard);
        }

        private void NotifyOpponentOfSwap(int playerId, List<CardsGame> hand, CardDto cardDto)
        {
            NotifyOpponent(playerId, (callback) =>
            {
                callback.NotifyOpponentDiscarded(cardDto);
                callback.OnOpponentHandUpdated(hand.Count);
            });
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

            CheckAndSetDiscardCardUsage(context, cardIds, playerId);
            SetHandCardIds(context, cardIds);
            SetCardsFromHand(context, hand);
            BuildFullMeld(context);

            return context;
        }

        private void CheckAndSetDiscardCardUsage(MoveContext context, List<string> cardIds, int playerId)
        {
            bool discardPileHasCards = (DiscardPile.Count > PLAYER_NOT_FOUND);

            if (discardPileHasCards)
            {
                int lastDiscardIndex = DiscardPile.Count - LAST_ELEMENT_INDEX;
                var topDiscard = DiscardPile[lastDiscardIndex];
                bool usingTopDiscard = cardIds.Contains(topDiscard.Id);

                if (usingTopDiscard)
                {
                    GameValidator.ValidateDiscardUsage(playerId, playerReviewingDiscardId, isCardDrawnFromDeck);
                    context.UsingDiscardCard = true;
                    context.DiscardCard = topDiscard;
                }
            }
        }

        private void SetHandCardIds(MoveContext context, List<string> cardIds)
        {
            if (context.UsingDiscardCard)
            {
                context.HandCardIds = cardIds.Where(id => id != context.DiscardCard.Id).ToList();
            }
            else
            {
                context.HandCardIds = cardIds;
            }
        }

        private void SetCardsFromHand(MoveContext context, List<CardsGame> hand)
        {
            context.CardsFromHand = hand.Where(card => context.HandCardIds.Contains(card.Id)).ToList();
        }

        private void BuildFullMeld(MoveContext context)
        {
            context.FullMeld = new List<CardsGame>(context.CardsFromHand);

            if (context.UsingDiscardCard)
            {
                context.FullMeld.Add(context.DiscardCard);
            }
        }

        private static void ValidateMeldRules(MoveContext context)
        {
            GameValidator.ValidateCardsInHand(context.CardsFromHand.Count, context.HandCardIds.Count);
            GameValidator.ValidateMeldSize(context.FullMeld.Count);

            bool isValidMeld = GameValidator.IsValidMeldCombination(context.FullMeld);

            if (!isValidMeld)
            {
                throw new BusinessLogicException(ServiceErrorType.InvalidMeld);
            }
        }

        private void ExecuteMeld(int playerId, List<CardsGame> hand, MoveContext context)
        {
            RemoveCardsFromHand(hand, context);
            RemoveDiscardCardIfUsed(context);
            AddMeldToPlayer(playerId, context);
        }

        private void RemoveCardsFromHand(List<CardsGame> hand, MoveContext context)
        {
            hand.RemoveAll(card => context.HandCardIds.Contains(card.Id));
        }

        private void RemoveDiscardCardIfUsed(MoveContext context)
        {
            if (context.UsingDiscardCard)
            {
                int lastDiscardIndex = DiscardPile.Count - LAST_ELEMENT_INDEX;
                DiscardPile.RemoveAt(lastDiscardIndex);
                playerReviewingDiscardId = null;
            }
        }

        private void AddMeldToPlayer(int playerId, MoveContext context)
        {
            PlayerMelds[playerId].Add(context.FullMeld);
        }

        private void NotifyAndCheckGameStatus(MeldNotificationContext context)
        {
            var cardDtos = ConvertMeldToCardDtos(context.FullMeld);

            NotifyOpponentOfMeld(context.PlayerId, context.HandCount, cardDtos);

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

        private CardDto[] ConvertMeldToCardDtos(List<CardsGame> meld)
        {
            var cardDtos = meld.Select(c => ConvertToCardDto(c)).ToArray();
            return cardDtos;
        }

        private void NotifyOpponentOfMeld(int playerId, int handCount, CardDto[] cardDtos)
        {
            NotifyOpponent(playerId, (callback) =>
            {
                callback.NotifyOpponentMeld(cardDtos);
                callback.OnOpponentHandUpdated(handCount);
            });
        }

        public void ProcessAFK(int afkPlayerId)
        {
            bool gameCanEnd = TryMarkGameAsEnded();

            if (!gameCanEnd)
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
            bool callbackExists = playerCallbacks.TryGetValue(playerId, out var callback);

            if (!callbackExists)
            {
                return;
            }

            SafeNotifyAsync(
                () => callback.NotifyGameEndedByAFK(reason),
                $"Error al notificar AFK al jugador {playerId}"
            );
        }

        private void NotifyRivalAFK(int afkPlayerId)
        {
            var rivalPlayer = Players.FirstOrDefault(p => p.idPlayer != afkPlayerId);
            int rivalId = rivalPlayer?.idPlayer ?? PLAYER_NOT_FOUND;
            bool rivalExists = (rivalId != PLAYER_NOT_FOUND);

            if (!rivalExists)
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
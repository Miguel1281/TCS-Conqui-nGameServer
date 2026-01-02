using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.BusinessLogic.Validation;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace ConquiánServidor.BusinessLogic.Game
{
    public class ConquianGame
    {
        private const int GAMEMODE_CLASSIC = 1;

        private const int TIMER_INTERVAL_MS = 1000; 
        private const int TIME_LIMIT_SHORT_SECONDS = 600; 
        private const int TIME_LIMIT_LONG_SECONDS = 1200;

        private const int HAND_SIZE_CLASSIC = 6;
        private const int HAND_SIZE_EXTENDED = 8;

        private const int MELDS_TO_WIN_CLASSIC = 2;
        private const int MELDS_TO_WIN_EXTENDED = 3;
        private const int POINTS_FOR_WIN = 25;
        private const int POINTS_FOR_DRAW = 0;

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

        private readonly ConcurrentDictionary<int, IGameCallback> playerCallbacks;
        public List<PlayerDto> Players { get; private set; }
        private List<Card> Deck { get; set; }
        public Dictionary<int, List<Card>> PlayerHands { get; private set; }
        public List<Card> StockPile { get; private set; }
        public List<Card> DiscardPile { get; private set; }
        public Dictionary<int, List<List<Card>>> PlayerMelds { get; private set; }

        public event Action<GameResultDto> OnGameFinished;

        private static readonly Random Rng = new Random();

        public ConquianGame(string roomCode, int gamemodeId, List<PlayerDto> players)
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

            PlayerHands = new Dictionary<int, List<Card>>();
            PlayerMelds = new Dictionary<int, List<List<Card>>>();
            PlayerHands = players.ToDictionary(player => player.idPlayer, player => new List<Card>());
            PlayerMelds = players.ToDictionary(player => player.idPlayer, player => new List<List<Card>>());

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
            Deck = new List<Card>();

            foreach (var suit in DECK_SUITS)
            {
                foreach (var rank in DECK_RANKS)
                {
                    Deck.Add(new Card(suit, rank));
                }
            }
        }

        private void ShuffleDeck()
        {
            int n = Deck.Count;
            while (n > 1)
            {
                n--;
                int k = Rng.Next(n + 1);
                Card value = Deck[k];
                Deck[k] = Deck[n];
                Deck[n] = value;
            }
        }

        private void DealHands()
        {
            int cardsToDeal = (GamemodeId == GAMEMODE_CLASSIC) ? HAND_SIZE_CLASSIC : HAND_SIZE_EXTENDED;

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
            DiscardPile = new List<Card>();
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
            return (GamemodeId == GAMEMODE_CLASSIC) ? TIME_LIMIT_SHORT_SECONDS : TIME_LIMIT_LONG_SECONDS;
        }

        public void StartGameTimer()
        {
            remainingSeconds = GetInitialTimeInSeconds();
            gameTimer = new Timer(TIMER_INTERVAL_MS);
            gameTimer.Elapsed += OnTimerTick;
            gameTimer.AutoReset = true;
            gameTimer.Start();
            Logger.Info($"Game timer started for Room Code: {RoomCode}");
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
            var currentPlayerIndex = Players.FindIndex(p => p.idPlayer == currentTurnPlayerId);
            var nextPlayerIndex = (currentPlayerIndex + 1) % Players.Count;
            currentTurnPlayerId = Players[nextPlayerIndex].idPlayer;
            playerReviewingDiscardId = currentTurnPlayerId;
            isCardDrawnFromDeck = false;
        }

        private void BroadcastTime(int gameSeconds, int turnSeconds, int currentPlayerId)
        {
            foreach (var kvp in playerCallbacks)
            {
                try
                {
                    kvp.Value.OnTimeUpdated(gameSeconds, turnSeconds, currentPlayerId);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Failed to broadcast time update to Player ID {kvp.Key} in Room Code: {RoomCode}. Removing callback.");
                    playerCallbacks.TryRemove(kvp.Key, out _);
                }
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

            if (!PlayerHands.TryGetValue(playerId, out List<Card> hand))
            {
                throw new InvalidOperationException(ServiceErrorType.OperationFailed.ToString());
            }

            var moveContext = BuildMoveContext(playerId, cardIds, hand);

            ValidateMeldRules(moveContext);

            ExecuteMeld(playerId, hand, moveContext);

            NotifyAndCheckGameStatus(playerId, hand.Count, moveContext.FullMeld, moveContext.UsingDiscardCard);
        }

        private bool CheckWinCondition(int playerId)
        {
            int meldsCount = PlayerMelds[playerId].Count;

            if (HasPlayerWon(meldsCount))
            {
                FinishGame(playerId, false);
                return true;
            }
            return false;
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
                topCardDto = new CardDto { Id = c.Id, Suit = c.Suit, Rank = c.Rank, ImagePath = c.ImagePath };
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
                Logger.Info($"Host passed initial discard. Now Rival (ID {currentTurnPlayerId}) reviews it.");
                return;
            }

            if (playerReviewingDiscardId == playerId && !isCardDrawnFromDeck)
            {
                if (hasHostPassedInitialDiscard)
                {
                    playerReviewingDiscardId = null; 
                    return;
                }

                playerReviewingDiscardId = null;
                return;
            }

            if (isCardDrawnFromDeck)
            {
                Logger.Info($"Player {playerId} passed on drawn card. Turn ends.");
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

            Logger.Info($"Player ID {playerId} drew from deck in Room {RoomCode}");
        }

        public void DiscardCard(int playerId, string cardId)
        {
            var card = PlayerHands[playerId].FirstOrDefault(c => c.Id == cardId);

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

            Logger.Info($"Player ID {playerId} discarded a card in Room {RoomCode}");

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
            StopGame();

            int loserId = -1;
            if (!isDraw)
            {
                loserId = Players.FirstOrDefault(p => p.idPlayer != winnerId)?.idPlayer ?? -1;
            }

            var p1 = Players.Count > 0 ? Players[PLAYER_1_INDEX] : null;
            var p2 = Players.Count > 1 ? Players[PLAYER_2_INDEX] : null;

            int duration = GetInitialTimeInSeconds() - remainingSeconds;
            if (duration < 0) duration = 0;

            int p1Score = (p1 != null && PlayerMelds.ContainsKey(p1.idPlayer)) ? PlayerMelds[p1.idPlayer].Count : 0;
            int p2Score = (p2 != null && PlayerMelds.ContainsKey(p2.idPlayer)) ? PlayerMelds[p2.idPlayer].Count : 0;

            var result = new GameResultDto
            {
                WinnerId = winnerId,
                LoserId = loserId,
                IsDraw = isDraw,
                PointsWon = 0,
                RoomCode = this.RoomCode,
                GamemodeId = GamemodeId,

                Player1Id = p1?.idPlayer ?? -1,
                Player1Name = p1?.nickname ?? "Unknown",
                Player1Score = p1Score,

                Player2Id = p2?.idPlayer ?? -1,
                Player2Name = p2?.nickname ?? "Unknown",
                Player2Score = p2Score,

                DurationSeconds = duration
            };

            OnGameFinished?.Invoke(result);

            Logger.Info($"Game {RoomCode} ended. Winner: {winnerId}. Draw: {isDraw}");
        }

        public void BroadcastGameResult(GameResultDto result)
        {
            Broadcast((callback) => callback.NotifyGameEnded(result));
            Logger.Info($"Broadcasted final game result for Room {RoomCode}. Points: {result.PointsWon}");
        }

        private void NotifyOpponent(int actingPlayerId, Action<IGameCallback> action)
        {
            int opponentId = playerCallbacks.Keys.FirstOrDefault(id => id != actingPlayerId);

            if (opponentId != 0 && playerCallbacks.TryGetValue(opponentId, out IGameCallback opponentCallback))
            {
                try
                {
                    action(opponentCallback);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Failed to notify opponent ID {opponentId} in Room {RoomCode}. Removing callback.");
                    playerCallbacks.TryRemove(opponentId, out _);
                }
            }
        }

        private void Broadcast(Action<IGameCallback> action)
        {
            foreach (var kvp in playerCallbacks)
            {
                try
                {
                    action(kvp.Value);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Broadcast failed for Player ID {kvp.Key} in Room {RoomCode}.");
                }
            }
        }
        public void NotifyGameEndedByAbandonment(int leavingPlayerId)
        {
            StopGame();

            int opponentId = playerCallbacks.Keys.FirstOrDefault(id => id != leavingPlayerId);

            if (opponentId != 0 && playerCallbacks.TryGetValue(opponentId, out IGameCallback callback))
            {
                try
                {
                    callback.OnOpponentLeft();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Error notifying room exit {RoomCode}");
                }
            }
        }

        public void SwapDrawnCard(int playerId, string cardIdToDiscard)
        {
            if (PlayerHands.TryGetValue(playerId, out List<Card> hand))
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

                Logger.Info($"Player ID {playerId} swapped drawn card {cardToTake.Id} for {cardToDiscard.Id} in Room {RoomCode}");
                ChangeTurn();
            }
            else
            {
                throw new InvalidOperationException(ServiceErrorType.OperationFailed.ToString());
            }
        }

        private sealed class MoveContext
        {
            public List<Card> CardsFromHand { get; set; }
            public List<Card> FullMeld { get; set; }
            public Card DiscardCard { get; set; }
            public bool UsingDiscardCard { get; set; }
            public List<string> HandCardIds { get; set; }
        }

        private MoveContext BuildMoveContext(int playerId, List<string> cardIds, List<Card> hand)
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

            context.HandCardIds = context.UsingDiscardCard
                ? cardIds.Where(id => id != context.DiscardCard.Id).ToList()
                : cardIds;

            context.CardsFromHand = hand.Where(card => context.HandCardIds.Contains(card.Id)).ToList();

            context.FullMeld = new List<Card>(context.CardsFromHand);
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

        private void ExecuteMeld(int playerId, List<Card> hand, MoveContext context)
        {
            hand.RemoveAll(card => context.HandCardIds.Contains(card.Id));

            if (context.UsingDiscardCard)
            {
                DiscardPile.RemoveAt(DiscardPile.Count - 1);
                playerReviewingDiscardId = null;
            }

            PlayerMelds[playerId].Add(context.FullMeld);
        }

        private void NotifyAndCheckGameStatus(int playerId, int handCount, List<Card> fullMeld, bool usingDiscardCard)
        {
            var cardDtos = fullMeld.Select(c => new CardDto
            {
                Id = c.Id,
                Suit = c.Suit,
                Rank = c.Rank,
                ImagePath = c.ImagePath
            }).ToArray();

            NotifyOpponent(playerId, (callback) =>
            {
                callback.NotifyOpponentMeld(cardDtos);
                callback.OnOpponentHandUpdated(handCount);
            });

            Logger.Info($"Player ID {playerId} successfully melded cards in Room {RoomCode}");

            if (usingDiscardCard)
            {
                BroadcastDiscardUpdate();
                mustDiscardToFinishTurn = true;
            }

            bool gameEnded = CheckWinCondition(playerId);

            if (!gameEnded && usingDiscardCard)
            {
                BroadcastDiscardUpdate();
            }
        }

        public void ProcessAFK(int afkPlayerId)
        {
            int rivalId = Players.FirstOrDefault(p => p.idPlayer != afkPlayerId)?.idPlayer ?? 0;

            if (playerCallbacks.TryGetValue(afkPlayerId, out var afkCallback))
            {
                Task.Run(() => afkCallback.NotifyGameEndedByAFK(AFK_REASON_SELF));
            }

            if (rivalId != 0 && playerCallbacks.TryGetValue(rivalId, out var rivalCallback))
            {
                Task.Run(() => rivalCallback.NotifyGameEndedByAFK(AFK_REASON_RIVAL));
            }

            Logger.Info($"Game ended in Room {RoomCode} due to inactivity of Player {afkPlayerId}.");

            StopGame();
        }
    }
}
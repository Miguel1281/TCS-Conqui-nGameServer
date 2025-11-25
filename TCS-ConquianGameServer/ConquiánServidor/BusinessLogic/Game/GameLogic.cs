using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.Properties.Langs;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace ConquiánServidor.BusinessLogic.Game
{
    public class ConquianGame
    {
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
            var suits = new[] { "Oros", "Copas", "Espadas", "Bastos" };
            var ranks = new[] { 1, 2, 3, 4, 5, 6, 7, 10, 11, 12 };

            foreach (var suit in suits)
            {
                foreach (var rank in ranks)
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
            int cardsToDeal = (GamemodeId == 1) ? 6 : 8;

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
            return (GamemodeId == 1) ? 600 : 1200;
        }

        public void StartGameTimer()
        {
            remainingSeconds = GetInitialTimeInSeconds();
            gameTimer = new Timer(1000);
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
            if (playerId != currentTurnPlayerId)
            {
                throw new InvalidOperationException(Lang.ErrorGameNotYourTurn);
            }

            if (mustDiscardToFinishTurn)
            {
                throw new InvalidOperationException("Ya bajaste juego. Debes pagar una carta para terminar.");
            }

            if (cardIds == null || cardIds.Count < 2) 
            {
                throw new ArgumentException(Lang.ErrorGameInvalidMove);
            }

            if (PlayerHands.TryGetValue(playerId, out List<Card> hand))
            {
                bool usingDiscardCard = false;
                Card discardCard = null;

                if (DiscardPile.Count > 0)
                {
                    discardCard = DiscardPile.Last();
                    if (cardIds.Contains(discardCard.Id))
                    {
                        if (playerId != playerReviewingDiscardId && !isCardDrawnFromDeck)
                        {
                            throw new InvalidOperationException("No puedes tomar esta carta ahora.");
                        }
                        usingDiscardCard = true;
                    }
                }

                var handCardIds = cardIds.Where(id => !usingDiscardCard || id != discardCard.Id).ToList();
                var cardsToPlay = hand.Where(card => handCardIds.Contains(card.Id)).ToList();

                if (cardsToPlay.Count != handCardIds.Count)
                {
                    throw new InvalidOperationException(Lang.ErrorGameInvalidMove); 
                }

                var fullMeld = new List<Card>(cardsToPlay);
                if (usingDiscardCard)
                {
                    fullMeld.Add(discardCard);
                }

                if (fullMeld.Count < 3)
                {
                    throw new ArgumentException("Un juego debe tener al menos 3 cartas.");
                }

                if (IsValidMeld(fullMeld))
                {
                    hand.RemoveAll(card => handCardIds.Contains(card.Id));

                    if (usingDiscardCard)
                    {
                        DiscardPile.RemoveAt(DiscardPile.Count - 1);
                        playerReviewingDiscardId = null;
                    }

                    PlayerMelds[playerId].Add(fullMeld);

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
                        callback.OnOpponentHandUpdated(hand.Count);
                    });

                    if (usingDiscardCard)
                    {
                        BroadcastDiscardUpdate();
                        mustDiscardToFinishTurn = true;
                    }

                    Logger.Info($"Player ID {playerId} successfully melded cards in Room {RoomCode}");

                    bool gameEnded = CheckWinCondition(playerId);

                    if (!gameEnded)
                    {
                        if (usingDiscardCard)
                        {
                            BroadcastDiscardUpdate();
                            mustDiscardToFinishTurn = true;
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException("Jugada invalida");
                }
            }
            else
            {
                throw new InvalidOperationException(Lang.ErrorGameAction);
            }
        }

        private bool CheckWinCondition(int playerId)
        {
            int meldsCount = PlayerMelds[playerId].Count;
            bool hasWon = false;

            if (GamemodeId == 1 && meldsCount >= 2)
            {
                hasWon = true;
            }
            else if (GamemodeId != 1 && meldsCount >= 3)
            {
                hasWon = true;
            }

            if (hasWon)
            {
                FinishGame(playerId, false);
                return true;
            }
            return false;
        }

        private void BroadcastDiscardUpdate()
        {
            CardDto topCardDto = null;
            if (DiscardPile.Count > 0)
            {
                var c = DiscardPile.Last();
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

            if (mustDiscardToFinishTurn)
            {
                throw new InvalidOperationException("No puedes pasar. Debes pagar una carta para terminar.");
            }

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
                return;
            }
        }

        public void DrawFromDeck(int playerId)
        {
            if (playerId != currentTurnPlayerId)
            {
                throw new InvalidOperationException(Lang.ErrorGameNotYourTurn);
            }

            if (mustDiscardToFinishTurn)
            {
                throw new InvalidOperationException("No puedes robar. Debes pagar una carta para terminar.");
            }

            if (playerReviewingDiscardId != null)
            {
                throw new InvalidOperationException("Debes decidir sobre la carta del descarte primero (Tomar o Pasar).");
            }

            if (StockPile.Count == 0)
            {
                throw new InvalidOperationException("Mazo vacío.");
                DetermineWinnerByPoints(); 
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
            if (playerId != currentTurnPlayerId)
            {
                throw new InvalidOperationException(Lang.ErrorGameNotYourTurn);
            }

            var card = PlayerHands[playerId].FirstOrDefault(c => c.Id == cardId);
            if (card == null)
            {
                throw new ArgumentException(Lang.ErrorGameInvalidMove);
            }

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

        private static bool IsValidMeld(List<Card> cards)
        {
            if (cards == null || cards.Count < 3) return false;

            cards = cards.OrderBy(c => c.Rank).ToList();

            bool isTercia = cards.All(c => c.Rank == cards[0].Rank);
            bool distinctSuits = cards.Select(c => c.Suit).Distinct().Count() == cards.Count;
            if (isTercia && distinctSuits) return true;

            bool isCorrida = cards.All(c => c.Suit == cards[0].Suit);
            if (!isCorrida) return false;

            for (int i = 0; i < cards.Count - 1; i++)
            {
                int currentRank = cards[i].Rank;
                int nextRank = cards[i + 1].Rank;

                if (currentRank == 7 && nextRank == 10) continue; 
                if (nextRank != currentRank + 1) return false;
            }

            return true;
        }

        private void DetermineWinnerByPoints()
        {
            var playerIds = Players.Select(p => p.idPlayer).ToList();
            int p1 = playerIds[0];
            int p2 = playerIds[1];

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

            var result = new GameResultDto
            {
                WinnerId = winnerId,
                LoserId = loserId,
                IsDraw = isDraw,
                PointsWon = isDraw ? 0 : 25 
            };

            OnGameFinished?.Invoke(result);

            Broadcast((callback) => callback.NotifyGameEnded(result));

            Logger.Info($"Game {RoomCode} ended. Winner: {winnerId}. Draw: {isDraw}");
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
    }
}
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
        private int currentTurnSeconds;
        private int currentTurnPlayerId;
        private const int TURN_DURATION_SECONDS = 20;

        private readonly ConcurrentDictionary<int, IGameCallback> playerCallbacks;
        public List<PlayerDto> Players { get; private set; }
        private List<Card> Deck { get; set; }
        public Dictionary<int, List<Card>> PlayerHands { get; private set; }
        public List<Card> StockPile { get; private set; }
        public List<Card> DiscardPile { get; private set; }
        public Dictionary<int, List<List<Card>>> PlayerMelds { get; private set; }

        private static readonly Random Rng = new Random();

        public ConquianGame(string roomCode, int gamemodeId, List<PlayerDto> players)
        {
            RoomCode = roomCode;
            GamemodeId = gamemodeId;
            Players = players;
            playerCallbacks = new ConcurrentDictionary<int, IGameCallback>();
            currentTurnPlayerId = Players[0].idPlayer;

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
            currentTurnSeconds = TURN_DURATION_SECONDS;
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
            currentTurnSeconds--;

            if (remainingSeconds <= 0)
            {
                Logger.Info($"Game timeout reached for Room Code: {RoomCode}. Stopping game.");
                StopGame();
            }
            else if (currentTurnSeconds <= 0)
            {
                Logger.Info($"Turn timeout for Player ID: {currentTurnPlayerId} in Room Code: {RoomCode}. Changing turn.");
                ChangeTurn();
            }

            BroadcastTime(remainingSeconds, currentTurnSeconds, currentTurnPlayerId);
        }

        private void ChangeTurn()
        {
            currentTurnSeconds = TURN_DURATION_SECONDS;
            var currentPlayerIndex = Players.FindIndex(p => p.idPlayer == currentTurnPlayerId);
            var nextPlayerIndex = (currentPlayerIndex + 1) % Players.Count;
            currentTurnPlayerId = Players[nextPlayerIndex].idPlayer;
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

        public int GetCurrentTurnSeconds()
        {
            return currentTurnSeconds;
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
                Logger.Warn($"Game action failed: Player ID {playerId} attempted move out of turn in Room {RoomCode}");
                throw new InvalidOperationException(Lang.ErrorGameNotYourTurn);
            }

            if (cardIds == null || cardIds.Count < 3)
            {
                Logger.Warn($"Game action failed: Player ID {playerId} attempted to play fewer than 3 cards in Room {RoomCode}");
                throw new ArgumentException(Lang.ErrorGameInvalidMove);
            }

            if (PlayerHands.TryGetValue(playerId, out List<Card> hand))
            {
                var cardsToPlay = hand.Where(card => cardIds.Contains(card.Id)).ToList();

                if (cardsToPlay.Count != cardIds.Count)
                {
                    Logger.Warn($"Game action failed: Player ID {playerId} attempted to play cards they do not possess in Room {RoomCode}");
                    throw new InvalidOperationException(Lang.ErrorGameInvalidMove);
                }

                if (IsValidMeld(cardsToPlay))
                {
                    hand.RemoveAll(card => cardIds.Contains(card.Id));
                    PlayerMelds[playerId].Add(cardsToPlay);

                    var cardDtos = cardsToPlay.Select(c => new CardDto
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

                    Logger.Info($"Player ID {playerId} successfully melded cards in Room {RoomCode}");
                }
                else
                {
                    Logger.Warn($"Game action failed: Invalid meld (set/run rules not met) by Player ID {playerId} in Room {RoomCode}");
                    throw new InvalidOperationException(Lang.ErrorGameInvalidMove);
                }
            }
            else
            {
                Logger.Error($"Hand not found for Player ID {playerId} in Room {RoomCode}");
                throw new InvalidOperationException(Lang.ErrorGameAction);
            }
        }

        private static bool IsValidMeld(List<Card> cards)
        {
            if (cards == null || cards.Count < 3)
            {
                return false;
            }

            cards = cards.OrderBy(c => c.Rank).ToList();

            bool isTercia = cards.All(c => c.Rank == cards[0].Rank);
            bool distinctSuits = cards.Select(c => c.Suit).Distinct().Count() == cards.Count;
            if (isTercia && distinctSuits)
            {
                return true;
            }

            bool isCorrida = cards.All(c => c.Suit == cards[0].Suit);
            if (!isCorrida)
            {
                return false;
            }

            for (int i = 0; i < cards.Count - 1; i++)
            {
                int currentRank = cards[i].Rank;
                int nextRank = cards[i + 1].Rank;

                if (currentRank == 7 && nextRank == 10)
                {
                    continue;
                }

                if (nextRank != currentRank + 1)
                {
                    return false;
                }
            }

            return true;
        }

        public void DrawFromDeck(int playerId)
        {
            if (playerId != currentTurnPlayerId)
            {
                Logger.Warn($"DrawFromDeck failed: Player ID {playerId} attempted action out of turn in Room {RoomCode}");
                throw new InvalidOperationException(Lang.ErrorGameNotYourTurn);
            }

            if (StockPile.Count == 0)
            {
                Logger.Warn($"DrawFromDeck failed: Stock pile is empty in Room {RoomCode}");
                throw new InvalidOperationException(Lang.ErrorGameAction);
            }

            var card = StockPile[0];
            StockPile.RemoveAt(0);
            DiscardPile.Add(card);

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

        public CardDto DrawFromDiscard(int playerId)
        {
            if (playerId != currentTurnPlayerId)
            {
                Logger.Warn($"DrawFromDiscard failed: Player ID {playerId} attempted action out of turn in Room {RoomCode}");
                throw new InvalidOperationException(Lang.ErrorGameNotYourTurn);
            }

            if (DiscardPile.Count == 0)
            {
                Logger.Warn($"DrawFromDiscard failed: Discard pile is empty in Room {RoomCode}");
                throw new InvalidOperationException(Lang.ErrorGameInvalidMove);
            }

            int lastIndex = DiscardPile.Count - 1;
            var card = DiscardPile[lastIndex];

            DiscardPile.RemoveAt(lastIndex);
            PlayerHands[playerId].Add(card);

            CardDto newTopCardDto = null;
            if (DiscardPile.Count > 0)
            {
                var newTopCard = DiscardPile[DiscardPile.Count - 1];
                newTopCardDto = new CardDto
                {
                    Id = newTopCard.Id,
                    Suit = newTopCard.Suit,
                    Rank = newTopCard.Rank,
                    ImagePath = newTopCard.ImagePath
                };
            }

            Broadcast((callback) =>
            {
                callback.NotifyOpponentDiscarded(newTopCardDto);
            });

            NotifyOpponent(playerId, (callback) =>
            {
                callback.OnOpponentHandUpdated(PlayerHands[playerId].Count);
            });

            Logger.Info($"Player ID {playerId} drew from discard pile in Room {RoomCode}");

            return new CardDto
            {
                Id = card.Id,
                Suit = card.Suit,
                Rank = card.Rank,
                ImagePath = card.ImagePath
            };
        }

        public void DiscardCard(int playerId, string cardId)
        {
            if (playerId != currentTurnPlayerId)
            {
                Logger.Warn($"DiscardCard failed: Player ID {playerId} attempted action out of turn in Room {RoomCode}");
                throw new InvalidOperationException(Lang.ErrorGameNotYourTurn);
            }

            var card = PlayerHands[playerId].FirstOrDefault(c => c.Id == cardId);
            if (card == null)
            {
                Logger.Warn($"DiscardCard failed: Card ID {cardId} not found in Player ID {playerId}'s hand in Room {RoomCode}");
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
    }
}
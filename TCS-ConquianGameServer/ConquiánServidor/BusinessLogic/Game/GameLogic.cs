using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
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
            currentTurnPlayerId = Players.First().idPlayer;

            PlayerHands = new Dictionary<int, List<Card>>();
            PlayerMelds = new Dictionary<int, List<List<Card>>>();
            foreach (var player in players)
            {
                PlayerHands[player.idPlayer] = new List<Card>();
                PlayerMelds[player.idPlayer] = new List<List<Card>>();
            }
            InitializeGame();
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
                    var card = Deck.First();
                    Deck.RemoveAt(0);
                    PlayerHands[player.idPlayer].Add(card);
                }
            }
        }

        private void SetupPiles()
        {
            StockPile = Deck;
            DiscardPile = new List<Card>();
            var firstDiscard = StockPile.First();
            StockPile.RemoveAt(0);
            DiscardPile.Add(firstDiscard);
        }
        public void RegisterPlayerCallback(int playerId, IGameCallback callback)
        {
            playerCallbacks[playerId] = callback;
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
        }
        private void OnTimerTick(object sender, ElapsedEventArgs e)
        {
            remainingSeconds--;

            currentTurnSeconds--;

            if (remainingSeconds <= 0)
            {
                StopGame();
            }
            else if (currentTurnSeconds <= 0)
            {
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
            foreach (var callback in playerCallbacks.Values)
            {
                try
                {
                    callback.OnTimeUpdated(gameSeconds, turnSeconds, currentPlayerId);
                }
                catch (Exception)
                {
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
            }
        }

        public void ProcessPlayerMove(int playerId, List<string> cardIds)
        {
            if (cardIds == null || cardIds.Count < 3) return;

            if (PlayerHands.TryGetValue(playerId, out List<Card> hand))
            {
                var cardsToPlay = hand.Where(card => cardIds.Contains(card.Id)).ToList();

                if (cardsToPlay.Count != cardIds.Count)
                {
                    Logger.Warn($"Jugador {playerId} intentó jugar cartas que no tiene.");
                    return;
                }

                if (IsValidMeld(cardsToPlay))
                {
                    hand.RemoveAll(card => cardIds.Contains(card.Id));
                    PlayerMelds[playerId].Add(cardsToPlay);

                    var cardDtos = cardsToPlay.Select(c => new CardDto { Id = c.Id, Suit = c.Suit, Rank = c.Rank, ImagePath = c.ImagePath }).ToArray();

                    NotifyOpponent(playerId, (callback) =>
                    {
                        callback.NotifyOpponentMeld(cardDtos);
                        callback.OnOpponentHandUpdated(hand.Count);
                    });
                }
                else
                {
                    Logger.Warn($"Jugador {playerId} intentó un juego inválido.");
                }
            }
        }

        private bool IsValidMeld(List<Card> cards)
        {
            if (cards == null || cards.Count < 3) return false;

            cards = cards.OrderBy(c => c.Rank).ToList();

            bool isTercia = cards.All(c => c.Rank == cards[0].Rank);
            bool distinctSuits = cards.Select(c => c.Suit).Distinct().Count() == cards.Count;
            if (isTercia && distinctSuits)
            {
                return true;
            }

            bool isCorrida = cards.All(c => c.Suit == cards[0].Suit);
            if (!isCorrida) return false;

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
            if (playerId != currentTurnPlayerId) return;
            if (StockPile.Count == 0) return; 

            var card = StockPile.First();
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
        }

        public CardDto DrawFromDiscard(int playerId)
        {
            if (playerId != currentTurnPlayerId) return null;
            if (DiscardPile.Count == 0) return null;

            var card = DiscardPile.Last();
            DiscardPile.RemoveAt(DiscardPile.Count - 1);
            PlayerHands[playerId].Add(card);

            var newTopCard = DiscardPile.Any() ? DiscardPile.Last() : null;
            CardDto newTopCardDto = null;
            if (newTopCard != null)
            {
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
            if (playerId != currentTurnPlayerId) return; 

            var card = PlayerHands[playerId].FirstOrDefault(c => c.Id == cardId);
            if (card == null) return;

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
                    Logger.Error(ex, $"Failed to send callback to player {opponentId}. Removing player.");
                    playerCallbacks.TryRemove(opponentId, out _);
                }
            }
        }

        private void Broadcast(Action<IGameCallback> action)
        {
            foreach (var callback in playerCallbacks.Values)
            {
                try
                {
                    action(callback);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Failed to broadcast callback.");
                }
            }
        }
    }
}
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace ConquiánServidor.BusinessLogic.Game
{
    public class ConquianGame
    {
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

        private static readonly Random Rng = new Random();

        public ConquianGame(string roomCode, int gamemodeId, List<PlayerDto> players)
        {
            RoomCode = roomCode;
            GamemodeId = gamemodeId;
            Players = players;
            playerCallbacks = new ConcurrentDictionary<int, IGameCallback>();
            currentTurnPlayerId = Players.First().idPlayer;
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
            PlayerHands = new Dictionary<int, List<Card>>();
            foreach (var player in Players)
            {
                PlayerHands[player.idPlayer] = new List<Card>();
            }

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
    }
}
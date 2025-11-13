using ConquiánServidor.Contracts.DataContracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConquiánServidor.BusinessLogic.Game
{
    public class ConquianGame
    {
        public string RoomCode { get; private set; }
        public int GamemodeId { get; private set; }
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
    }
}
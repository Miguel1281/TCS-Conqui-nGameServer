using System;

namespace ConquiánServidor.BusinessLogic.Game
{
    public class Card
    {
        public string Suit { get; set; } 
        public int Rank { get; set; }    
        public string ImagePath { get; set; }

        public string Id => $"{Suit}_{Rank}";

        public Card(string suit, int rank)
        {
            Suit = suit;
            Rank = rank;
            ImagePath = $"/Resources/Assets/{suit}/{suit.ToLower()}_{rank}s.jpg";
        }
    }
}
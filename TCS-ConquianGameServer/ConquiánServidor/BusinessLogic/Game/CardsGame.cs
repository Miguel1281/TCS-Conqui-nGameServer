
namespace ConquiánServidor.BusinessLogic.Game
{
    public class CardsGame
    {
        public string Suit { get; set; } 
        public int Rank { get; set; }    
        public string ImagePath { get; set; }

        public string Id
        {
            get
            {
                return $"{Suit}_{Rank}";
            }
        }

        public CardsGame(string suit, int rank)
        {
            Suit = suit;
            Rank = rank;
            ImagePath = $"/Resources/Assets/{suit}/{suit.ToLower()}_{rank}s.png";
        }
    }
}
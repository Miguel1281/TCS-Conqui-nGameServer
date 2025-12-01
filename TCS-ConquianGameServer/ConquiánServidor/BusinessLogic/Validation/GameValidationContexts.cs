using ConquiánServidor.BusinessLogic.Game;

namespace ConquiánServidor.BusinessLogic.Validation
{
    public class DrawValidationContext
    {
        public int PlayerId { get; set; }
        public int CurrentTurnPlayerId { get; set; }
        public bool IsCardDrawnFromDeck { get; set; }
        public bool MustDiscardToFinishTurn { get; set; }
        public int? PlayerReviewingDiscardId { get; set; }
        public int StockCount { get; set; }
    }


    public class SwapValidationContext
    {
        public int PlayerId { get; set; }
        public int CurrentTurnPlayerId { get; set; }
        public bool IsCardDrawnFromDeck { get; set; }
        public int? PlayerReviewingDiscardId { get; set; }
        public bool MustDiscardToFinishTurn { get; set; }
        public Card CardToDiscard { get; set; }
        public int DiscardPileCount { get; set; }
    }
}

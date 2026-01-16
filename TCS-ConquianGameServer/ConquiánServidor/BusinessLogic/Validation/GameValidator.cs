using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.BusinessLogic.Game;
using ConquiánServidor.Contracts.DataContracts;
using System.Collections.Generic;
using System.Linq;

namespace ConquiánServidor.BusinessLogic.Validation
{
    public static class GameValidator
    {
        private const int MINIMUM_MELD_SIZE = 3;
        private const int MINIMUM_INPUT_CARDS = 2;
        private const int EMPTY_COUNT = 0;

        private const int SPANISH_DECK_LIMIT_BEFORE_JUMP = 7;
        private const int SPANISH_DECK_FIGURE_START = 10;

       public static void ValidateTurnOwner(int playerId, int currentTurnPlayerId)
        {
            if (playerId != currentTurnPlayerId)
            {
                throw new BusinessLogicException(ServiceErrorType.NotYourTurn);
            }
        }

        public static void ValidateActionAllowed(bool mustDiscardToFinishTurn)
        {
            if (mustDiscardToFinishTurn)
            {
                throw new BusinessLogicException(ServiceErrorType.MustDiscardToFinish);
            }
        }

        public static void ValidateMoveInputs(List<string> cardIds)
        {
            if (cardIds == null || cardIds.Count < MINIMUM_INPUT_CARDS)
            {
                throw new BusinessLogicException(ServiceErrorType.GameRuleViolation);
            }
        }

        public static void ValidateDiscardUsage(int playerId, int? playerReviewingDiscardId, bool isCardDrawnFromDeck)
        {
            if (playerId != playerReviewingDiscardId && !isCardDrawnFromDeck)
            {
                throw new BusinessLogicException(ServiceErrorType.InvalidCardAction);
            }
        }

        public static void ValidateCardsInHand(int countToPlay, int countRequired)
        {
            if (countToPlay != countRequired)
            {
                throw new BusinessLogicException(ServiceErrorType.GameRuleViolation);
            }
        }

        public static void ValidateMeldSize(int count)
        {
            if (count < MINIMUM_MELD_SIZE)
            {
                throw new BusinessLogicException(ServiceErrorType.InvalidMeld);
            }
        }

        public static void ValidateDraw(DrawValidationContext context)
        {
            ValidateTurnOwner(context.PlayerId, context.CurrentTurnPlayerId);

            if (context.IsCardDrawnFromDeck)
            {
                throw new BusinessLogicException(ServiceErrorType.AlreadyDrawn);
            }

            ValidateActionAllowed(context.MustDiscardToFinishTurn);

            if (context.PlayerReviewingDiscardId != null)
            {
                throw new BusinessLogicException(ServiceErrorType.PendingDiscardAction);
            }

            if (context.StockCount == EMPTY_COUNT)
            {
                throw new BusinessLogicException(ServiceErrorType.DeckEmpty);
            }
        }

        public static void ValidateDiscard(int playerId, int currentTurnPlayerId, CardsGame card)
        {
            ValidateTurnOwner(playerId, currentTurnPlayerId);

            if (card == null)
            {
                throw new BusinessLogicException(ServiceErrorType.GameRuleViolation);
            }
        }

        public static void ValidateSwap(SwapValidationContext context)
        {
            ValidateTurnOwner(context.PlayerId, context.CurrentTurnPlayerId);

            if (!context.IsCardDrawnFromDeck)
            {
                throw new BusinessLogicException(ServiceErrorType.InvalidCardAction);
            }

            if (context.PlayerReviewingDiscardId != context.PlayerId)
            {
                throw new BusinessLogicException(ServiceErrorType.InvalidCardAction);
            }

            ValidateActionAllowed(context.MustDiscardToFinishTurn);

            if (context.CardToDiscard == null)
            {
                throw new BusinessLogicException(ServiceErrorType.GameRuleViolation);
            }

            if (context.DiscardPileCount == EMPTY_COUNT)
            {
                throw new BusinessLogicException(ServiceErrorType.EmptyDiscaard);
            }
        }

        public static bool IsValidMeldCombination(List<CardsGame> cards)
        {
            if (cards == null || cards.Count < MINIMUM_MELD_SIZE)
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

                if (currentRank == SPANISH_DECK_LIMIT_BEFORE_JUMP && nextRank == SPANISH_DECK_FIGURE_START)
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
    }
}
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
        private const int RANK_INCREMENT = 1;
        private const int FIRST_CARD_INDEX = 0;

        public static void ValidateTurnOwner(int playerId, int currentTurnPlayerId)
        {
            bool isNotPlayerTurn = (playerId != currentTurnPlayerId);

            if (isNotPlayerTurn)
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
            bool inputIsInvalid = (cardIds == null || cardIds.Count < MINIMUM_INPUT_CARDS);

            if (inputIsInvalid)
            {
                throw new BusinessLogicException(ServiceErrorType.GameRuleViolation);
            }
        }

        public static void ValidateDiscardUsage(
            int playerId,
            int? playerReviewingDiscardId,
            bool isCardDrawnFromDeck)
        {
            bool cannotUseDiscard = (playerId != playerReviewingDiscardId && !isCardDrawnFromDeck);

            if (cannotUseDiscard)
            {
                throw new BusinessLogicException(ServiceErrorType.InvalidCardAction);
            }
        }

        public static void ValidateCardsInHand(int countToPlay, int countRequired)
        {
            bool countMismatch = (countToPlay != countRequired);

            if (countMismatch)
            {
                throw new BusinessLogicException(ServiceErrorType.GameRuleViolation);
            }
        }

        public static void ValidateMeldSize(int count)
        {
            bool meldTooSmall = (count < MINIMUM_MELD_SIZE);

            if (meldTooSmall)
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

            bool hasReviewingPlayer = (context.PlayerReviewingDiscardId != null);

            if (hasReviewingPlayer)
            {
                throw new BusinessLogicException(ServiceErrorType.PendingDiscardAction);
            }

            bool deckIsEmpty = (context.StockCount == EMPTY_COUNT);

            if (deckIsEmpty)
            {
                throw new BusinessLogicException(ServiceErrorType.DeckEmpty);
            }
        }

        public static void ValidateDiscard(int playerId, int currentTurnPlayerId, CardsGame card)
        {
            ValidateTurnOwner(playerId, currentTurnPlayerId);

            bool cardIsNull = (card == null);

            if (cardIsNull)
            {
                throw new BusinessLogicException(ServiceErrorType.GameRuleViolation);
            }
        }

        public static void ValidateSwap(SwapValidationContext context)
        {
            ValidateTurnOwner(context.PlayerId, context.CurrentTurnPlayerId);

            bool hasNotDrawnCard = !context.IsCardDrawnFromDeck;

            if (hasNotDrawnCard)
            {
                throw new BusinessLogicException(ServiceErrorType.InvalidCardAction);
            }

            bool isNotReviewingPlayer = (context.PlayerReviewingDiscardId != context.PlayerId);

            if (isNotReviewingPlayer)
            {
                throw new BusinessLogicException(ServiceErrorType.InvalidCardAction);
            }

            ValidateActionAllowed(context.MustDiscardToFinishTurn);

            bool cardToDiscardIsNull = (context.CardToDiscard == null);

            if (cardToDiscardIsNull)
            {
                throw new BusinessLogicException(ServiceErrorType.GameRuleViolation);
            }

            bool discardPileIsEmpty = (context.DiscardPileCount == EMPTY_COUNT);

            if (discardPileIsEmpty)
            {
                throw new BusinessLogicException(ServiceErrorType.EmptyDiscaard);
            }
        }

        public static bool IsValidMeldCombination(List<CardsGame> cards)
        {
            bool cardsAreInvalid = (cards == null || cards.Count < MINIMUM_MELD_SIZE);

            if (cardsAreInvalid)
            {
                const bool INVALID_MELD = false;
                return INVALID_MELD;
            }

            List<CardsGame> sortedCards = cards.OrderBy(c => c.Rank).ToList();

            bool isTercia = CheckIfTercia(sortedCards);

            if (isTercia)
            {
                const bool VALID_TERCIA = true;
                return VALID_TERCIA;
            }

            bool isCorrida = CheckIfCorrida(sortedCards);
            return isCorrida;
        }

        private static bool CheckIfTercia(List<CardsGame> cards)
        {
            int firstCardRank = cards[FIRST_CARD_INDEX].Rank;
            bool allSameRank = cards.All(c => c.Rank == firstCardRank);

            int distinctSuitCount = cards.Select(c => c.Suit).Distinct().Count();
            bool allDifferentSuits = (distinctSuitCount == cards.Count);

            bool isTercia = (allSameRank && allDifferentSuits);
            return isTercia;
        }

        private static bool CheckIfCorrida(List<CardsGame> cards)
        {
            bool allSameSuit = CheckIfAllSameSuit(cards);

            if (!allSameSuit)
            {
                const bool INVALID_CORRIDA = false;
                return INVALID_CORRIDA;
            }

            bool hasConsecutiveRanks = CheckIfConsecutiveRanks(cards);
            return hasConsecutiveRanks;
        }

        private static bool CheckIfAllSameSuit(List<CardsGame> cards)
        {
            string firstCardSuit = cards[FIRST_CARD_INDEX].Suit;
            bool allSameSuit = cards.All(c => c.Suit == firstCardSuit);
            return allSameSuit;
        }

        private static bool CheckIfConsecutiveRanks(List<CardsGame> cards)
        {
            for (int i = FIRST_CARD_INDEX; i < cards.Count - 1; i++)
            {
                bool isConsecutive = AreRanksConsecutive(cards, i);

                if (!isConsecutive)
                {
                    const bool SEQUENCE_BROKEN = false;
                    return SEQUENCE_BROKEN;
                }
            }

            const bool ALL_CONSECUTIVE = true;
            return ALL_CONSECUTIVE;
        }

        private static bool AreRanksConsecutive(List<CardsGame> cards, int currentIndex)
        {
            int currentRank = cards[currentIndex].Rank;
            int nextRank = cards[currentIndex + 1].Rank;

            bool isSpanishDeckJump = IsSpanishDeckJump(currentRank, nextRank);

            if (isSpanishDeckJump)
            {
                const bool VALID_JUMP = true;
                return VALID_JUMP;
            }

            bool ranksAreConsecutive = (nextRank == currentRank + RANK_INCREMENT);
            return ranksAreConsecutive;
        }

        private static bool IsSpanishDeckJump(int currentRank, int nextRank)
        {
            bool isJumpFrom7To10 = (currentRank == SPANISH_DECK_LIMIT_BEFORE_JUMP &&
                                    nextRank == SPANISH_DECK_FIGURE_START);
            return isJumpFrom7To10;
        }
    }
}
using Xunit;
using System.Collections.Generic;
using ConquiánServidor.BusinessLogic.Validation;
using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.BusinessLogic.Game;

namespace ConquiánServidor.Tests.Validation
{
    public class GameValidatorTest
    {
        [Fact]
        public void ValidateTurnOwner_IdsMatch_ReturnsVoid()
        {
            GameValidator.ValidateTurnOwner(1, 1);
        }

        [Fact]
        public void ValidateTurnOwner_IdsDoNotMatch_ThrowsNotYourTurnException()
        {
            var exception = Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateTurnOwner(1, 2));
            Assert.Equal(ServiceErrorType.NotYourTurn, exception.ErrorType);
        }

        [Fact]
        public void ValidateActionAllowed_MustDiscardIsFalse_ReturnsVoid()
        {
            GameValidator.ValidateActionAllowed(false);
        }

        [Fact]
        public void ValidateActionAllowed_MustDiscardIsTrue_ThrowsMustDiscardToFinishException()
        {
            var exception = Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateActionAllowed(true));
            Assert.Equal(ServiceErrorType.MustDiscardToFinish, exception.ErrorType);
        }

        [Fact]
        public void ValidateMoveInputs_ValidList_ReturnsVoid()
        {
            var cardIds = new List<string> { "C1", "C2" };
            GameValidator.ValidateMoveInputs(cardIds);
        }

        [Fact]
        public void ValidateMoveInputs_NullList_ThrowsGameRuleViolationException()
        {
            var exception = Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateMoveInputs(null));
            Assert.Equal(ServiceErrorType.GameRuleViolation, exception.ErrorType);
        }

        [Fact]
        public void ValidateMoveInputs_ListWithLessThanTwoItems_ThrowsGameRuleViolationException()
        {
            var cardIds = new List<string> { "C1" };
            var exception = Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateMoveInputs(cardIds));
            Assert.Equal(ServiceErrorType.GameRuleViolation, exception.ErrorType);
        }

        [Fact]
        public void ValidateDiscardUsage_PlayerIsReviewer_ReturnsVoid()
        {
            GameValidator.ValidateDiscardUsage(1, 1, false);
        }

        [Fact]
        public void ValidateDiscardUsage_CardDrawnFromDeck_ReturnsVoid()
        {
            GameValidator.ValidateDiscardUsage(1, 2, true);
        }

        [Fact]
        public void ValidateDiscardUsage_PlayerNotReviewerAndNotDrawnFromDeck_ThrowsInvalidCardActionException()
        {
            var exception = Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateDiscardUsage(1, 2, false));
            Assert.Equal(ServiceErrorType.InvalidCardAction, exception.ErrorType);
        }

        [Fact]
        public void ValidateCardsInHand_CountsMatch_ReturnsVoid()
        {
            GameValidator.ValidateCardsInHand(9, 9);
        }

        [Fact]
        public void ValidateCardsInHand_CountsDoNotMatch_ThrowsGameRuleViolationException()
        {
            var exception = Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateCardsInHand(8, 9));
            Assert.Equal(ServiceErrorType.GameRuleViolation, exception.ErrorType);
        }

        [Fact]
        public void ValidateMeldSize_CountIsThree_ReturnsVoid()
        {
            GameValidator.ValidateMeldSize(3);
        }

        [Fact]
        public void ValidateMeldSize_CountLessThanThree_ThrowsInvalidMeldException()
        {
            var exception = Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateMeldSize(2));
            Assert.Equal(ServiceErrorType.InvalidMeld, exception.ErrorType);
        }

        [Fact]
        public void ValidateDraw_ValidContext_ReturnsVoid()
        {
            var context = new DrawValidationContext
            {
                PlayerId = 1,
                CurrentTurnPlayerId = 1,
                IsCardDrawnFromDeck = false,
                MustDiscardToFinishTurn = false,
                PlayerReviewingDiscardId = null,
                StockCount = 10
            };

            GameValidator.ValidateDraw(context);
        }

        [Fact]
        public void ValidateDraw_NotTurnOwner_ThrowsNotYourTurnException()
        {
            var context = new DrawValidationContext
            {
                PlayerId = 1,
                CurrentTurnPlayerId = 2
            };

            var exception = Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateDraw(context));
            Assert.Equal(ServiceErrorType.NotYourTurn, exception.ErrorType);
        }

        [Fact]
        public void ValidateDraw_AlreadyDrawn_ThrowsAlreadyDrawnException()
        {
            var context = new DrawValidationContext
            {
                PlayerId = 1,
                CurrentTurnPlayerId = 1,
                IsCardDrawnFromDeck = true
            };

            var exception = Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateDraw(context));
            Assert.Equal(ServiceErrorType.AlreadyDrawn, exception.ErrorType);
        }

        [Fact]
        public void ValidateDraw_MustDiscardToFinish_ThrowsMustDiscardToFinishException()
        {
            var context = new DrawValidationContext
            {
                PlayerId = 1,
                CurrentTurnPlayerId = 1,
                IsCardDrawnFromDeck = false,
                MustDiscardToFinishTurn = true
            };

            var exception = Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateDraw(context));
            Assert.Equal(ServiceErrorType.MustDiscardToFinish, exception.ErrorType);
        }

        [Fact]
        public void ValidateDraw_PendingDiscardAction_ThrowsPendingDiscardActionException()
        {
            var context = new DrawValidationContext
            {
                PlayerId = 1,
                CurrentTurnPlayerId = 1,
                IsCardDrawnFromDeck = false,
                MustDiscardToFinishTurn = false,
                PlayerReviewingDiscardId = 2
            };

            var exception = Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateDraw(context));
            Assert.Equal(ServiceErrorType.PendingDiscardAction, exception.ErrorType);
        }

        [Fact]
        public void ValidateDraw_StockEmpty_ThrowsDeckEmptyException()
        {
            var context = new DrawValidationContext
            {
                PlayerId = 1,
                CurrentTurnPlayerId = 1,
                IsCardDrawnFromDeck = false,
                MustDiscardToFinishTurn = false,
                PlayerReviewingDiscardId = null,
                StockCount = 0
            };

            var exception = Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateDraw(context));
            Assert.Equal(ServiceErrorType.DeckEmpty, exception.ErrorType);
        }

        [Fact]
        public void ValidateDiscard_Valid_ReturnsVoid()
        {
            var card = new CardsGame("Oros", 1);
            GameValidator.ValidateDiscard(1, 1, card);
        }

        [Fact]
        public void ValidateDiscard_NotTurnOwner_ThrowsNotYourTurnException()
        {
            var card = new CardsGame("Oros", 1);
            var exception = Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateDiscard(1, 2, card));
            Assert.Equal(ServiceErrorType.NotYourTurn, exception.ErrorType);
        }

        [Fact]
        public void ValidateDiscard_CardNull_ThrowsGameRuleViolationException()
        {
            var exception = Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateDiscard(1, 1, null));
            Assert.Equal(ServiceErrorType.GameRuleViolation, exception.ErrorType);
        }

        [Fact]
        public void ValidateSwap_ValidContext_ReturnsVoid()
        {
            var context = new SwapValidationContext
            {
                PlayerId = 1,
                CurrentTurnPlayerId = 1,
                IsCardDrawnFromDeck = true,
                PlayerReviewingDiscardId = 1,
                MustDiscardToFinishTurn = false,
                CardToDiscard = new CardsGame("Copas", 1),
                DiscardPileCount = 5
            };
            GameValidator.ValidateSwap(context);
        }

        [Fact]
        public void ValidateSwap_NotTurnOwner_ThrowsNotYourTurnException()
        {
            var context = new SwapValidationContext { PlayerId = 1, CurrentTurnPlayerId = 2 };
            var exception = Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateSwap(context));
            Assert.Equal(ServiceErrorType.NotYourTurn, exception.ErrorType);
        }

        [Fact]
        public void ValidateSwap_NotDrawnFromDeck_ThrowsInvalidCardActionException()
        {
            var context = new SwapValidationContext
            {
                PlayerId = 1,
                CurrentTurnPlayerId = 1,
                IsCardDrawnFromDeck = false
            };
            var exception = Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateSwap(context));
            Assert.Equal(ServiceErrorType.InvalidCardAction, exception.ErrorType);
        }

        [Fact]
        public void ValidateSwap_PlayerNotReviewingDiscard_ThrowsInvalidCardActionException()
        {
            var context = new SwapValidationContext
            {
                PlayerId = 1,
                CurrentTurnPlayerId = 1,
                IsCardDrawnFromDeck = true,
                PlayerReviewingDiscardId = 2
            };
            var exception = Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateSwap(context));
            Assert.Equal(ServiceErrorType.InvalidCardAction, exception.ErrorType);
        }

        [Fact]
        public void ValidateSwap_MustDiscardToFinish_ThrowsMustDiscardToFinishException()
        {
            var context = new SwapValidationContext
            {
                PlayerId = 1,
                CurrentTurnPlayerId = 1,
                IsCardDrawnFromDeck = true,
                PlayerReviewingDiscardId = 1,
                MustDiscardToFinishTurn = true
            };
            var exception = Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateSwap(context));
            Assert.Equal(ServiceErrorType.MustDiscardToFinish, exception.ErrorType);
        }

        [Fact]
        public void ValidateSwap_CardToDiscardNull_ThrowsGameRuleViolationException()
        {
            var context = new SwapValidationContext
            {
                PlayerId = 1,
                CurrentTurnPlayerId = 1,
                IsCardDrawnFromDeck = true,
                PlayerReviewingDiscardId = 1,
                MustDiscardToFinishTurn = false,
                CardToDiscard = null
            };
            var exception = Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateSwap(context));
            Assert.Equal(ServiceErrorType.GameRuleViolation, exception.ErrorType);
        }

        [Fact]
        public void ValidateSwap_DiscardPileEmpty_ThrowsEmptyDiscaardException()
        {
            var context = new SwapValidationContext
            {
                PlayerId = 1,
                CurrentTurnPlayerId = 1,
                IsCardDrawnFromDeck = true,
                PlayerReviewingDiscardId = 1,
                MustDiscardToFinishTurn = false,
                CardToDiscard = new CardsGame("Oros", 1),
                DiscardPileCount = 0
            };
            var exception = Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateSwap(context));
            Assert.Equal(ServiceErrorType.EmptyDiscaard, exception.ErrorType);
        }

        [Fact]
        public void IsValidMeldCombination_NullList_ReturnsFalse()
        {
            bool result = GameValidator.IsValidMeldCombination(null);
            Assert.False(result);
        }

        [Fact]
        public void IsValidMeldCombination_ListTooSmall_ReturnsFalse()
        {
            var cards = new List<CardsGame>
            {
                new CardsGame("Oros", 1),
                new CardsGame("Copas", 1)
            };
            bool result = GameValidator.IsValidMeldCombination(cards);
            Assert.False(result);
        }

        [Fact]
        public void IsValidMeldCombination_ValidTercia_ReturnsTrue()
        {
            var cards = new List<CardsGame>
            {
                new CardsGame("Oros", 1),
                new CardsGame("Copas", 1),
                new CardsGame("Espadas", 1)
            };
            bool result = GameValidator.IsValidMeldCombination(cards);
            Assert.True(result);
        }

        [Fact]
        public void IsValidMeldCombination_InvalidTercia_DuplicateSuits_ReturnsFalse()
        {
            var cards = new List<CardsGame>
            {
                new CardsGame("Oros", 1),
                new CardsGame("Oros", 1),
                new CardsGame("Espadas", 1)
            };
            bool result = GameValidator.IsValidMeldCombination(cards);
            Assert.False(result);
        }

        [Fact]
        public void IsValidMeldCombination_ValidCorrida_ReturnsTrue()
        {
            var cards = new List<CardsGame>
            {
                new CardsGame("Oros", 1),
                new CardsGame("Oros", 2),
                new CardsGame("Oros", 3)
            };
            bool result = GameValidator.IsValidMeldCombination(cards);
            Assert.True(result);
        }

        [Fact]
        public void IsValidMeldCombination_ValidCorridaWithJump_ReturnsTrue()
        {
            var cards = new List<CardsGame>
            {
                new CardsGame("Espadas", 6),
                new CardsGame("Espadas", 7),
                new CardsGame("Espadas", 10)
            };
            bool result = GameValidator.IsValidMeldCombination(cards);
            Assert.True(result);
        }

        [Fact]
        public void IsValidMeldCombination_InvalidCorrida_MixedSuits_ReturnsFalse()
        {
            var cards = new List<CardsGame>
            {
                new CardsGame("Oros", 1),
                new CardsGame("Copas", 2),
                new CardsGame("Oros", 3)
            };
            bool result = GameValidator.IsValidMeldCombination(cards);
            Assert.False(result);
        }

        [Fact]
        public void IsValidMeldCombination_InvalidCorrida_GapInRanks_ReturnsFalse()
        {
            var cards = new List<CardsGame>
            {
                new CardsGame("Oros", 1),
                new CardsGame("Oros", 3),
                new CardsGame("Oros", 4)
            };
            bool result = GameValidator.IsValidMeldCombination(cards);
            Assert.False(result);
        }
    }
}
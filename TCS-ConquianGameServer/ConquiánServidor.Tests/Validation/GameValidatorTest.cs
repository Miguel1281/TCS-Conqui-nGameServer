using Xunit;
using ConquiánServidor.BusinessLogic.Validation;
using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.BusinessLogic.Game;
using System.Collections.Generic;

namespace ConquiánServidor.Tests.BusinessLogic.Validation
{
    public class GameValidatorTest
    {
        [Fact]
        public void ValidateTurnOwner_IdsMatch_Void()
        {
            GameValidator.ValidateTurnOwner(1, 1);
        }

        [Fact]
        public void ValidateTurnOwner_IdsDoNotMatch_ThrowsBusinessLogicException()
        {
            Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateTurnOwner(1, 2));
        }

        [Fact]
        public void ValidateActionAllowed_MustDiscardFalse_Void()
        {
            GameValidator.ValidateActionAllowed(false);
        }

        [Fact]
        public void ValidateActionAllowed_MustDiscardTrue_ThrowsBusinessLogicException()
        {
            Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateActionAllowed(true));
        }

        [Fact]
        public void ValidateMoveInputs_NullList_ThrowsBusinessLogicException()
        {
            Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateMoveInputs(null));
        }

        [Fact]
        public void ValidateMoveInputs_EmptyList_ThrowsBusinessLogicException()
        {
            Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateMoveInputs(new List<string>()));
        }

        [Fact]
        public void ValidateMoveInputs_OneCard_ThrowsBusinessLogicException()
        {
            Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateMoveInputs(new List<string> { "card1" }));
        }

        [Fact]
        public void ValidateMoveInputs_TwoCards_Void()
        {
            GameValidator.ValidateMoveInputs(new List<string> { "card1", "card2" });
        }

        [Fact]
        public void ValidateDiscardUsage_PlayerIsReviewing_Void()
        {
            GameValidator.ValidateDiscardUsage(1, 1, false);
        }

        [Fact]
        public void ValidateDiscardUsage_CardDrawnFromDeck_Void()
        {
            GameValidator.ValidateDiscardUsage(1, 2, true);
        }

        [Fact]
        public void ValidateDiscardUsage_NotReviewingAndNotDrawnFromDeck_ThrowsBusinessLogicException()
        {
            Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateDiscardUsage(1, 2, false));
        }

        [Fact]
        public void ValidateCardsInHand_CountsMatch_Void()
        {
            GameValidator.ValidateCardsInHand(3, 3);
        }

        [Fact]
        public void ValidateCardsInHand_CountsDoNotMatch_ThrowsBusinessLogicException()
        {
            Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateCardsInHand(3, 4));
        }

        [Fact]
        public void ValidateMeldSize_CountIsThree_Void()
        {
            GameValidator.ValidateMeldSize(3);
        }

        [Fact]
        public void ValidateMeldSize_CountIsLessThanThree_ThrowsBusinessLogicException()
        {
            Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateMeldSize(2));
        }

        [Fact]
        public void ValidateDraw_ValidContext_Void()
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
        public void ValidateDraw_WrongTurn_ThrowsBusinessLogicException()
        {
            var context = new DrawValidationContext
            {
                PlayerId = 1,
                CurrentTurnPlayerId = 2
            };

            Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateDraw(context));
        }

        [Fact]
        public void ValidateDraw_AlreadyDrawn_ThrowsBusinessLogicException()
        {
            var context = new DrawValidationContext
            {
                PlayerId = 1,
                CurrentTurnPlayerId = 1,
                IsCardDrawnFromDeck = true
            };

            Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateDraw(context));
        }

        [Fact]
        public void ValidateDraw_PendingDiscardAction_ThrowsBusinessLogicException()
        {
            var context = new DrawValidationContext
            {
                PlayerId = 1,
                CurrentTurnPlayerId = 1,
                IsCardDrawnFromDeck = false,
                MustDiscardToFinishTurn = false,
                PlayerReviewingDiscardId = 1
            };

            Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateDraw(context));
        }

        [Fact]
        public void ValidateDraw_StockEmpty_ThrowsBusinessLogicException()
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

            Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateDraw(context));
        }

        [Fact]
        public void ValidateDiscard_Valid_Void()
        {
            GameValidator.ValidateDiscard(1, 1, new Card("Oros", 1));
        }

        [Fact]
        public void ValidateDiscard_NullCard_ThrowsBusinessLogicException()
        {
            Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateDiscard(1, 1, null));
        }

        [Fact]
        public void ValidateSwap_ValidContext_Void()
        {
            var context = new SwapValidationContext
            {
                PlayerId = 1,
                CurrentTurnPlayerId = 1,
                IsCardDrawnFromDeck = true,
                PlayerReviewingDiscardId = 1,
                MustDiscardToFinishTurn = false,
                CardToDiscard = new Card("Oros", 1),
                DiscardPileCount = 1
            };

            GameValidator.ValidateSwap(context);
        }

        [Fact]
        public void ValidateSwap_NotDrawnFromDeck_ThrowsBusinessLogicException()
        {
            var context = new SwapValidationContext
            {
                PlayerId = 1,
                CurrentTurnPlayerId = 1,
                IsCardDrawnFromDeck = false
            };

            Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateSwap(context));
        }

        [Fact]
        public void ValidateSwap_ReviewingIdMismatch_ThrowsBusinessLogicException()
        {
            var context = new SwapValidationContext
            {
                PlayerId = 1,
                CurrentTurnPlayerId = 1,
                IsCardDrawnFromDeck = true,
                PlayerReviewingDiscardId = 2
            };

            Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateSwap(context));
        }

        [Fact]
        public void ValidateSwap_DiscardPileEmpty_ThrowsBusinessLogicException()
        {
            var context = new SwapValidationContext
            {
                PlayerId = 1,
                CurrentTurnPlayerId = 1,
                IsCardDrawnFromDeck = true,
                PlayerReviewingDiscardId = 1,
                MustDiscardToFinishTurn = false,
                CardToDiscard = new Card("Oros", 1),
                DiscardPileCount = 0
            };

            Assert.Throws<BusinessLogicException>(() => GameValidator.ValidateSwap(context));
        }

        [Fact]
        public void IsValidMeldCombination_NullList_ReturnsFalse()
        {
            var result = GameValidator.IsValidMeldCombination(null);
            Assert.False(result);
        }

        [Fact]
        public void IsValidMeldCombination_ValidTercia_ReturnsTrue()
        {
            var cards = new List<Card>
            {
                new Card("Oros", 1),
                new Card("Copas", 1),
                new Card("Espadas", 1)
            };
            var result = GameValidator.IsValidMeldCombination(cards);
            Assert.True(result);
        }

        [Fact]
        public void IsValidMeldCombination_InvalidTerciaSameSuit_ReturnsFalse()
        {
            var cards = new List<Card>
            {
                new Card("Oros", 1),
                new Card("Oros", 1),
                new Card("Espadas", 1)
            };
            var result = GameValidator.IsValidMeldCombination(cards);
            Assert.False(result);
        }

        [Fact]
        public void IsValidMeldCombination_ValidCorrida_ReturnsTrue()
        {
            var cards = new List<Card>
            {
                new Card("Oros", 1),
                new Card("Oros", 2),
                new Card("Oros", 3)
            };
            var result = GameValidator.IsValidMeldCombination(cards);
            Assert.True(result);
        }

        [Fact]
        public void IsValidMeldCombination_ValidCorridaWithJump_ReturnsTrue()
        {
            var cards = new List<Card>
            {
                new Card("Oros", 6),
                new Card("Oros", 7),
                new Card("Oros", 10)
            };
            var result = GameValidator.IsValidMeldCombination(cards);
            Assert.True(result);
        }

        [Fact]
        public void IsValidMeldCombination_InvalidCorridaMixedSuits_ReturnsFalse()
        {
            var cards = new List<Card>
            {
                new Card("Oros", 1),
                new Card("Copas", 2),
                new Card("Oros", 3)
            };
            var result = GameValidator.IsValidMeldCombination(cards);
            Assert.False(result);
        }

        [Fact]
        public void IsValidMeldCombination_InvalidCorridaNotSequential_ReturnsFalse()
        {
            var cards = new List<Card>
            {
                new Card("Oros", 1),
                new Card("Oros", 3),
                new Card("Oros", 4)
            };
            var result = GameValidator.IsValidMeldCombination(cards);
            Assert.False(result);
        }
    }
}
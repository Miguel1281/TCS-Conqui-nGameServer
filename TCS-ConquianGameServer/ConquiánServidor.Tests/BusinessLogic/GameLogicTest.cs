using Xunit;
using Moq;
using ConquiánServidor.BusinessLogic.Game;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.BusinessLogic.Exceptions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConquiánServidor.Tests.BusinessLogic
{
    public class GameLogicTest
    {
        private readonly Mock<IGameCallback> mockCallback1;
        private readonly Mock<IGameCallback> mockCallback2;
        private readonly List<PlayerDto> players;
        private const string ROOM_CODE = "TEST1";
        private const int GAMEMODE_CLASSIC = 1;

        public GameLogicTest()
        {
            mockCallback1 = new Mock<IGameCallback>();
            mockCallback2 = new Mock<IGameCallback>();
            players = new List<PlayerDto>
            {
                new PlayerDto { idPlayer = 1, nickname = "Player1", pathPhoto = "photo1" },
                new PlayerDto { idPlayer = 2, nickname = "Player2", pathPhoto = "photo2" }
            };
        }

        private GameLogic CreateGame()
        {
            var game = new GameLogic(ROOM_CODE, GAMEMODE_CLASSIC, players);
            game.RegisterPlayerCallback(1, mockCallback1.Object);
            game.RegisterPlayerCallback(2, mockCallback2.Object);
            return game;
        }

        private void TransitionToDrawPhase(GameLogic game)
        {
            int p1 = players[0].idPlayer;
            game.PassTurn(p1);

            int p2 = players[1].idPlayer;
            game.PassTurn(p2);
        }

        [Fact]
        public void Constructor_Initialization_DealsCorrectCardsToPlayer1()
        {
            var game = CreateGame();
            Assert.Equal(6, game.PlayerHands[1].Count);
        }

        [Fact]
        public void Constructor_Initialization_DealsCorrectCardsToPlayer2()
        {
            var game = CreateGame();
            Assert.Equal(6, game.PlayerHands[2].Count);
        }

        [Fact]
        public void Constructor_Initialization_CreatesSingleCardDiscardPile()
        {
            var game = CreateGame();
            Assert.Single(game.DiscardPile);
        }

        [Fact]
        public void DrawFromDeck_NotTurnOwner_ThrowsBusinessLogicException()
        {
            var game = CreateGame();
            TransitionToDrawPhase(game);
            int turnPlayer = game.GetCurrentTurnPlayerId();
            int notTurnPlayer = players.First(p => p.idPlayer != turnPlayer).idPlayer;

            Assert.Throws<BusinessLogicException>(() => game.DrawFromDeck(notTurnPlayer));
        }

        [Fact]
        public void DrawFromDeck_AlreadyDrawn_ThrowsBusinessLogicException()
        {
            var game = CreateGame();
            TransitionToDrawPhase(game);
            int turnPlayer = game.GetCurrentTurnPlayerId();
            game.DrawFromDeck(turnPlayer);

            Assert.Throws<BusinessLogicException>(() => game.DrawFromDeck(turnPlayer));
        }

        [Fact]
        public void DrawFromDeck_DeckEmpty_FinishesGame()
        {
            var game = CreateGame();
            TransitionToDrawPhase(game);
            int turnPlayer = game.GetCurrentTurnPlayerId();
            while (game.StockPile.Count > 0)
            {
                game.StockPile.RemoveAt(0);
            }
            bool gameFinished = false;
            game.OnGameFinished += (result) => gameFinished = true;

            try { game.DrawFromDeck(turnPlayer); } catch { }

            Assert.True(gameFinished);
        }

        [Fact]
        public void DiscardCard_NotTurnOwner_ThrowsBusinessLogicException()
        {
            var game = CreateGame();
            int notTurnPlayer = 2;
            var card = game.PlayerHands[notTurnPlayer].First();

            Assert.Throws<BusinessLogicException>(() => game.DiscardCard(notTurnPlayer, card.Id));
        }

        [Fact]
        public void DiscardCard_CardNotInHand_ThrowsBusinessLogicException()
        {
            var game = CreateGame();
            int turnPlayer = game.GetCurrentTurnPlayerId();
            string fakeCardId = "invalid_id";

            Assert.Throws<BusinessLogicException>(() => game.DiscardCard(turnPlayer, fakeCardId));
        }

        [Fact]
        public void DiscardCard_Success_ChangesTurn()
        {
            var game = CreateGame();
            TransitionToDrawPhase(game);
            int turnPlayer = game.GetCurrentTurnPlayerId();
            game.DrawFromDeck(turnPlayer);
            var card = game.PlayerHands[turnPlayer].First();
            int initialTurn = turnPlayer;

            game.DiscardCard(turnPlayer, card.Id);

            Assert.NotEqual(initialTurn, game.GetCurrentTurnPlayerId());
        }

        [Fact]
        public void DiscardCard_Success_AddsCardToDiscardPile()
        {
            var game = CreateGame();
            TransitionToDrawPhase(game);
            int turnPlayer = game.GetCurrentTurnPlayerId();
            game.DrawFromDeck(turnPlayer);
            var card = game.PlayerHands[turnPlayer].First();

            game.DiscardCard(turnPlayer, card.Id);

            Assert.Contains(game.DiscardPile, c => c.Id == card.Id);
        }

        [Fact]
        public void ProcessPlayerMove_InvalidMeld_ThrowsBusinessLogicException()
        {
            var game = CreateGame();
            TransitionToDrawPhase(game);
            int turnPlayer = game.GetCurrentTurnPlayerId();
            var cardIds = new List<string> { game.PlayerHands[turnPlayer].First().Id };

            Assert.Throws<BusinessLogicException>(() => game.ProcessPlayerMove(turnPlayer, cardIds));
        }

        [Fact]
        public void ProcessPlayerMove_NotYourTurn_ThrowsBusinessLogicException()
        {
            var game = CreateGame();
            int notTurnPlayer = 2;
            var cardIds = game.PlayerHands[notTurnPlayer].Take(3).Select(c => c.Id).ToList();

            Assert.Throws<BusinessLogicException>(() => game.ProcessPlayerMove(notTurnPlayer, cardIds));
        }

        [Fact]
        public void SwapDrawnCard_NotDrawnFromDeck_ThrowsBusinessLogicException()
        {
            var game = CreateGame();
            TransitionToDrawPhase(game);
            int turnPlayer = game.GetCurrentTurnPlayerId();
            var cardToDiscard = game.PlayerHands[turnPlayer].First();

            Assert.Throws<BusinessLogicException>(() => game.SwapDrawnCard(turnPlayer, cardToDiscard.Id));
        }

        [Fact]
        public void SwapDrawnCard_CardNotInHand_ThrowsBusinessLogicException()
        {
            var game = CreateGame();
            TransitionToDrawPhase(game);
            int turnPlayer = game.GetCurrentTurnPlayerId();
            game.DrawFromDeck(turnPlayer);

            Assert.Throws<BusinessLogicException>(() => game.SwapDrawnCard(turnPlayer, "fake_id"));
        }

        [Fact]
        public void PassTurn_HostPassesInitialDiscard_ChangesTurn()
        {
            var game = CreateGame();
            int hostId = 1;

            game.PassTurn(hostId);

            Assert.Equal(2, game.GetCurrentTurnPlayerId());
        }

        [Fact]
        public void PassTurn_NotTurnOwner_DoesNothing()
        {
            var game = CreateGame();
            int notTurnPlayer = 2;
            int currentTurn = game.GetCurrentTurnPlayerId();

            game.PassTurn(notTurnPlayer);

            Assert.Equal(currentTurn, game.GetCurrentTurnPlayerId());
        }

        [Fact]
        public async Task NotifyGameEndedByAbandonment_PlayerAbandons_CallsOnOpponentLeft()
        {
            var game = CreateGame();
            int leaverId = 1;

            game.NotifyGameEndedByAbandonment(leaverId);
            await Task.Delay(100);

            mockCallback2.Verify(c => c.OnOpponentLeft(), Times.Once);
        }

        [Fact]
        public async Task ProcessAFK_NotifiesRival()
        {
            var game = CreateGame();
            int afkPlayerId = 1;

            game.ProcessAFK(afkPlayerId);
            await Task.Delay(200);

            mockCallback2.Verify(c => c.NotifyGameEndedByAFK(It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Fact]
        public void GetInitialTimeInSeconds_ClassicMode_Returns600()
        {
            var game = new GameLogic(ROOM_CODE, 1, players);
            int time = game.GetInitialTimeInSeconds();
            Assert.Equal(600, time);
        }

        [Fact]
        public void GetInitialTimeInSeconds_ExtendedMode_Returns1200()
        {
            var game = new GameLogic(ROOM_CODE, 2, players);
            int time = game.GetInitialTimeInSeconds();
            Assert.Equal(1200, time);
        }

        [Fact]
        public void DrawFromDeck_Success_UpdatesDiscardPile()
        {
            var game = CreateGame();
            TransitionToDrawPhase(game);
            int turnPlayer = game.GetCurrentTurnPlayerId();
            var cardToDraw = game.StockPile[0];

            game.DrawFromDeck(turnPlayer);

            Assert.Equal(cardToDraw.Id, game.DiscardPile.Last().Id);
        }

        [Fact]
        public void SwapDrawnCard_Success_UpdatesHand()
        {
            var game = CreateGame();
            TransitionToDrawPhase(game);
            int turnPlayer = game.GetCurrentTurnPlayerId();
            var cardToDraw = game.StockPile[0];
            game.DrawFromDeck(turnPlayer);
            var cardToDiscard = game.PlayerHands[turnPlayer].First();

            game.SwapDrawnCard(turnPlayer, cardToDiscard.Id);

            Assert.Contains(game.PlayerHands[turnPlayer], c => c.Id == cardToDraw.Id);
        }

        [Fact]
        public void SwapDrawnCard_Success_ChangesTurn()
        {
            var game = CreateGame();
            TransitionToDrawPhase(game);
            int turnPlayer = game.GetCurrentTurnPlayerId();
            game.DrawFromDeck(turnPlayer);
            var cardToDiscard = game.PlayerHands[turnPlayer].First();
            int initialTurn = turnPlayer;

            game.SwapDrawnCard(turnPlayer, cardToDiscard.Id);

            Assert.NotEqual(initialTurn, game.GetCurrentTurnPlayerId());
        }

        [Fact]
        public void ProcessPlayerMove_ValidMeld_AddsMeldToPlayer()
        {
            var game = CreateGame();
            TransitionToDrawPhase(game);
            int turnPlayer = game.GetCurrentTurnPlayerId();
            game.PlayerHands[turnPlayer].Clear();
            game.PlayerHands[turnPlayer].Add(new CardsGame("Oros", 1));
            game.PlayerHands[turnPlayer].Add(new CardsGame("Copas", 1));
            game.PlayerHands[turnPlayer].Add(new CardsGame("Espadas", 1));
            var cardIds = new List<string> { "Oros_1", "Copas_1", "Espadas_1" };

            game.ProcessPlayerMove(turnPlayer, cardIds);

            Assert.Single(game.PlayerMelds[turnPlayer]);
        }

        [Fact]
        public void ProcessPlayerMove_ValidMeld_RemovesCardsFromHand()
        {
            var game = CreateGame();
            TransitionToDrawPhase(game);
            int turnPlayer = game.GetCurrentTurnPlayerId();
            game.PlayerHands[turnPlayer].Clear();
            game.PlayerHands[turnPlayer].Add(new CardsGame("Oros", 1));
            game.PlayerHands[turnPlayer].Add(new CardsGame("Copas", 1));
            game.PlayerHands[turnPlayer].Add(new CardsGame("Espadas", 1));
            var cardIds = new List<string> { "Oros_1", "Copas_1", "Espadas_1" };

            game.ProcessPlayerMove(turnPlayer, cardIds);

            Assert.Empty(game.PlayerHands[turnPlayer]);
        }

        [Fact]
        public void ProcessPlayerMove_WinningCondition_FinishesGame()
        {
            var game = CreateGame();
            TransitionToDrawPhase(game);
            int turnPlayer = game.GetCurrentTurnPlayerId();
            bool gameFinished = false;
            game.OnGameFinished += (result) => gameFinished = true;
            game.PlayerMelds[turnPlayer].Add(new List<CardsGame> { new CardsGame("Bastos", 10), new CardsGame("Bastos", 11), new CardsGame("Bastos", 12) });
            game.PlayerHands[turnPlayer].Clear();
            game.PlayerHands[turnPlayer].Add(new CardsGame("Oros", 1));
            game.PlayerHands[turnPlayer].Add(new CardsGame("Copas", 1));
            game.PlayerHands[turnPlayer].Add(new CardsGame("Espadas", 1));
            var cardIds = new List<string> { "Oros_1", "Copas_1", "Espadas_1" };

            game.ProcessPlayerMove(turnPlayer, cardIds);

            Assert.True(gameFinished);
        }

        [Fact]
        public void PassTurn_RivalPassesDiscard_DoesNotChangeTurn()
        {
            var game = CreateGame();
            int hostId = players[0].idPlayer;
            int rivalId = players[1].idPlayer;
            game.PassTurn(hostId);
            int currentTurn = game.GetCurrentTurnPlayerId();

            game.PassTurn(rivalId);

            Assert.Equal(currentTurn, game.GetCurrentTurnPlayerId());
        }

        [Fact]
        public void PassTurn_AfterDrawing_ChangesTurn()
        {
            var game = CreateGame();
            TransitionToDrawPhase(game);
            int turnPlayer = game.GetCurrentTurnPlayerId();
            game.DrawFromDeck(turnPlayer);
            int initialTurn = turnPlayer;

            game.PassTurn(turnPlayer);

            Assert.NotEqual(initialTurn, game.GetCurrentTurnPlayerId());
        }
    }
}
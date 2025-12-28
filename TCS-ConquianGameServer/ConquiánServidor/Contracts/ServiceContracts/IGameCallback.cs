using ConquiánServidor.Contracts.DataContracts;
using System.ServiceModel;
namespace ConquiánServidor.Contracts.ServiceContracts
{
    [ServiceContract]
    public interface IGameCallback
    {
        [OperationContract(IsOneWay = true)]
        void NotifyGameUpdate(GameStateDto newState); 

        [OperationContract(IsOneWay = true)]
        void NotifyOpponentDrewDeck();

        [OperationContract(IsOneWay = true)]
        void NotifyOpponentDiscarded(CardDto card);

        [OperationContract(IsOneWay = true)]
        void OnTimeUpdated(int gameRemainingSeconds, int turnRemainingSeconds, int currentTurnPlayerId);

        [OperationContract(IsOneWay = true)]
        void OnOpponentHandUpdated(int newCardCount);

        [OperationContract(IsOneWay = true)]
        void NotifyOpponentMeld(CardDto[] meldCards);

        [OperationContract(IsOneWay = true)]
        void OnOpponentLeft();

        [OperationContract(IsOneWay = true)]
        void NotifyGameEnded(GameResultDto result);

        [OperationContract(IsOneWay = true)]
        void NotifyGameEndedByAFK(string reasonKey);
    }
}
using ConquiánServidor.Contracts.DataContracts;
using System.ServiceModel;
using System.Threading.Tasks;
namespace ConquiánServidor.Contracts.ServiceContracts
{
    [ServiceContract(CallbackContract = typeof(IGameCallback))]
    public interface IGame
    {
        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        Task<GameStateDto> JoinGameAsync(string roomCode, int playerId);

        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        Task PlayCardsAsync(string roomCode, int playerId, string[] cardIds);

        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        Task DrawFromDeckAsync(string roomCode, int playerId);

        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        Task DiscardCardAsync(string roomCode, int playerId, string cardId);

        [OperationContract(IsOneWay = true)] 
        void LeaveGame(string roomCode, int playerId);

        [OperationContract]
        Task PassTurnAsync(string roomCode, int playerId);

        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        Task SwapDrawnCardAsync(string roomCode, int playerId, string cardIdToDiscard);
    }
}
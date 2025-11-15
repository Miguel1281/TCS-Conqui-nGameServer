using ConquiánServidor.Contracts.DataContracts;
using System.ServiceModel;
using System.Threading.Tasks;
namespace ConquiánServidor.Contracts.ServiceContracts
{
    [ServiceContract(CallbackContract = typeof(IGameCallback))]
    public interface IGame
    {
        [OperationContract]
        Task<GameStateDto> JoinGameAsync(string roomCode, int playerId);

        [OperationContract(IsOneWay = true)]
        void PlayCards(string roomCode, int playerId, string[] cardIds);

        [OperationContract(IsOneWay = true)]
        void DrawFromDeck(string roomCode, int playerId);

        [OperationContract]
        Task<CardDto> DrawFromDiscardAsync(string roomCode, int playerId);

        [OperationContract(IsOneWay = true)]
        void DiscardCard(string roomCode, int playerId, string cardId);
    }
}
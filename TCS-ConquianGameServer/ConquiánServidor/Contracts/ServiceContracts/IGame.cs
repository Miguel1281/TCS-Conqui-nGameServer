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

    }
}
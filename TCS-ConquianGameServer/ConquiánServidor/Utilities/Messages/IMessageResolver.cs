using ConquiánServidor.Contracts.DataContracts;

namespace ConquiánServidor.Utilities.Messages
{
    public interface IMessageResolver
    {
        string GetMessage(ServiceErrorType errorType);
    }
}

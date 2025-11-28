using ConquiánServidor.Contracts.DataContracts;
using System;

namespace ConquiánServidor.BusinessLogic.Exceptions
{
    public class BusinessLogicException : Exception
    {
        public ServiceErrorType ErrorType { get; }

        public BusinessLogicException(ServiceErrorType errorType)
            : base(errorType.ToString())
        {
            ErrorType = errorType;
        }

        public BusinessLogicException(ServiceErrorType errorType, string message)
            : base(message)
        {
            ErrorType = errorType;
        }
    }
}

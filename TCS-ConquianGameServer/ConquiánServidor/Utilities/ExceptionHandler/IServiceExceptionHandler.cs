using ConquiánServidor.Contracts.DataContracts;
using System;
using System.ServiceModel;

namespace ConquiánServidor.Utilities.ExceptionHandler
{
    public interface IServiceExceptionHandler
    {
        FaultException<ServiceFaultDto> HandleException(Exception exception, string operationContext);
    }
}

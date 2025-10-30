using System;
using System.Runtime.Serialization;

namespace ConquiánServidor.Contracts.FaultContracts 
{
    [DataContract]
    public class SessionActiveFault
    {
        [DataMember]
        public string Message { get; set; }

        public SessionActiveFault(string message)
        {
            this.Message = message;
        }
    }
}
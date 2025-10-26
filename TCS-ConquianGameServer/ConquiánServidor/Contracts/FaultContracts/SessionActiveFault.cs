using System;
using System.Runtime.Serialization;

namespace ConquiánServidor.Contracts.FaultContracts 
{
    [DataContract]
    public class SessionActiveFault
    {
        private string message;

        [DataMember]
        public string Message
        {
            get { return message; }
            set { message = value; }
        }

        public SessionActiveFault(string message)
        {
            this.message = message;
        }
    }
}
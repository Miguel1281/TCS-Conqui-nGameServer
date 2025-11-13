using System.Runtime.Serialization;

namespace ConquiánServidor.Contracts.FaultContracts
{
    [DataContract]
    public class RegisteredUserAsGuestFault
    {
        [DataMember]
        public string Message { get; set; }
    }
}
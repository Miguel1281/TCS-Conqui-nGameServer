using System.Runtime.Serialization;

namespace ConquiánServidor.Contracts.FaultContracts
{
    [DataContract]
    public class GuestInviteUsedFault
    {
        [DataMember]
        public string Message { get; set; }
    }
}
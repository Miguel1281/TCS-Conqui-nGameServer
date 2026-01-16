using System.Runtime.Serialization;

namespace ConquiánServidor.Contracts.DataContracts
{
    [DataContract]
    public class InvitationSenderDto
    {
        [DataMember]
        public int IdPlayer { get; set; }

        [DataMember]
        public string Nickname { get; set; }
    }
}

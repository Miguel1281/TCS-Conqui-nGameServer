using System.Runtime.Serialization;

namespace ConquiánServidor.Contracts.Enums
{
    [DataContract(Name = "PlayerStatus")]
    public enum PlayerStatus
    {
        [EnumMember]
        Online = 1,

        [EnumMember]
        Offline = 2,
    }
}

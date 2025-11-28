using System.Runtime.Serialization;

namespace ConquiánServidor.Contracts.Enums
{
    [DataContract(Name = "LobbyStatus")]
    public enum LobbyStatus
    {
        [EnumMember]
        Waiting = 1,

        [EnumMember]
        InGame = 2,

        [EnumMember]
        Finished = 3
    }
}

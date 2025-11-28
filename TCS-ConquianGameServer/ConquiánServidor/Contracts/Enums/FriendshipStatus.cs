using System.Runtime.Serialization;

namespace ConquiánServidor.Contracts.Enums
{
    [DataContract(Name = "FriendshipStatus")]
    public enum FriendshipStatus
    {
        [EnumMember]
        Accepted = 1,

        [EnumMember]
        Pending = 3 
    }
}

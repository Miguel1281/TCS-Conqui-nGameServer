using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ConquiánServidor.Contracts.DataContracts
{
    [DataContract(Name = "ServiceErrorType")]
    public enum ServiceErrorType
    {
        [EnumMember]
        Unknown = 0,

        [EnumMember]
        DatabaseError = 1,

        [EnumMember]
        ValidationFailed = 3,

        [EnumMember]
        NotFound = 4,

        [EnumMember]
        OperationFailed = 5,

        [EnumMember]
        SessionActive = 10,

        [EnumMember]
        GuestInviteUsed = 11,

        [EnumMember]
        RegisteredUserAsGuest = 12,

        [EnumMember]
        LobbyFull = 13,

        [EnumMember]
        GameInProgress = 14
    }
}

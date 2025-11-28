using System.Runtime.Serialization;

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
        DuplicateRecord = 2,

        [EnumMember] 
        ValidationFailed = 3,

        [EnumMember] 
        NotFound = 4,

        [EnumMember] 
        OperationFailed = 5,

        [EnumMember] 
        CommunicationError = 6,

        [EnumMember] 
        ServerInternalError = 7,

        [EnumMember] 
        UserNotFound = 8,

        [EnumMember] 
        InvalidPassword = 9,

        [EnumMember] 
        SessionActive = 10,

        [EnumMember] 
        GuestInviteUsed = 11,

        [EnumMember] 
        RegisteredUserAsGuest = 12,

        [EnumMember] 
        LobbyFull = 13,

        [EnumMember] 
        GameInProgress = 14,

        [EnumMember] 
        InvalidEmailFormat = 15,

        [EnumMember] 
        InvalidPasswordFormat = 16,

        [EnumMember] 
        InvalidNameFormat = 17,

        [EnumMember] 
        InvalidVerificationCode = 18,

        [EnumMember] 
        VerificationCodeExpired = 19
    }
}

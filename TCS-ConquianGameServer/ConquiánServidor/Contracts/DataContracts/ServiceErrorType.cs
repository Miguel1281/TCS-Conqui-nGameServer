using System.Data.Entity.Core.Metadata.Edm;
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
        VerificationCodeExpired = 19,

        [EnumMember]
        GameNotFound = 20,

        [EnumMember]
        NotYourTurn = 21,

        [EnumMember]
        MustDiscardToFinish = 22,

        [EnumMember]
        AlreadyDrawn = 23,

        [EnumMember]
        PendingDiscardAction = 24,

        [EnumMember]
        DeckEmpty = 25,

        [EnumMember]
        InvalidMeld = 26,

        [EnumMember]
        CardNotFound = 27,

        [EnumMember]
        InvalidCardAction = 28,

        [EnumMember]
        GameRuleViolation = 29,

        [EnumMember]
        EmptyDiscaard = 30,

        [EnumMember]
        LobbyNotFound = 31,

        [EnumMember]
        UserOffline = 32,

        [EnumMember]
        InvitationFailed = 33,

        [EnumMember]
        ExistingRequest = 34,

        [EnumMember]
        HostUserNotFound = 35,

        [EnumMember]
        NotEnoughPlayers = 36,

        [EnumMember]
        NotLobbyHost = 37,

        [EnumMember]
        NotKickYourSelf = 38,

        [EnumMember]
        RegisteredMail = 39,

        [EnumMember]
        UserInGame = 40,
    }
}

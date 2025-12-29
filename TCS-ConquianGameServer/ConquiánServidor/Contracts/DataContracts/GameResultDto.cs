using System.Runtime.Serialization;

namespace ConquiánServidor.Contracts.DataContracts
{
    [DataContract]
    public class GameResultDto
    {
        [DataMember]
        public int WinnerId { get; set; }

        [DataMember]
        public int LoserId { get; set; }

        [DataMember]
        public int PointsWon { get; set; }

        [DataMember]
        public bool IsDraw { get; set; }

        [DataMember]
        public int GamemodeId { get; set; }

        [DataMember]
        public int Player1Id { get; set; }

        [DataMember]
        public string Player1Name { get; set; }

        [DataMember]
        public int Player1Score { get; set; }

        [DataMember]
        public int Player2Id { get; set; }

        [DataMember]
        public string Player2Name { get; set; }

        [DataMember]
        public int Player2Score { get; set; }

        [DataMember]
        public int DurationSeconds { get; set; }

        [DataMember]
        public string RoomCode { get; set; }
    }
}
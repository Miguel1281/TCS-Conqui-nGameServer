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
    }
}
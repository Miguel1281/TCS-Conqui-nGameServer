using System.Runtime.Serialization;

namespace ConquiánServidor.Contracts.DataContracts
{
    [DataContract]
    public class GameHistoryDto
    {
        [DataMember]
        public string OpponentName { get; set; }

        [DataMember]
        public string ResultStatus { get; set; }

        [DataMember]
        public int PointsEarned { get; set; }

        [DataMember]
        public string GameTime { get; set; }

        [DataMember]
        public string GameMode { get; set; }
    }
}

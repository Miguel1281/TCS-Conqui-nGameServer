using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ConquiánServidor.Contracts.DataContracts
{
    [DataContract]
    public class PlayerDto
    {
        [DataMember]
        public int idPlayer { get; set; }

        [DataMember]
        public string password { get; set; }

        [DataMember]
        public string name { get; set; }

        [DataMember]
        public string lastName { get; set; }

        [DataMember]
        public string nickname { get; set; }

        [DataMember]
        public string email { get; set; }

        [DataMember]
        public int idLevel { get; set; }

        [DataMember]
        public int currentPoints { get; set; }

        [DataMember]
        public string pathPhoto { get; set; }

        [DataMember]
        public int? idStatus { get; set; }

        [DataMember]
        public int PointsToNextLevel { get; set; }

        [DataMember]
        public string RankName { get; set; }
    }
}

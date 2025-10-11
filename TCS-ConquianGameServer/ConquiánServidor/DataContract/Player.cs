using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ConquiánServidor.DataContract
{
    [DataContract]
    public partial class Player
    {
        [DataMember]
        public int idPlayer { get; set; }

        [DataMember]
        public string name { get; set; }

        [DataMember]
        public string lastName { get; set; }

        [DataMember]
        public string nickname { get; set; }

        [DataMember]
        public string email { get; set; }

        [DataMember]
        public string level { get; set; }

        [DataMember]
        public string currentPoints { get; set; }
    }
}

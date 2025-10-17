using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ConquiánServidor.Contracts.DataContracts
{
    [DataContract]
    public class LobbyDto
    {
        [DataMember]
        public string RoomCode { get; set; }

        [DataMember]
        public int idHostPlayer { get; set; }

        [DataMember]
        public List<PlayerDto> Players { get; set; }

        [DataMember]
        public string GameMode { get; set; }

        [DataMember]
        public string StatusLobby { get; set; }
    }
}

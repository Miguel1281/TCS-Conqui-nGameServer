using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ConquiánServidor.Contracts.DataContracts
{
    [DataContract]
    public class FriendRequestDto
    {
        [DataMember]
        public int IdFriendship { get; set; }
        [DataMember]
        public string Nickname { get; set; }
    }
}

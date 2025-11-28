using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ConquiánServidor.Contracts
{
    [DataContract(Name = "PlayerStatus")]
    public enum PlayerStatus
    {
        [EnumMember]
        Online = 1,

        [EnumMember]
        Offline = 2,
    }
}

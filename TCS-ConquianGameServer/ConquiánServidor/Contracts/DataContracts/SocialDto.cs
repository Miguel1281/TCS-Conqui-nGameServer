using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ConquiánServidor.Contracts.DataContracts
{
    [DataContract]
    public class SocialDto
    {
        [DataMember]
        public int IdSocial { get; set; }

        [DataMember]
        public int IdSocialType { get; set; }

        [DataMember]
        public string UserLink { get; set; }

    }
}

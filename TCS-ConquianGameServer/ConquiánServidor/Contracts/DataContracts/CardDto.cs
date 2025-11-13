using System.Runtime.Serialization;
namespace ConquiánServidor.Contracts.DataContracts
{
    [DataContract]
    public class CardDto
    {
        [DataMember] 
        public string Suit { get; set; }
        [DataMember] 
        public int Rank { get; set; }
        [DataMember] 
        public string ImagePath { get; set; }
        [DataMember] 
        public string Id { get; set; }
    }
}
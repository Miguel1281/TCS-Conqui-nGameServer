using System.Collections.Generic;
using System.Runtime.Serialization;
namespace ConquiánServidor.Contracts.DataContracts
{
    [DataContract]
    public class GameStateDto
    {
        [DataMember] public List<CardDto> PlayerHand { get; set; }
        [DataMember] public CardDto TopDiscardCard { get; set; }
        [DataMember] public PlayerDto Opponent { get; set; } 
        [DataMember] public int CurrentTurnPlayerId { get; set; }
    }
}
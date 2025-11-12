using System.Collections.Generic;
using ConquiánServidor.Contracts.DataContracts;

namespace ConquiánServidor.BusinessLogic
{
    public class LobbySession
    {
        public string RoomCode { get; set; }
        public int IdHostPlayer { get; set; }
        public List<PlayerDto> Players { get; set; }
        public int? IdGamemode { get; set; }

        public LobbySession()
        {
            Players = new List<PlayerDto>();
        }
    }
}

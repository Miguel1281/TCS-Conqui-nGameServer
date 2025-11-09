using ConquiánServidor.Contracts.DataContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic.Game
{
    public class ConquianGame
    {
        public string RoomCode { get; private set; }
        public int GamemodeId { get; private set; }
        public List<PlayerDto> Players { get; private set; }


        public ConquianGame(string roomCode, int gamemodeId, List<PlayerDto> players)
        {
            RoomCode = roomCode;
            GamemodeId = gamemodeId;
            Players = players;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic
{
    public class LobbyOperationException : Exception
    {
        public LobbyOperationException() { }

        public LobbyOperationException(string message): base(message) { }

        public LobbyOperationException(string message, Exception inner) : base(message, inner) { }
    }
}

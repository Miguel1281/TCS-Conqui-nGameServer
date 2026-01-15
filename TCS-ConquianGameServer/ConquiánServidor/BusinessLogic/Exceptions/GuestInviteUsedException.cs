using System;

namespace ConquiánServidor.BusinessLogic.Exceptions
{
    public class GuestInviteUsedException : Exception
    {
        public GuestInviteUsedException(string message) : base(message)
        {
        }
    }
}
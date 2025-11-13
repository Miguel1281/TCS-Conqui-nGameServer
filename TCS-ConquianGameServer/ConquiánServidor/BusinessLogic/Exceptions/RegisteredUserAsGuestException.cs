using System;

namespace ConquiánServidor.BusinessLogic.Exceptions
{
    public class RegisteredUserAsGuestException : Exception
    {
        public RegisteredUserAsGuestException(string message) : base(message) { }
    }
}
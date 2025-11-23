using System;

namespace ConquiánServidor.BusinessLogic
{
    public class GuestInviteData
    {
        public string Email { get; set; }
        public string RoomCode { get; set; }
        public DateTime CreationDate { get; set; }
        public bool WasUsed { get; set; }
    }
}
using ConquiánServidor.BusinessLogic.Interfaces;
using System;
using System.Collections.Concurrent;

namespace ConquiánServidor.BusinessLogic
{
    public class GuestInvitationManager:IGuestInvitationManager
    {
        private readonly ConcurrentDictionary<string, GuestInviteData> invitations;

        public enum InviteResult 
        { 
            Valid, 
            NotFound, 
            Expired, 
            Used 
        }

        public GuestInvitationManager()
        {
            invitations = new ConcurrentDictionary<string, GuestInviteData>();
        }

        public void AddInvitation(string email, string roomCode)
        {
            var data = new GuestInviteData
            {
                Email = email,
                RoomCode = roomCode,
                CreationDate = DateTime.UtcNow
            };

            invitations.AddOrUpdate(email, data, (key, oldValue) => data);
        }

        public GuestInviteData GetInvitation(string email)
        {
            invitations.TryGetValue(email, out var data);
            return data;
        }

        public InviteResult ValidateInvitation(string email, string roomCode)
        {
            if (invitations.TryGetValue(email, out var data))
            {
                if (data.WasUsed)
                {
                    return InviteResult.Used;
                }

                if ((DateTime.UtcNow - data.CreationDate).TotalMinutes >= 30)
                {
                    invitations.TryRemove(email, out _); 
                    return InviteResult.Expired;
                }

                if (data.RoomCode != roomCode)
                {
                    return InviteResult.NotFound; 
                }

                data.WasUsed = true;
                return InviteResult.Valid;
            }

            return InviteResult.NotFound;
        }
    }
}
using ConquiánServidor.BusinessLogic.Interfaces;
using System;
using System.Collections.Concurrent;

namespace ConquiánServidor.BusinessLogic.Guest
{
    public class GuestInvitationManager:IGuestInvitationManager
    {
        private const int INVITATION_EXPIRATION_MINUTES = 30;

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
            InviteResult result = InviteResult.NotFound;

            if (invitations.TryGetValue(email, out var data))
            {
                result = DetermineInvitationStatus(data, roomCode, email);
            }

            return result;
        }

        private InviteResult DetermineInvitationStatus(GuestInviteData data, string roomCode, string email)
        {
            InviteResult status;

            if (data.WasUsed)
            {
                status = InviteResult.Used;
            }
            else if (IsInvitationExpired(data))
            {
                invitations.TryRemove(email, out _);
                status = InviteResult.Expired;
            }
            else if (data.RoomCode != roomCode)
            {
                status = InviteResult.NotFound;
            }
            else
            {
                data.WasUsed = true;
                status = InviteResult.Valid;
            }

            return status;
        }

        private bool IsInvitationExpired(GuestInviteData data)
        {
            return (DateTime.UtcNow - data.CreationDate).TotalMinutes >= INVITATION_EXPIRATION_MINUTES;
        }
    }
}
using Xunit;
using ConquiánServidor.BusinessLogic;
using System;
using static ConquiánServidor.BusinessLogic.GuestInvitationManager;

namespace ConquiánServidor.Tests.BusinessLogic
{
    public class GuestInvitationManagerTest
    {
        [Fact]
        public void AddInvitation_NewEmail_ReturnsNotNullInvitation()
        {
            var manager = new GuestInvitationManager();
            string email = "test@example.com";
            string roomCode = "ABCDE";

            manager.AddInvitation(email, roomCode);
            var result = manager.GetInvitation(email);

            Assert.NotNull(result);
        }

        [Fact]
        public void AddInvitation_NewEmail_StoresCorrectEmail()
        {
            var manager = new GuestInvitationManager();
            string email = "test@example.com";
            string roomCode = "ABCDE";

            manager.AddInvitation(email, roomCode);
            var result = manager.GetInvitation(email);

            Assert.Equal(email, result.Email);
        }

        [Fact]
        public void AddInvitation_NewEmail_StoresCorrectRoomCode()
        {
            var manager = new GuestInvitationManager();
            string email = "test@example.com";
            string roomCode = "ABCDE";

            manager.AddInvitation(email, roomCode);
            var result = manager.GetInvitation(email);

            Assert.Equal(roomCode, result.RoomCode);
        }

        [Fact]
        public void AddInvitation_NewEmail_SetsWasUsedToFalse()
        {
            var manager = new GuestInvitationManager();
            string email = "test@example.com";
            string roomCode = "ABCDE";

            manager.AddInvitation(email, roomCode);
            var result = manager.GetInvitation(email);

            Assert.False(result.WasUsed);
        }

        [Fact]
        public void AddInvitation_ExistingEmail_UpdatesRoomCode()
        {
            var manager = new GuestInvitationManager();
            string email = "test@example.com";
            manager.AddInvitation(email, "OLD12");

            manager.AddInvitation(email, "NEW34");
            var result = manager.GetInvitation(email);

            Assert.Equal("NEW34", result.RoomCode);
        }

        [Fact]
        public void GetInvitation_NonExistentEmail_ReturnsNull()
        {
            var manager = new GuestInvitationManager();

            var result = manager.GetInvitation("nobody@example.com");

            Assert.Null(result);
        }

        [Fact]
        public void ValidateInvitation_CorrectCredentials_ReturnsValid()
        {
            var manager = new GuestInvitationManager();
            string email = "valid@example.com";
            string roomCode = "VALID";
            manager.AddInvitation(email, roomCode);

            var result = manager.ValidateInvitation(email, roomCode);

            Assert.Equal(InviteResult.Valid, result);
        }

        [Fact]
        public void ValidateInvitation_CorrectCredentials_MarksAsUsed()
        {
            var manager = new GuestInvitationManager();
            string email = "valid@example.com";
            string roomCode = "VALID";
            manager.AddInvitation(email, roomCode);

            manager.ValidateInvitation(email, roomCode);
            var data = manager.GetInvitation(email);

            Assert.True(data.WasUsed);
        }

        [Fact]
        public void ValidateInvitation_EmailNotFound_ReturnsNotFound()
        {
            var manager = new GuestInvitationManager();

            var result = manager.ValidateInvitation("unknown@example.com", "ANY");

            Assert.Equal(InviteResult.NotFound, result);
        }

        [Fact]
        public void ValidateInvitation_WrongRoomCode_ReturnsNotFound()
        {
            var manager = new GuestInvitationManager();
            string email = "test@example.com";
            manager.AddInvitation(email, "CORRECT");

            var result = manager.ValidateInvitation(email, "WRONG");

            Assert.Equal(InviteResult.NotFound, result);
        }

        [Fact]
        public void ValidateInvitation_AlreadyUsed_ReturnsUsed()
        {
            var manager = new GuestInvitationManager();
            string email = "test@example.com";
            string roomCode = "ABCDE";
            manager.AddInvitation(email, roomCode);

            manager.ValidateInvitation(email, roomCode);
            var result = manager.ValidateInvitation(email, roomCode);

            Assert.Equal(InviteResult.Used, result);
        }

        [Fact]
        public void ValidateInvitation_ExpiredTime_ReturnsExpired()
        {
            var manager = new GuestInvitationManager();
            string email = "expired@example.com";
            manager.AddInvitation(email, "ABCDE");

            var data = manager.GetInvitation(email);
            data.CreationDate = DateTime.UtcNow.AddMinutes(-31);

            var result = manager.ValidateInvitation(email, "ABCDE");

            Assert.Equal(InviteResult.Expired, result);
        }

        [Fact]
        public void ValidateInvitation_ExpiredTime_RemovesInvitation()
        {
            var manager = new GuestInvitationManager();
            string email = "expired@example.com";
            manager.AddInvitation(email, "ABCDE");

            var data = manager.GetInvitation(email);
            data.CreationDate = DateTime.UtcNow.AddMinutes(-31);

            manager.ValidateInvitation(email, "ABCDE");
            var retrieved = manager.GetInvitation(email);

            Assert.Null(retrieved);
        }
    }
}
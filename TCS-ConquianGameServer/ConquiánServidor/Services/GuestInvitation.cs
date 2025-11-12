using ConquiánServidor.ConquiánDB;
using ConquiánServidor.Contracts.ServiceContracts;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.Utilities.Email;
using ConquiánServidor.Utilities.Email.Templates;
using System;
using System.Data.Entity;
using System.ServiceModel;
using System.Threading.Tasks;
using ConquiánServidor.ConquiánDB.Repositories;

namespace ConquiánServidor.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class GuestInvitation : IGuestInvitation
    {
        private readonly ConquiánDBEntities dbContext;
        private readonly IEmailService emailService;
        private readonly ILobbyRepository lobbyRepository;

        public GuestInvitation()
        {
            dbContext = new ConquiánDBEntities();
            emailService = new EmailService();
            lobbyRepository = new LobbyRepository(dbContext);
        }

        public async Task<bool> SendGuestInviteAsync(string roomCode, string email)
        {
            try
            {
                var lobby = await lobbyRepository.GetLobbyByRoomCodeAsync(roomCode);
                if (lobby == null)
                {
                    return false; 
                }

                var guestInvite = new ConquiánDB.GuestInvite
                {
                    email = email,
                    roomCode = roomCode,
                    creationDate = DateTime.UtcNow
                };
                dbContext.GuestInvite.Add(guestInvite);
                await dbContext.SaveChangesAsync();

                var emailTemplate = new GuestInviteEmailTemplate(roomCode);
                await emailService.SendEmailAsync(email, emailTemplate);

                return true;
            }
            catch (Exception ex)
            {
                // TODO: log del error
                return false;
            }
        }
    }
}
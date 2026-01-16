using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.BusinessLogic.Interfaces;
using ConquiánServidor.Contracts.DataContracts;
using ConquiánServidor.DataAccess.Abstractions;
using ConquiánServidor.Properties.Langs;
using ConquiánServidor.Utilities.Email;
using ConquiánServidor.Utilities.Email.Templates;
using NLog;
using System;
using System.Data.SqlClient;
using System.Net.Mail;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic.Guest
{
    public class GuestInvitationLogic : IGuestInvitationLogic
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IEmailService emailService;
        private readonly ILobbyRepository lobbyRepository;
        private readonly IGuestInvitationManager guestInvitationManager;

        public GuestInvitationLogic(
            IEmailService emailService,
            ILobbyRepository lobbyRepository,
            IGuestInvitationManager guestInvitationManager)
        {
            this.emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            this.lobbyRepository = lobbyRepository ?? throw new ArgumentNullException(nameof(lobbyRepository));
            this.guestInvitationManager = guestInvitationManager ?? throw new ArgumentNullException(nameof(guestInvitationManager));
        }

        public async Task SendGuestInviteAsync(string roomCode, string email)
        {
            ValidateInputParameters(roomCode, email);

            try
            {
                await ValidateLobbyExistsAsync(roomCode);

                RegisterInvitation(email, roomCode);

                await SendInvitationEmailAsync(email, roomCode);

                Logger.Info(string.Format(Lang.LogGuestInviteSent, email, roomCode));
            }
            catch (BusinessLogicException)
            {
                throw;
            }
            catch (SqlException ex)
            {
                Logger.Error(ex, $"Database error sending guest invitation. Email: {email}, RoomCode: {roomCode}, SqlError: {ex.Number}");
                throw new BusinessLogicException(ServiceErrorType.DatabaseError, "Error accessing database");
            }
            catch (TimeoutException ex)
            {
                Logger.Error(ex, $"Timeout sending guest invitation. Email: {email}, RoomCode: {roomCode}");
                throw new BusinessLogicException(ServiceErrorType.CommunicationError, "Operation timed out");
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error(ex, $"Invalid operation sending guest invitation. Email: {email}, RoomCode: {roomCode}");
                throw new BusinessLogicException(ServiceErrorType.OperationFailed, "Invalid operation");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error sending guest invitation. Email: {email}, RoomCode: {roomCode}, Type: {ex.GetType().Name}");
                throw new BusinessLogicException(ServiceErrorType.ServerInternalError, "Internal server error");
            }
        }

        private void ValidateInputParameters(string roomCode, string email)
        {
            if (string.IsNullOrWhiteSpace(roomCode))
            {
                Logger.Warn("SendGuestInviteAsync called with null or empty roomCode");
                throw new BusinessLogicException(ServiceErrorType.ValidationFailed, "Room code is required");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                Logger.Warn($"SendGuestInviteAsync called with null or empty email. RoomCode: {roomCode}");
                throw new BusinessLogicException(ServiceErrorType.ValidationFailed, "Email is required");
            }
        }

        private async Task ValidateLobbyExistsAsync(string roomCode)
        {
            try
            {
                var lobby = await lobbyRepository.GetLobbyByRoomCodeAsync(roomCode);

                if (lobby == null)
                {
                    Logger.Warn($"Lobby not found for room code: {roomCode}");
                    throw new BusinessLogicException(ServiceErrorType.LobbyNotFound);
                }

                Logger.Debug($"Lobby found for room code: {roomCode}");
            }
            catch (BusinessLogicException)
            {
                throw;
            }
            catch (SqlException ex)
            {
                Logger.Error(ex, $"Database error retrieving lobby. RoomCode: {roomCode}, SqlError: {ex.Number}");
                throw;
            }
            catch (TimeoutException ex)
            {
                Logger.Error(ex, $"Timeout retrieving lobby. RoomCode: {roomCode}");
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error retrieving lobby. RoomCode: {roomCode}, Type: {ex.GetType().Name}");
                throw;
            }
        }

        private void RegisterInvitation(string email, string roomCode)
        {
            try
            {
                this.guestInvitationManager.AddInvitation(email, roomCode);
            }
            catch (BusinessLogicException ex)
            {
                Logger.Warn(ex, $"Business logic error registering invitation. Email: {email}, RoomCode: {roomCode}, ErrorType: {ex.ErrorType}");
                throw;
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error(ex, $"Invalid operation registering invitation. Email: {email}, RoomCode: {roomCode}");
                throw new BusinessLogicException(ServiceErrorType.OperationFailed, "Failed to register invitation");
            }
            catch (ArgumentException ex)
            {
                Logger.Error(ex, $"Invalid argument registering invitation. Email: {email}, RoomCode: {roomCode}");
                throw new BusinessLogicException(ServiceErrorType.ValidationFailed, "Invalid invitation parameters");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error registering invitation. Email: {email}, RoomCode: {roomCode}, Type: {ex.GetType().Name}");
                throw;
            }
        }

        private async Task SendInvitationEmailAsync(string email, string roomCode)
        {
            try
            {
                var emailTemplate = new GuestInviteEmailTemplate(roomCode);
                await emailService.SendEmailAsync(email, emailTemplate);
            }
            catch (SmtpException ex)
            {
                Logger.Error(ex, $"SMTP error sending email to {email}. StatusCode: {ex.StatusCode}");
                throw new BusinessLogicException(ServiceErrorType.CommunicationError, "Failed to send invitation email");
            }
            catch (FormatException ex)
            {
                Logger.Error(ex, $"Invalid email format: {email}");
                throw new BusinessLogicException(ServiceErrorType.InvalidEmailFormat, "Invalid email address format");
            }
            catch (ArgumentException ex)
            {
                Logger.Error(ex, $"Invalid argument sending email to {email}");
                throw new BusinessLogicException(ServiceErrorType.ValidationFailed, "Invalid email parameters");
            }
            catch (TimeoutException ex)
            {
                Logger.Error(ex, $"Timeout sending email to {email}");
                throw new BusinessLogicException(ServiceErrorType.CommunicationError, "Email service timeout");
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error(ex, $"Invalid operation sending email to {email}");
                throw new BusinessLogicException(ServiceErrorType.CommunicationError, "Email service error");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error sending email to {email}. Type: {ex.GetType().Name}");
                throw new BusinessLogicException(ServiceErrorType.CommunicationError, "Failed to send invitation email");
            }
        }
    }
}
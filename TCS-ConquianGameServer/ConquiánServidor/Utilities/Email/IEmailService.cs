using System.Threading.Tasks;

namespace ConquiánServidor.Utilities.Email
{
    public interface IEmailService
    {
        string GenerateVerificationCode();
        Task SendEmailAsync(string toEmail, IEmailTemplate template);
    }
}

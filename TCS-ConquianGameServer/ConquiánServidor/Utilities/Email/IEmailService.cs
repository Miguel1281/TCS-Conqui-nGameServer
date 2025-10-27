using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConquiánServidor.Utilities.Email
{
    public interface IEmailService
    {
        string GenerateVerificationCode();
        Task SendEmailAsync(string toEmail, IEmailTemplate template);
    }
}

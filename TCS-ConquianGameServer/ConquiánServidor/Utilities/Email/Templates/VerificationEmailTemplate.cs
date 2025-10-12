using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConquiánServidor.Utilities.Email.Templates
{
    public class VerificationEmailTemplate : IEmailTemplate
    {
        public string Subject => "Tu código de verificación de Conquián";
        public string HtmlBody { get; }

        public VerificationEmailTemplate(string verificationCode)
        {
            HtmlBody = $@"
                <!DOCTYPE html>
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>¡Bienvenido a Conquián!</h2>
                    <p>Gracias por registrarte. Tu código de verificación es el siguiente:</p>
                    <div style='background-color: #f0f0f0; padding: 15px; text-align: center;'>
                        <strong style='font-size: 24px; letter-spacing: 3px;'>{verificationCode}</strong>
                    </div>
                    <p>Si no creaste esta cuenta, puedes ignorar este correo.</p>
                </body>
                </html>";
        }
    }
}

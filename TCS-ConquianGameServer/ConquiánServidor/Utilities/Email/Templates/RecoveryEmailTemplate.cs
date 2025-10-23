using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConquiánServidor.Utilities.Email.Templates
{
    public class RecoveryEmailTemplate : IEmailTemplate
    {
        private readonly string token;

        public RecoveryEmailTemplate(string token)
        {
            this.token = token;
        }

        public string Subject
        {
            get { return "Recuperación de Contraseña - Conquián"; }
        }

        public string HtmlBody
        {
            get
            {
                return $@"
                <html>
                <body>
                    <h2>Recuperación de Contraseña</h2>
                    <p>Hemos recibido una solicitud para reiniciar la contraseña de tu cuenta.</p>
                    <p>Usa el siguiente código de verificación para continuar:</p>
                    <div style='background-color: #f0f0f0; padding: 15px; text-align: center;'>
                    <strong style='font-size: 24px; letter-spacing: 3px;'>{token}</strong>
                    </div>
                    <p>Este código expira en 15 minutos.</p>
                    <p>Si no solicitaste esto, puedes ignorar este correo de forma segura.</p>
                </body>
                </html>";
            }
        }
    }
}

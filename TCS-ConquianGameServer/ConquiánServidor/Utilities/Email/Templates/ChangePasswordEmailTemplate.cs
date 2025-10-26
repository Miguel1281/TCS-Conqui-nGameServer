using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConquiánServidor.Utilities.Email.Templates
{
    public class ChangePasswordEmailTemplate : IEmailTemplate
    {
        private readonly string token;

        public ChangePasswordEmailTemplate(string token)
        {
            this.token = token;
        }

        public string Subject
        {
            get { return "Conquián - Solicitud de cambio de contraseña"; }
        }

        public string HtmlBody
        {
            get
            {
                return $@"
            <html>
            <head>
                <style>
                    body {{ font-family: 'Arial', sans-serif; line-height: 1.6; }}
                    .container {{ width: 90%; margin: auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px; }}
                    .header {{ font-size: 24px; color: #333; }}
                    .content {{ font-size: 16px; margin-top: 20px; }}
                    .token {{ font-size: 20px; font-weight: bold; color: #E74C3C; margin: 20px 0; }}
                    .footer {{ font-size: 12px; color: #888; margin-top: 30px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>Solicitud de cambio de contraseña</div>
                    <div class='content'>
                        <p>Hola,</p>
                        <p>Hemos recibido una solicitud para cambiar la contraseña de tu cuenta desde la configuración de tu perfil.</p>
                        <p>Usa el siguiente código para continuar con el proceso. Si no solicitaste este cambio, puedes ignorar este correo de forma segura.</p>
                        <div class='token'>{token}</div>
                        <p>El código expirará en 10 minutos.</p>
                    </div>
                    <div class='footer'>
                        <p>&copy; {DateTime.Now.Year} Conquián. Todos los derechos reservados.</p>
                    </div>
                </div>
            </body>
            </html>";
            }
        }
    }
}
using ConquiánServidor.Properties.Langs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConquiánServidor.Utilities.Email.Templates
{
    public class GuestInviteEmailTemplate : IEmailTemplate
    {
        public string Subject => Lang.GuestEmailSubject; 
        public string HtmlBody { get; }

        public GuestInviteEmailTemplate(string roomCode)
        {
            HtmlBody = $@"
                <!DOCTYPE html>
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>¡Has sido invitado a Conquián!</h2>
                    <p>Un amigo te ha invitado a unirte a su sala. Usa el siguiente código para entrar:</p>
                    <div style='background-color: #f0f0f0; padding: 15px; text-align: center;'>
                        <strong style='font-size: 24px; letter-spacing: 3px;'>{roomCode}</strong>
                    </div>
                    <p>Descarga el juego y usa la opción 'Ingresar como invitado'.</p>
                </body>
                </html>";
        }
    }
}
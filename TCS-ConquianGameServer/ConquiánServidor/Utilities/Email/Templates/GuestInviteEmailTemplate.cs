namespace ConquiánServidor.Utilities.Email.Templates
{
    public class GuestInviteEmailTemplate : BaseEmailTemplate
    {
        private readonly string roomCode;

        public GuestInviteEmailTemplate(string roomCode)
        {
            this.roomCode = roomCode;
        }

        public override string Subject => "Fuise invitado a una partida de conquian!";
        protected override string Title => "¡Has sido invitado a Conquián!";
        protected override string Message => "Un amigo te ha invitado a unirte a su sala. Usa el siguiente código para entrar:";
        protected override string Code => roomCode;
        protected override string Footer => "Descarga el juego y usa la opción 'Ingresar como invitado'.";
    }
}
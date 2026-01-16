namespace ConquiánServidor.Utilities.Email.Templates
{
    public class RecoveryEmailTemplate : BaseEmailTemplate
    {
        private readonly string token;

        public RecoveryEmailTemplate(string token)
        {
            this.token = token;
        }

        public override string Subject => "Recuperación de Contraseña - Conquián";
        protected override string Title => "Recuperación de Contraseña";
        protected override string Message => "Hemos recibido una solicitud para reiniciar tu contraseña. Usa este código:";
        protected override string Code => token;
        protected override string Footer => "Este código expira en 10 minutos.<br>Si no solicitaste esto, ignora este correo.";
    }
}

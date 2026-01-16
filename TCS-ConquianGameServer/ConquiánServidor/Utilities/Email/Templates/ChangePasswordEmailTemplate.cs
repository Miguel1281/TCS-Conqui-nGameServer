namespace ConquiánServidor.Utilities.Email.Templates
{
    public class ChangePasswordEmailTemplate : BaseEmailTemplate
    {
        private readonly string token;

        public ChangePasswordEmailTemplate(string token)
        {
            this.token = token;
        }

        public override string Subject => "Conquián - Solicitud de cambio de contraseña";
        protected override string Title => "Cambio de Contraseña";
        protected override string Message => "Solicitaste cambiar tu contraseña. Usa el siguiente código para continuar:";
        protected override string Code => token;
        protected override string Footer => "Este código expira en 10 minutos.<br>Si no fuiste tú, protege tu cuenta.";
    }
}
namespace ConquiánServidor.Utilities.Email.Templates
{
    public class VerificationEmailTemplate : BaseEmailTemplate
    {
        private readonly string verificationCode;

        public VerificationEmailTemplate(string verificationCode)
        {
            this.verificationCode = verificationCode;
        }

        public override string Subject => "Tu código de verificación de Conquián";
        protected override string Title => "¡Bienvenido a Conquián!";
        protected override string Message => "Gracias por registrarte. Tu código de verificación es el siguiente:";
        protected override string Code => verificationCode;
        protected override string Footer => "Si no creaste esta cuenta, puedes ignorar este correo.";
    }
}

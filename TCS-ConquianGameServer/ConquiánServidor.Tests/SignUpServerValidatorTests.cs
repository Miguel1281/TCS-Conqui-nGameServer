using ConquiánServidor.BusinessLogic.Validation;
using Xunit;

namespace ConquiánServidor.Tests
{
    public class SignUpServerValidatorTests
    {
        [Theory]
        [InlineData("Juan")]
        [InlineData("Maria Luisa")]
        [InlineData("a")]
        [InlineData("Nombre De Veinticinco C")]
        [InlineData("  Juan")]
        [InlineData("Juan  ")] 
        [InlineData("  Juan Carlos  ")] 
        [InlineData("   ")]
        public void ValidateName_ShouldReturnEmpty_WhenNameIsValidAccordingToCurrentLogic(string validName)
        {
            string result = SignUpServerValidator.ValidateName(validName);
            Assert.Empty(result);
        }

        [Theory]
        [InlineData(null, SignUpServerValidator.ERROR_NAME_EMPTY)]
        [InlineData("", SignUpServerValidator.ERROR_NAME_EMPTY)]
        [InlineData("NombreDemasiadoLargoQueSuperaElLimite", SignUpServerValidator.ERROR_NAME_LENGTH)]
        [InlineData("Juan123", SignUpServerValidator.ERROR_VALID_NAME)]
        [InlineData("Pedro!", SignUpServerValidator.ERROR_VALID_NAME)]
        public void ValidateName_ShouldReturnError_WhenNameIsInvalid(string invalidName, string expectedError)
        {
            string result = SignUpServerValidator.ValidateName(invalidName);
            Assert.Equal(expectedError, result);
        }

        [Theory]
        [InlineData("Perez")]
        [InlineData("De La Cruz")]
        [InlineData("a")]
        [InlineData("Apellido Muy Largo Que Cumple Cincuenta Caracteres")]
        [InlineData("  ApellidoConEspacios  ")]
        public void ValidateLastName_ShouldReturnEmpty_WhenLastNameIsValid(string validLastName)
        {
            string result = SignUpServerValidator.ValidateLastName(validLastName);
            Assert.Empty(result);
        }

        [Theory]
        [InlineData(null, SignUpServerValidator.ERROR_LAST_NAME_EMPTY)]
        [InlineData("", SignUpServerValidator.ERROR_LAST_NAME_EMPTY)]
        [InlineData("   ", SignUpServerValidator.ERROR_LAST_NAME_INVALID_CHARS)]
        [InlineData("ApellidoExcesivamenteLargoQueSuperaLosCincuentaCaracteresPermitidos", SignUpServerValidator.ERROR_LAST_NAME_LENGTH)]
        [InlineData("Gomez9", SignUpServerValidator.ERROR_LAST_NAME_INVALID_CHARS)]
        [InlineData("Nuñez", SignUpServerValidator.ERROR_LAST_NAME_INVALID_CHARS)] 
        [InlineData("O'Malley", SignUpServerValidator.ERROR_LAST_NAME_INVALID_CHARS)]
        [InlineData("García", SignUpServerValidator.ERROR_LAST_NAME_INVALID_CHARS)]
        public void ValidateLastName_ShouldReturnError_WhenLastNameIsInvalid(string invalidLastName, string expectedError)
        {
            string result = SignUpServerValidator.ValidateLastName(invalidLastName);
            Assert.Equal(expectedError, result);
        }

        [Theory]
        [InlineData("Player1")]
        [InlineData("Nickname")]
        [InlineData("a")]
        [InlineData("NickCon15Chars")]
        [InlineData("  NickEspacios  ")]
        public void ValidateNickname_ShouldReturnEmpty_WhenNicknameIsValid(string validNickname)
        {
            string result = SignUpServerValidator.ValidateNickname(validNickname);
            Assert.Empty(result);
        }

        [Theory]
        [InlineData(null, SignUpServerValidator.ERROR_NICKNAME_EMPTY)]
        [InlineData("", SignUpServerValidator.ERROR_NICKNAME_EMPTY)]
        [InlineData("   ", SignUpServerValidator.ERROR_NICKNAME_INVALID_CHARS)]
        [InlineData("EsteNickEsDemasiadoLargo", SignUpServerValidator.ERROR_NICKNAME_LENGTH)]
        [InlineData("Nick Con Espacio", SignUpServerValidator.ERROR_NICKNAME_LENGTH)]
        [InlineData("Nick-Guion", SignUpServerValidator.ERROR_NICKNAME_INVALID_CHARS)]
        [InlineData("Ñandú", SignUpServerValidator.ERROR_NICKNAME_INVALID_CHARS)]
        public void ValidateNickname_ShouldReturnError_WhenNicknameIsInvalid(string invalidNickname, string expectedError)
        {
            string result = SignUpServerValidator.ValidateNickname(invalidNickname);
            Assert.Equal(expectedError, result);
        }

        [Theory]
        [InlineData("test@example.com")]
        [InlineData("usuario.apellido@dominio.co")]
        [InlineData("a@b.co")]
        [InlineData("correo.largo.con.subdominios@ejemplo.com.mx")]
        [InlineData("correoCon45CaracteresExactos@dominioLargo.com")] 
        [InlineData("  correo@espacios.com  ")]
        public void ValidateEmail_ShouldReturnEmpty_WhenEmailIsValid(string validEmail)
        {
            string result = SignUpServerValidator.ValidateEmail(validEmail);
            Assert.Empty(result);
        }

        [Theory]
        [InlineData(null, SignUpServerValidator.ERROR_EMAIL_EMPTY)]
        [InlineData("", SignUpServerValidator.ERROR_EMAIL_EMPTY)]
        [InlineData("   ", SignUpServerValidator.ERROR_EMAIL_EMPTY)]
        [InlineData("correoextremadamentelargoquedefinitivasuperalos45@caracteres.com", SignUpServerValidator.ERROR_EMAIL_LENGTH)] // > 45
        [InlineData("sinarroba.com", SignUpServerValidator.ERROR_EMAIL_INVALID_FORMAT)]
        [InlineData("@dominio.com", SignUpServerValidator.ERROR_EMAIL_INVALID_FORMAT)]
        [InlineData("usuario@", SignUpServerValidator.ERROR_EMAIL_INVALID_FORMAT)]
        [InlineData("usuario@dominio", SignUpServerValidator.ERROR_EMAIL_INVALID_FORMAT)]
        [InlineData("usuario@dominio.", SignUpServerValidator.ERROR_EMAIL_INVALID_FORMAT)]
        [InlineData("usuario@.com", SignUpServerValidator.ERROR_EMAIL_INVALID_FORMAT)]
        [InlineData("usuario con espacio@dominio.com", SignUpServerValidator.ERROR_EMAIL_INVALID_FORMAT)]
        [InlineData("usuario@dominio .com", SignUpServerValidator.ERROR_EMAIL_INVALID_FORMAT)]
        [InlineData("usuario@dominio.c", SignUpServerValidator.ERROR_EMAIL_INVALID_FORMAT)]
        public void ValidateEmail_ShouldReturnError_WhenEmailIsInvalid(string invalidEmail, string expectedError)
        {
            string result = SignUpServerValidator.ValidateEmail(invalidEmail);
            Assert.Equal(expectedError, result);
        }

        [Theory]
        [InlineData("Passwd1!")]
        [InlineData("P@ssw0rdValido")]
        [InlineData("Val1dP@sswrd15c")]
        public void ValidatePassword_ShouldReturnEmpty_WhenPasswordIsValid(string validPassword)
        {
            string result = SignUpServerValidator.ValidatePassword(validPassword);
            Assert.Empty(result);
        }

        [Theory]
        [InlineData(null, SignUpServerValidator.ERROR_PASSWORD_EMPTY)]
        [InlineData("", SignUpServerValidator.ERROR_PASSWORD_EMPTY)]
        [InlineData("corto1!", SignUpServerValidator.ERROR_PASSWORD_LENGTH)]
        [InlineData("MuyLargo1!MuyLargo", SignUpServerValidator.ERROR_PASSWORD_LENGTH)]
        [InlineData("Pass Con Espacio1!", SignUpServerValidator.ERROR_PASSWORD_LENGTH)]
        [InlineData("password1!", SignUpServerValidator.ERROR_PASSWORD_NO_UPPERCASE)]
        [InlineData("PASSWORD12", SignUpServerValidator.ERROR_PASSWORD_NO_SPECIAL_CHAR)]
        [InlineData("Pass Esp1!", SignUpServerValidator.ERROR_PASSWORD_NO_SPACES)]
        public void ValidatePassword_ShouldReturnError_WhenPasswordIsInvalid(string invalidPassword, string expectedError)
        {
            string result = SignUpServerValidator.ValidatePassword(invalidPassword);
            Assert.Equal(expectedError, result);
        }
    }
}

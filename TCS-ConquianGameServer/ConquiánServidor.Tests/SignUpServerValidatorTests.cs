using ConquiánServidor.BusinessLogic.Validation;
using Xunit;

namespace ConquiánServidor.Tests
{
    public class SignUpServerValidatorTests
    {
        [Fact]
        public void ValidateName_ShouldReturnEmpty_WhenNameIsSimple()
        {
            string validName = "Juan";

            string result = SignUpServerValidator.ValidateName(validName);

            Assert.Empty(result);
        }

        [Fact]
        public void ValidateName_ShouldReturnEmpty_WhenNameIsCompound()
        {
            string validName = "Maria Luisa";

            string result = SignUpServerValidator.ValidateName(validName);

            Assert.Empty(result);
        }

        [Fact]
        public void ValidateName_ShouldReturnEmpty_WhenNameIsSingleChar()
        {
            string validName = "a";

            string result = SignUpServerValidator.ValidateName(validName);

            Assert.Empty(result);
        }

        [Fact]
        public void ValidateName_ShouldReturnEmpty_WhenNameIsAtMaxLength()
        {
            string validName = "Nombre De Veinticinco C";

            string result = SignUpServerValidator.ValidateName(validName);

            Assert.Empty(result);
        }

        [Fact]
        public void ValidateName_ShouldReturnEmpty_WhenNameHasLeadingSpaces()
        {
            string validName = "  Juan";

            string result = SignUpServerValidator.ValidateName(validName);

            Assert.Empty(result);
        }

        [Fact]
        public void ValidateName_ShouldReturnEmpty_WhenNameHasTrailingSpaces()
        {
            string validName = "Juan  ";

            string result = SignUpServerValidator.ValidateName(validName);

            Assert.Empty(result);
        }

        [Fact]
        public void ValidateName_ShouldReturnEmpty_WhenNameHasSurroundingSpaces()
        {
            string validName = "  Juan Carlos  ";

            string result = SignUpServerValidator.ValidateName(validName);

            Assert.Empty(result);
        }

        [Fact]
        public void ValidateName_ShouldReturnError_WhenNameIsNull()
        {
            string invalidName = null;
            string expectedError = SignUpServerValidator.ERROR_NAME_EMPTY;

            string result = SignUpServerValidator.ValidateName(invalidName);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateName_ShouldReturnError_WhenNameIsEmpty()
        {
            string invalidName = "";
            string expectedError = SignUpServerValidator.ERROR_NAME_EMPTY;

            string result = SignUpServerValidator.ValidateName(invalidName);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateName_ShouldReturnError_WhenNameIsOnlySpaces()
        {
            string invalidName = "    ";
            string expectedError = SignUpServerValidator.ERROR_VALID_NAME;

            string result = SignUpServerValidator.ValidateName(invalidName);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateName_ShouldReturnError_WhenNameIsTooLong()
        {
            string invalidName = "NombreDemasiadoLargoQueSuperaElLimite";
            string expectedError = SignUpServerValidator.ERROR_NAME_LENGTH;

            string result = SignUpServerValidator.ValidateName(invalidName);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateName_ShouldReturnError_WhenNameContainsNumbers()
        {
            string invalidName = "Juan123";
            string expectedError = SignUpServerValidator.ERROR_VALID_NAME;

            string result = SignUpServerValidator.ValidateName(invalidName);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateName_ShouldReturnError_WhenNameContainsSpecialChars()
        {
            string invalidName = "Pedro!";
            string expectedError = SignUpServerValidator.ERROR_VALID_NAME;

            string result = SignUpServerValidator.ValidateName(invalidName);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateLastName_ShouldReturnEmpty_WhenLastNameIsSimple()
        {
            string validLastName = "Perez";

            string result = SignUpServerValidator.ValidateLastName(validLastName);

            Assert.Empty(result);
        }

        [Fact]
        public void ValidateLastName_ShouldReturnEmpty_WhenLastNameIsCompound()
        {
            string validLastName = "De La Cruz";

            string result = SignUpServerValidator.ValidateLastName(validLastName);

            Assert.Empty(result);
        }

        [Fact]
        public void ValidateLastName_ShouldReturnEmpty_WhenLastNameIsSingleChar()
        {
            string validLastName = "a";

            string result = SignUpServerValidator.ValidateLastName(validLastName);

            Assert.Empty(result);
        }

        [Fact]
        public void ValidateLastName_ShouldReturnEmpty_WhenLastNameIsAtMaxLength()
        {
            string validLastName = "Apellido Muy Largo Que Cumple Cincuenta Caracteres";

            string result = SignUpServerValidator.ValidateLastName(validLastName);

            Assert.Empty(result);
        }

        [Fact]
        public void ValidateLastName_ShouldReturnEmpty_WhenLastNameHasSurroundingSpaces()
        {
            string validLastName = "  ApellidoConEspacios  ";

            string result = SignUpServerValidator.ValidateLastName(validLastName);

            Assert.Empty(result);
        }

        [Fact]
        public void ValidateLastName_ShouldReturnError_WhenLastNameIsNull()
        {
            string invalidLastName = null;
            string expectedError = SignUpServerValidator.ERROR_LAST_NAME_EMPTY;

            string result = SignUpServerValidator.ValidateLastName(invalidLastName);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateLastName_ShouldReturnError_WhenLastNameIsEmpty()
        {
            string invalidLastName = "";
            string expectedError = SignUpServerValidator.ERROR_LAST_NAME_EMPTY;

            string result = SignUpServerValidator.ValidateLastName(invalidLastName);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateLastName_ShouldReturnError_WhenLastNameIsOnlySpaces()
        {
            string invalidLastName = "    ";
            string expectedError = SignUpServerValidator.ERROR_LAST_NAME_INVALID_CHARS;

            string result = SignUpServerValidator.ValidateLastName(invalidLastName);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateLastName_ShouldReturnError_WhenLastNameIsTooLong()
        {
            string invalidLastName = "ApellidoExcesivamenteLargoQueSuperaLosCincuentaCaracteresPermitidos";
            string expectedError = SignUpServerValidator.ERROR_LAST_NAME_LENGTH;

            string result = SignUpServerValidator.ValidateLastName(invalidLastName);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateLastName_ShouldReturnError_WhenLastNameContainsNumbers()
        {
            string invalidLastName = "Gomez9";
            string expectedError = SignUpServerValidator.ERROR_LAST_NAME_INVALID_CHARS;

            string result = SignUpServerValidator.ValidateLastName(invalidLastName);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateLastName_ShouldReturnError_WhenLastNameContainsAccentN()
        {
            string invalidLastName = "Nuñez";
            string expectedError = SignUpServerValidator.ERROR_LAST_NAME_INVALID_CHARS;

            string result = SignUpServerValidator.ValidateLastName(invalidLastName);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateLastName_ShouldReturnError_WhenLastNameContainsApostrophe()
        {
            string invalidLastName = "O'Malley";
            string expectedError = SignUpServerValidator.ERROR_LAST_NAME_INVALID_CHARS;

            string result = SignUpServerValidator.ValidateLastName(invalidLastName);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateLastName_ShouldReturnError_WhenLastNameContainsAccentA()
        {
            string invalidLastName = "García";
            string expectedError = SignUpServerValidator.ERROR_LAST_NAME_INVALID_CHARS;

            string result = SignUpServerValidator.ValidateLastName(invalidLastName);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateNickname_ShouldReturnEmpty_WhenNicknameIsValid()
        {
            string validNickname = "Player1";

            string result = SignUpServerValidator.ValidateNickname(validNickname);

            Assert.Empty(result);
        }

        [Fact]
        public void ValidateNickname_ShouldReturnEmpty_WhenNicknameIsLettersOnly()
        {
            string validNickname = "Nickname";

            string result = SignUpServerValidator.ValidateNickname(validNickname);

            Assert.Empty(result);
        }

        [Fact]
        public void ValidateNickname_ShouldReturnEmpty_WhenNicknameIsSingleChar()
        {
            string validNickname = "a";

            string result = SignUpServerValidator.ValidateNickname(validNickname);

            Assert.Empty(result);
        }

        [Fact]
        public void ValidateNickname_ShouldReturnEmpty_WhenNicknameIsAtMaxLength()
        {
            string validNickname = "NickCon15Chars";

            string result = SignUpServerValidator.ValidateNickname(validNickname);

            Assert.Empty(result);
        }

        [Fact]
        public void ValidateNickname_ShouldReturnEmpty_WhenNicknameHasSurroundingSpaces()
        {
            string validNickname = "  NickEspacios  ";

            string result = SignUpServerValidator.ValidateNickname(validNickname);

            Assert.Empty(result);
        }

        [Fact]
        public void ValidateNickname_ShouldReturnError_WhenNicknameIsNull()
        {
            string invalidNickname = null;
            string expectedError = SignUpServerValidator.ERROR_NICKNAME_EMPTY;

            string result = SignUpServerValidator.ValidateNickname(invalidNickname);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateNickname_ShouldReturnError_WhenNicknameIsEmpty()
        {
            string invalidNickname = "";
            string expectedError = SignUpServerValidator.ERROR_NICKNAME_EMPTY;

            string result = SignUpServerValidator.ValidateNickname(invalidNickname);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateNickname_ShouldReturnError_WhenNicknameIsOnlySpaces()
        {
            string invalidNickname = "    ";
            string expectedError = SignUpServerValidator.ERROR_NICKNAME_INVALID_CHARS;

            string result = SignUpServerValidator.ValidateNickname(invalidNickname);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateNickname_ShouldReturnError_WhenNicknameIsTooLong()
        {
            string invalidNickname = "EsteNickEsDemasiadoLargo";
            string expectedError = SignUpServerValidator.ERROR_NICKNAME_LENGTH;

            string result = SignUpServerValidator.ValidateNickname(invalidNickname);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateNickname_ShouldReturnError_WhenNicknameHasInternalSpace()
        {
            string invalidNickname = "Nick Con Espacio";
            string expectedError = SignUpServerValidator.ERROR_NICKNAME_LENGTH;

            string result = SignUpServerValidator.ValidateNickname(invalidNickname);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateNickname_ShouldReturnError_WhenNicknameContainsHyphen()
        {
            string invalidNickname = "Nick-Guion";
            string expectedError = SignUpServerValidator.ERROR_NICKNAME_INVALID_CHARS;

            string result = SignUpServerValidator.ValidateNickname(invalidNickname);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateNickname_ShouldReturnError_WhenNicknameContainsAccent()
        {
            string invalidNickname = "Ñandú";
            string expectedError = SignUpServerValidator.ERROR_NICKNAME_INVALID_CHARS;

            string result = SignUpServerValidator.ValidateNickname(invalidNickname);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateEmail_ShouldReturnEmpty_WhenEmailIsSimple()
        {
            string validEmail = "test@example.com";

            string result = SignUpServerValidator.ValidateEmail(validEmail);

            Assert.Empty(result);
        }

        [Fact]
        public void ValidateEmail_ShouldReturnEmpty_WhenEmailHasDotInUser()
        {
            string validEmail = "usuario.apellido@dominio.co";

            string result = SignUpServerValidator.ValidateEmail(validEmail);

            Assert.Empty(result);
        }

        [Fact]
        public void ValidateEmail_ShouldReturnEmpty_WhenEmailIsShort()
        {
            string validEmail = "a@b.co";

            string result = SignUpServerValidator.ValidateEmail(validEmail);

            Assert.Empty(result);
        }

        [Fact]
        public void ValidateEmail_ShouldReturnEmpty_WhenEmailHasSubdomains()
        {
            string validEmail = "correo.largo.con.subdominios@ejemplo.com.mx";

            string result = SignUpServerValidator.ValidateEmail(validEmail);

            Assert.Empty(result);
        }

        [Fact]
        public void ValidateEmail_ShouldReturnEmpty_WhenEmailIsAtMaxLength()
        {
            string validEmail = "correoCon45CaracteresExactos@dominioLargo.com";

            string result = SignUpServerValidator.ValidateEmail(validEmail);

            Assert.Empty(result);
        }

        [Fact]
        public void ValidateEmail_ShouldReturnEmpty_WhenEmailHasSurroundingSpaces()
        {
            string validEmail = "  correo@espacios.com  ";

            string result = SignUpServerValidator.ValidateEmail(validEmail);

            Assert.Empty(result);
        }

        [Fact]
        public void ValidateEmail_ShouldReturnError_WhenEmailIsNull()
        {
            string invalidEmail = null;
            string expectedError = SignUpServerValidator.ERROR_EMAIL_EMPTY;

            string result = SignUpServerValidator.ValidateEmail(invalidEmail);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateEmail_ShouldReturnError_WhenEmailIsEmpty()
        {
            string invalidEmail = "";
            string expectedError = SignUpServerValidator.ERROR_EMAIL_EMPTY;

            string result = SignUpServerValidator.ValidateEmail(invalidEmail);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateEmail_ShouldReturnError_WhenEmailIsOnlySpaces()
        {
            string invalidEmail = "    ";
            string expectedError = SignUpServerValidator.ERROR_EMAIL_EMPTY;

            string result = SignUpServerValidator.ValidateEmail(invalidEmail);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateEmail_ShouldReturnError_WhenEmailIsTooLong()
        {
            string invalidEmail = "correoextremadamentelargoquedefinitivasuperalos45@caracteres.com";
            string expectedError = SignUpServerValidator.ERROR_EMAIL_LENGTH;

            string result = SignUpServerValidator.ValidateEmail(invalidEmail);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateEmail_ShouldReturnError_WhenEmailHasNoAtSign()
        {
            string invalidEmail = "sinarroba.com";
            string expectedError = SignUpServerValidator.ERROR_EMAIL_INVALID_FORMAT;

            string result = SignUpServerValidator.ValidateEmail(invalidEmail);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateEmail_ShouldReturnError_WhenEmailHasNoUser()
        {
            string invalidEmail = "@dominio.com";
            string expectedError = SignUpServerValidator.ERROR_EMAIL_INVALID_FORMAT;

            string result = SignUpServerValidator.ValidateEmail(invalidEmail);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateEmail_ShouldReturnError_WhenEmailHasNoDomain()
        {
            string invalidEmail = "usuario@";
            string expectedError = SignUpServerValidator.ERROR_EMAIL_INVALID_FORMAT;

            string result = SignUpServerValidator.ValidateEmail(invalidEmail);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateEmail_ShouldReturnError_WhenEmailHasNoTld()
        {
            string invalidEmail = "usuario@dominio";
            string expectedError = SignUpServerValidator.ERROR_EMAIL_INVALID_FORMAT;

            string result = SignUpServerValidator.ValidateEmail(invalidEmail);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateEmail_ShouldReturnError_WhenEmailTldIsInvalid()
        {
            string invalidEmail = "usuario@dominio.";
            string expectedError = SignUpServerValidator.ERROR_EMAIL_INVALID_FORMAT;

            string result = SignUpServerValidator.ValidateEmail(invalidEmail);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateEmail_ShouldReturnError_WhenEmailDomainIsInvalid()
        {
            string invalidEmail = "usuario@.com";
            string expectedError = SignUpServerValidator.ERROR_EMAIL_INVALID_FORMAT;

            string result = SignUpServerValidator.ValidateEmail(invalidEmail);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateEmail_ShouldReturnError_WhenEmailUserHasSpace()
        {
            string invalidEmail = "usuario con espacio@dominio.com";
            string expectedError = SignUpServerValidator.ERROR_EMAIL_INVALID_FORMAT;

            string result = SignUpServerValidator.ValidateEmail(invalidEmail);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateEmail_ShouldReturnError_WhenEmailDomainHasSpace()
        {
            string invalidEmail = "usuario@dominio .com";
            string expectedError = SignUpServerValidator.ERROR_EMAIL_INVALID_FORMAT;

            string result = SignUpServerValidator.ValidateEmail(invalidEmail);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateEmail_ShouldReturnError_WhenEmailTldIsTooShort()
        {
            string invalidEmail = "usuario@dominio.c";
            string expectedError = SignUpServerValidator.ERROR_EMAIL_INVALID_FORMAT;

            string result = SignUpServerValidator.ValidateEmail(invalidEmail);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidatePassword_ShouldReturnEmpty_WhenPasswordIsValidAtMinLength()
        {
            string validPassword = "Passwd1!";

            string result = SignUpServerValidator.ValidatePassword(validPassword);

            Assert.Empty(result);
        }

        [Fact]
        public void ValidatePassword_ShouldReturnEmpty_WhenPasswordIsValid()
        {
            string validPassword = "P@ssw0rdValido";

            string result = SignUpServerValidator.ValidatePassword(validPassword);

            Assert.Empty(result);
        }

        [Fact]
        public void ValidatePassword_ShouldReturnEmpty_WhenPasswordIsValidAtMaxLength()
        {
            string validPassword = "Val1dP@sswrd15c";

            string result = SignUpServerValidator.ValidatePassword(validPassword);

            Assert.Empty(result);
        }

        [Fact]
        public void ValidatePassword_ShouldReturnError_WhenPasswordIsNull()
        {
            string invalidPassword = null;
            string expectedError = SignUpServerValidator.ERROR_PASSWORD_EMPTY;

            string result = SignUpServerValidator.ValidatePassword(invalidPassword);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidatePassword_ShouldReturnError_WhenPasswordIsEmpty()
        {
            string invalidPassword = "";
            string expectedError = SignUpServerValidator.ERROR_PASSWORD_EMPTY;

            string result = SignUpServerValidator.ValidatePassword(invalidPassword);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidatePassword_ShouldReturnError_WhenPasswordIsTooShort()
        {
            string invalidPassword = "corto1!";
            string expectedError = SignUpServerValidator.ERROR_PASSWORD_LENGTH;

            string result = SignUpServerValidator.ValidatePassword(invalidPassword);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidatePassword_ShouldReturnError_WhenPasswordIsTooLong()
        {
            string invalidPassword = "MuyLargo1!MuyLar";
            string expectedError = SignUpServerValidator.ERROR_PASSWORD_LENGTH;

            string result = SignUpServerValidator.ValidatePassword(invalidPassword);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidatePassword_ShouldReturnError_WhenPasswordHasSpaceAndIsTooLong()
        {
            string invalidPassword = "Pass Con Espacio1!";
            string expectedError = SignUpServerValidator.ERROR_PASSWORD_LENGTH;

            string result = SignUpServerValidator.ValidatePassword(invalidPassword);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidatePassword_ShouldReturnError_WhenPasswordHasNoUppercase()
        {
            string invalidPassword = "password1!";
            string expectedError = SignUpServerValidator.ERROR_PASSWORD_NO_UPPERCASE;

            string result = SignUpServerValidator.ValidatePassword(invalidPassword);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidatePassword_ShouldReturnError_WhenPasswordHasNoSpecialChar()
        {
            string invalidPassword = "PASSWORD12";
            string expectedError = SignUpServerValidator.ERROR_PASSWORD_NO_SPECIAL_CHAR;

            string result = SignUpServerValidator.ValidatePassword(invalidPassword);

            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidatePassword_ShouldReturnError_WhenPasswordHasSpace()
        {
            string invalidPassword = "Pass Esp1!";
            string expectedError = SignUpServerValidator.ERROR_PASSWORD_NO_SPACES;

            string result = SignUpServerValidator.ValidatePassword(invalidPassword);

            Assert.Equal(expectedError, result);
        }
    }
}
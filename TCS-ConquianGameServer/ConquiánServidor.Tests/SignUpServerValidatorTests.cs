using ConquiánServidor.BusinessLogic.Validation;
using Xunit;

namespace ConquiánServidor.Tests
{
    public class SignUpServerValidatorTests
    {
        [Fact]
        public void ValidateName_ShouldReturnEmpty_WhenNameIsValid()
        {
            // Arrange
            string validName = "Juan Carlos";

            // Act
            string result = SignUpServerValidator.ValidateName(validName);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Theory]
        [InlineData(null, SignUpServerValidator.ERROR_NAME_EMPTY)]
        [InlineData("", SignUpServerValidator.ERROR_NAME_EMPTY)]
        [InlineData("UnNombreDemasiadoLargoQueSuperaLos25", SignUpServerValidator.ERROR_NAME_LENGTH)]
        [InlineData("NombreConNumer0s", SignUpServerValidator.ERROR_VALID_NAME)]
        [InlineData("NombreCon$", SignUpServerValidator.ERROR_VALID_NAME)]
        public void ValidateName_ShouldReturnError_WhenNameIsInvalid(string invalidName, string expectedError)
        {
            // Act
            string result = SignUpServerValidator.ValidateName(invalidName);

            // Assert
            Assert.Equal(expectedError, result);
        }

        [Theory]
        [InlineData(null, SignUpServerValidator.ERROR_LAST_NAME_EMPTY)]
        [InlineData("", SignUpServerValidator.ERROR_LAST_NAME_EMPTY)]
        [InlineData("UnApellidoDemasiadoLargoQueSuperaLosCincuentaCaracteresDefinitivamente", SignUpServerValidator.ERROR_LAST_NAME_LENGTH)]
        [InlineData("ApellidoConNumer0s", SignUpServerValidator.ERROR_LAST_NAME_INVALID_CHARS)]
        [InlineData("ApellidoCon$", SignUpServerValidator.ERROR_LAST_NAME_INVALID_CHARS)]
        public void ValidateLastName_ShouldReturnError_WhenLastNameIsInvalid(string invalidLastName, string expectedError)
        {
            // Act
            string result = SignUpServerValidator.ValidateLastName(invalidLastName);
            // Assert
            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateNickname_ShouldReturnEmpty_WhenNicknameIsValid()
        {
            // Arrange
            string validNickname = "SeniorDev123";

            // Act
            string result = SignUpServerValidator.ValidateNickname(validNickname);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Theory]
        [InlineData(null, SignUpServerValidator.ERROR_NICKNAME_EMPTY)]
        [InlineData("", SignUpServerValidator.ERROR_NICKNAME_EMPTY)]
        [InlineData("UnNickDemasiadoLargo123456", SignUpServerValidator.ERROR_NICKNAME_LENGTH)]
        [InlineData("e spa ci o", SignUpServerValidator.ERROR_NICKNAME_INVALID_CHARS)]
        [InlineData("NickCon$", SignUpServerValidator.ERROR_NICKNAME_INVALID_CHARS)]
        public void ValidateNickname_ShouldReturnError_WhenNicknameIsInvalid(string invalidNickname, string expectedError)
        {
            // Act
            string result = SignUpServerValidator.ValidateNickname(invalidNickname);

            // Assert
            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidateEmail_ShouldReturnEmpty_WhenEmailIsValid()
        {
            // Act
            string result = SignUpServerValidator.ValidateEmail("test.valido@dominio.com");

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Theory]
        [InlineData(null, SignUpServerValidator.ERROR_EMAIL_EMPTY)]
        [InlineData(" ", SignUpServerValidator.ERROR_EMAIL_EMPTY)]
        [InlineData("correo-muy-largo-definitivamente-excede-los-45-caracteres@dominio.com", SignUpServerValidator.ERROR_EMAIL_LENGTH)]
        [InlineData("correo-invalido.com", SignUpServerValidator.ERROR_EMAIL_INVALID_FORMAT)]
        [InlineData("correo@dominio", SignUpServerValidator.ERROR_EMAIL_INVALID_FORMAT)]
        public void ValidateEmail_ShouldReturnError_WhenEmailIsInvalid(string invalidEmail, string expectedError)
        {
            // Act
            string result = SignUpServerValidator.ValidateEmail(invalidEmail);

            // Assert
            Assert.Equal(expectedError, result);
        }

        [Fact]
        public void ValidatePassword_ShouldReturnEmpty_WhenPasswordIsValid()
        {
            // Act
            string result = SignUpServerValidator.ValidatePassword("P@sswordValido1");

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Theory]
        [InlineData("corta", SignUpServerValidator.ERROR_PASSWORD_LENGTH)]
        [InlineData("EstaClaveEsDemasiadoLarga", SignUpServerValidator.ERROR_PASSWORD_LENGTH)]
        [InlineData("sinespacios1@", SignUpServerValidator.ERROR_PASSWORD_NO_UPPERCASE)]
        [InlineData("SINEspecial1", SignUpServerValidator.ERROR_PASSWORD_NO_SPECIAL_CHAR)]
        [InlineData("Con Espacios1@", SignUpServerValidator.ERROR_PASSWORD_NO_SPACES)]
        [InlineData("", SignUpServerValidator.ERROR_PASSWORD_EMPTY)]
        public void ValidatePassword_ShouldReturnError_WhenPasswordIsInvalid(string invalidPassword, string expectedError)
        {
            // Act
            string result = SignUpServerValidator.ValidatePassword(invalidPassword);

            // Assert
            Assert.Equal(expectedError, result);
        }
    }
}

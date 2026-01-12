using Xunit;
using ConquiánServidor.BusinessLogic.Validation;

namespace ConquiánServidor.Tests.BusinessLogic.Validation
{
    public class SignUpServerValidatorTest
    {
        [Fact]
        public void ValidateName_ValidName_ReturnsEmptyString()
        {
            string name = "Juan Carlos";
            string result = SignUpServerValidator.ValidateName(name);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ValidateName_NullName_ReturnsErrorNameEmpty()
        {
            string name = null;
            string result = SignUpServerValidator.ValidateName(name);
            Assert.Equal(SignUpServerValidator.ERROR_NAME_EMPTY, result);
        }

        [Fact]
        public void ValidateName_EmptyName_ReturnsErrorNameEmpty()
        {
            string name = "";
            string result = SignUpServerValidator.ValidateName(name);
            Assert.Equal(SignUpServerValidator.ERROR_NAME_EMPTY, result);
        }

        [Fact]
        public void ValidateName_NameTooLong_ReturnsErrorNameLength()
        {
            string name = "JuanCarlosJuanCarlosJuanCa";
            string result = SignUpServerValidator.ValidateName(name);
            Assert.Equal(SignUpServerValidator.ERROR_NAME_LENGTH, result);
        }

        [Fact]
        public void ValidateName_NameWithNumbers_ReturnsErrorValidName()
        {
            string name = "Juan123";
            string result = SignUpServerValidator.ValidateName(name);
            Assert.Equal(SignUpServerValidator.ERROR_VALID_NAME, result);
        }

        [Fact]
        public void ValidateLastName_ValidLastName_ReturnsEmptyString()
        {
            string lastName = "Perez Lopez";
            string result = SignUpServerValidator.ValidateLastName(lastName);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ValidateLastName_EmptyLastName_ReturnsErrorLastNameEmpty()
        {
            string lastName = "";
            string result = SignUpServerValidator.ValidateLastName(lastName);
            Assert.Equal(SignUpServerValidator.ERROR_LAST_NAME_EMPTY, result);
        }

        [Fact]
        public void ValidateLastName_LastNameTooLong_ReturnsErrorLastNameLength()
        {
            string lastName = "PerezLopezPerezLopezPerezLopezPerezLopezPerezLopez1";
            string result = SignUpServerValidator.ValidateLastName(lastName);
            Assert.Equal(SignUpServerValidator.ERROR_LAST_NAME_LENGTH, result);
        }

        [Fact]
        public void ValidateLastName_LastNameWithSymbols_ReturnsErrorLastNameInvalidChars()
        {
            string lastName = "Perez$";
            string result = SignUpServerValidator.ValidateLastName(lastName);
            Assert.Equal(SignUpServerValidator.ERROR_LAST_NAME_INVALID_CHARS, result);
        }

        [Fact]
        public void ValidateNickname_ValidNickname_ReturnsEmptyString()
        {
            string nickname = "Gamer123";
            string result = SignUpServerValidator.ValidateNickname(nickname);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ValidateNickname_EmptyNickname_ReturnsErrorNicknameEmpty()
        {
            string nickname = "";
            string result = SignUpServerValidator.ValidateNickname(nickname);
            Assert.Equal(SignUpServerValidator.ERROR_NICKNAME_EMPTY, result);
        }

        [Fact]
        public void ValidateNickname_NicknameTooLong_ReturnsErrorNicknameLength()
        {
            string nickname = "Gamer12345678901";
            string result = SignUpServerValidator.ValidateNickname(nickname);
            Assert.Equal(SignUpServerValidator.ERROR_NICKNAME_LENGTH, result);
        }

        [Fact]
        public void ValidateNickname_NicknameWithSpace_ReturnsErrorNicknameInvalidChars()
        {
            string nickname = "Gamer 123";
            string result = SignUpServerValidator.ValidateNickname(nickname);
            Assert.Equal(SignUpServerValidator.ERROR_NICKNAME_INVALID_CHARS, result);
        }

        [Fact]
        public void ValidateEmail_ValidEmail_ReturnsEmptyString()
        {
            string email = "usuario@ejemplo.com";
            string result = SignUpServerValidator.ValidateEmail(email);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ValidateEmail_EmptyEmail_ReturnsErrorEmailEmpty()
        {
            string email = "   ";
            string result = SignUpServerValidator.ValidateEmail(email);
            Assert.Equal(SignUpServerValidator.ERROR_EMAIL_EMPTY, result);
        }

        [Fact]
        public void ValidateEmail_EmailTooLong_ReturnsErrorEmailLength()
        {
            string email = "usuariomuycargadodetextoydominioextremolargo@test.com";
            string result = SignUpServerValidator.ValidateEmail(email);
            Assert.Equal(SignUpServerValidator.ERROR_EMAIL_LENGTH, result);
        }

        [Fact]
        public void ValidateEmail_EmailInvalidFormat_ReturnsErrorEmailInvalidFormat()
        {
            string email = "usuarioejemplo.com";
            string result = SignUpServerValidator.ValidateEmail(email);
            Assert.Equal(SignUpServerValidator.ERROR_EMAIL_INVALID_FORMAT, result);
        }

        [Fact]
        public void ValidatePassword_ValidPassword_ReturnsEmptyString()
        {
            string password = "Password1$";
            string result = SignUpServerValidator.ValidatePassword(password);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ValidatePassword_EmptyPassword_ReturnsErrorPasswordEmpty()
        {
            string password = "";
            string result = SignUpServerValidator.ValidatePassword(password);
            Assert.Equal(SignUpServerValidator.ERROR_PASSWORD_EMPTY, result);
        }

        [Fact]
        public void ValidatePassword_PasswordTooShort_ReturnsErrorPasswordLength()
        {
            string password = "Pas1$";
            string result = SignUpServerValidator.ValidatePassword(password);
            Assert.Equal(SignUpServerValidator.ERROR_PASSWORD_LENGTH, result);
        }

        [Fact]
        public void ValidatePassword_PasswordTooLong_ReturnsErrorPasswordLength()
        {
            string password = "PasswordMuyLargo123$";
            string result = SignUpServerValidator.ValidatePassword(password);
            Assert.Equal(SignUpServerValidator.ERROR_PASSWORD_LENGTH, result);
        }

        [Fact]
        public void ValidatePassword_PasswordWithSpaces_ReturnsErrorPasswordNoSpaces()
        {
            string password = "Password 1$";
            string result = SignUpServerValidator.ValidatePassword(password);
            Assert.Equal(SignUpServerValidator.ERROR_PASSWORD_NO_SPACES, result);
        }

        [Fact]
        public void ValidatePassword_PasswordWithoutUppercase_ReturnsErrorPasswordNoUppercase()
        {
            string password = "password1$";
            string result = SignUpServerValidator.ValidatePassword(password);
            Assert.Equal(SignUpServerValidator.ERROR_PASSWORD_NO_UPPERCASE, result);
        }

        [Fact]
        public void ValidatePassword_PasswordWithoutSpecialChar_ReturnsErrorPasswordNoSpecialChar()
        {
            string password = "Password123";
            string result = SignUpServerValidator.ValidatePassword(password);
            Assert.Equal(SignUpServerValidator.ERROR_PASSWORD_NO_SPECIAL_CHAR, result);
        }
    }
}
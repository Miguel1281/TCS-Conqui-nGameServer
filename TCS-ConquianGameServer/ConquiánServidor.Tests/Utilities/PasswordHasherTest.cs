using ConquiánServidor.Utilities;
using System;
using Xunit;

namespace ConquiánServidor.Tests.Utilities
{
    public class PasswordHasherTest
    {
        [Fact]
        public void HashPassword_ValidPassword_ReturnsNonEmptyString()
        {
            string password = "Password123!";

            string result = PasswordHasher.hashPassword(password);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.NotEqual(password, result);
        }

        [Fact]
        public void HashPassword_SamePasswordTwice_ReturnsDifferentHashes()
        {
            string password = "SecretPassword";

            string hash1 = PasswordHasher.hashPassword(password);
            string hash2 = PasswordHasher.hashPassword(password);

            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void HashPassword_NullPassword_ThrowsArgumentNullException()
        {
            string password = null;

            Assert.Throws<ArgumentNullException>(() => PasswordHasher.hashPassword(password));
        }

        [Fact]
        public void HashPassword_EmptyString_ReturnsValidHash()
        {
            string password = "";

            string result = PasswordHasher.hashPassword(password);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public void VerifyPassword_CorrectPasswordAndHash_ReturnsTrue()
        {
            string password = "MyPassword";
            string hash = PasswordHasher.hashPassword(password);

            bool result = PasswordHasher.verifyPassword(password, hash);

            Assert.True(result);
        }

        [Fact]
        public void VerifyPassword_WrongPassword_ReturnsFalse()
        {
            string password = "CorrectPassword";
            string wrongPassword = "WrongPassword";
            string hash = PasswordHasher.hashPassword(password);

            bool result = PasswordHasher.verifyPassword(wrongPassword, hash);

            Assert.False(result);
        }

        [Fact]
        public void VerifyPassword_NullHash_ThrowsArgumentNullException()
        {
            string password = "Password";
            string hash = null;

            Assert.Throws<ArgumentNullException>(() => PasswordHasher.verifyPassword(password, hash));
        }

        [Fact]
        public void VerifyPassword_InvalidHashFormat_ThrowsException()
        {
            string password = "Password";
            string invalidHash = "NotABcryptHash";

            Assert.ThrowsAny<Exception>(() => PasswordHasher.verifyPassword(password, invalidHash));
        }
    }
}

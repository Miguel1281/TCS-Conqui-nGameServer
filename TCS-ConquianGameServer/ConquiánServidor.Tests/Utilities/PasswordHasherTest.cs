using Xunit;
using ConquiánServidor.Utilities;
using System;

namespace ConquiánServidor.Tests.Utilities
{
    public class PasswordHasherTest
    {
        [Fact]
        public void HashPassword_ValidPassword_ReturnsNonEmptyString()
        {
            string password = "TestPassword123!";
            string result = PasswordHasher.hashPassword(password);
            Assert.False(string.IsNullOrEmpty(result));
        }

        [Fact]
        public void HashPassword_SamePasswordTwice_ReturnsDifferentHashes()
        {
            string password = "SamePassword";
            string hash1 = PasswordHasher.hashPassword(password);
            string hash2 = PasswordHasher.hashPassword(password);
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void HashPassword_NullPassword_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => PasswordHasher.hashPassword(null));
        }

        [Fact]
        public void VerifyPassword_CorrectPassword_ReturnsTrue()
        {
            string password = "MySecretPassword";
            string hash = PasswordHasher.hashPassword(password);
            bool result = PasswordHasher.verifyPassword(password, hash);
            Assert.True(result);
        }

        [Fact]
        public void VerifyPassword_IncorrectPassword_ReturnsFalse()
        {
            string password = "CorrectPassword";
            string wrongPassword = "WrongPassword";
            string hash = PasswordHasher.hashPassword(password);
            bool result = PasswordHasher.verifyPassword(wrongPassword, hash);
            Assert.False(result);
        }

        [Fact]
        public void VerifyPassword_EmptyPasswordAgainstHash_ReturnsFalse()
        {
            string password = "Password";
            string hash = PasswordHasher.hashPassword(password);
            bool result = PasswordHasher.verifyPassword("", hash);
            Assert.False(result);
        }
    }
}
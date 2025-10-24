using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConquiánServidor.BusinessLogic.Validation
{
    public static class SignUpServerValidator
    {
        private const int MAX_NAME_LENGTH = 25;
        private const int MAX_LAST_NAME_LENGTH = 50;
        private const int MAX_NICKNAME_LENGTH = 15;
        private const int MAX_EMAIL_LENGTH = 45;
        private const int MIN_PASSWORD_LENGTH = 8;
        private const int MAX_PASSWORD_LENGTH = 15;

        public const string ERROR_NAME_EMPTY = "ERROR_NAME_EMPTY";
        public const string ERROR_NAME_LENGTH = "ERROR_NAME_LENGTH";
        public const string ERROR_VALID_NAME = "ERROR_VALID_NAME";
        public const string ERROR_LAST_NAME_EMPTY = "ERROR_LAST_NAME_EMPTY";
        public const string ERROR_LAST_NAME_LENGTH = "ERROR_LAST_NAME_LENGTH";
        public const string ERROR_LAST_NAME_INVALID_CHARS = "ERROR_LAST_NAME_INVALID_CHARS";
        public const string ERROR_NICKNAME_EMPTY = "ERROR_NICKNAME_EMPTY";
        public const string ERROR_NICKNAME_LENGTH = "ERROR_NICKNAME_LENGTH";
        public const string ERROR_NICKNAME_INVALID_CHARS = "ERROR_NICKNAME_INVALID_CHARS";
        public const string ERROR_EMAIL_EMPTY = "ERROR_EMAIL_EMPTY";
        public const string ERROR_EMAIL_LENGTH = "ERROR_EMAIL_LENGTH";
        public const string ERROR_EMAIL_INVALID_FORMAT = "ERROR_EMAIL_INVALID_FORMAT";
        public const string ERROR_PASSWORD_EMPTY = "ERROR_PASSWORD_EMPTY";
        public const string ERROR_PASSWORD_LENGTH = "ERROR_PASSWORD_LENGTH";
        public const string ERROR_PASSWORD_NO_SPACES = "ERROR_PASSWORD_NO_SPACES";
        public const string ERROR_PASSWORD_NO_UPPERCASE = "ERROR_PASSWORD_NO_UPPERCASE";
        public const string ERROR_PASSWORD_NO_SPECIAL_CHAR = "ERROR_PASSWORD_NO_SPECIAL_CHAR";

        public static string ValidateName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return ERROR_NAME_EMPTY;
            }
            if (name.Length > MAX_NAME_LENGTH)
            {
                return ERROR_NAME_LENGTH;
            }
            if (!Regex.IsMatch(name, @"^[a-zA-Z\s]+$"))
            {
                return ERROR_VALID_NAME;
            }
            return string.Empty;
        }

        public static string ValidateLastName(string lastName)
        {
            if (string.IsNullOrEmpty(lastName))
            {
                return ERROR_LAST_NAME_EMPTY;
            }
            lastName = lastName.Trim();
            if (lastName.Length > MAX_LAST_NAME_LENGTH)
            {
                return ERROR_LAST_NAME_LENGTH;
            }
            if (!Regex.IsMatch(lastName, @"^[a-zA-Z\s]+$"))
            {
                return ERROR_LAST_NAME_INVALID_CHARS;
            }
            return string.Empty;
        }

        public static string ValidateNickname(string nickname)
        {
            if (string.IsNullOrEmpty(nickname))
            {
                return ERROR_NICKNAME_EMPTY;
            }
            nickname = nickname.Trim();
            if (nickname.Length > MAX_NICKNAME_LENGTH)
            {
                return ERROR_NICKNAME_LENGTH;
            }
            if (!Regex.IsMatch(nickname, @"^[a-zA-Z0-9]+$"))
            {
                return ERROR_NICKNAME_INVALID_CHARS;
            }
            return string.Empty;
        }

        public static string ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return ERROR_EMAIL_EMPTY;
            }
            email = email.Trim();
            if (email.Length > MAX_EMAIL_LENGTH)
            {
                return ERROR_EMAIL_LENGTH;
            }
            string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$";
            if (!Regex.IsMatch(email, emailPattern))
            {
                return ERROR_EMAIL_INVALID_FORMAT;
            }
            return string.Empty;
        }

        public static string ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return ERROR_PASSWORD_EMPTY;
            }
            if (password.Length < MIN_PASSWORD_LENGTH || password.Length > MAX_PASSWORD_LENGTH)
            {
                return ERROR_PASSWORD_LENGTH;
            }
            if (password.Contains(" "))
            {
                return ERROR_PASSWORD_NO_SPACES;
            }
            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                return ERROR_PASSWORD_NO_UPPERCASE;
            }
            if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]"))
            {
                return ERROR_PASSWORD_NO_SPECIAL_CHAR;
            }
            return string.Empty;
        }
    }
}

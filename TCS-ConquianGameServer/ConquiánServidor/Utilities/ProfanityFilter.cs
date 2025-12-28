using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace ConquiánServidor.Utilities
{
    public static class ProfanityFilter
    {
        private static readonly HashSet<string> Blacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "puta", "puto", "pendejo", "pendeja", "mierda", "verga", "chinga", "chingar",
            "cabron", "cabrona", "zorra", "estupido", "idiota", "maricon", "culero", "pinche",
            "mamon", "mamona", "imbecil", "joto", "vergas", 
            "fuck", "shit", "bitch", "asshole", "dick", "pussy", "bastard", "whore",
            "cunt", "nigger", "faggot", "slut", "motherfucker", "cock"
        };

        private static string pattern;
        private static Regex regex;

        static ProfanityFilter()
        {
            InitializeRegex();
        }

        private static void InitializeRegex()
        {

            var escapedWords = Blacklist.Select(Regex.Escape);
            pattern = $@"\b({string.Join("|", escapedWords)})\b";

            regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public static string CensorMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return message;

            return regex.Replace(message, "*****");
        }

        public static void AddWord(string word)
        {
            if (!Blacklist.Contains(word))
            {
                Blacklist.Add(word);
                InitializeRegex(); 
            }
        }
    }
}
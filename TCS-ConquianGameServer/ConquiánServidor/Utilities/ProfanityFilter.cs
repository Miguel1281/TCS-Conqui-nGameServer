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
            "puta", "puto", "putos", "putas",
            "pendejo", "pendeja", "pendejos", "pendejas",
            "mierda", "mierdas",
            "verga", "vergas", "vergazo",
            "chinga", "chingar", "chingada", "chingado", "chingados", "chingadas", "chingatumadre",
            "cabron", "cabrona", "cabrones", "cabronas",
            "zorra", "zorro",
            "estupido", "estupida", "estupidos", "estupidas",
            "idiota", "idiotas",
            "maricon", "marica", "maricones",
            "culero", "culeros", "culo",
            "pinche", "pinches",
            "mamon", "mamona", "mamones", "mamonas",
            "imbecil", "imbeciles",
            "joto", "jotos",
            "bastardo", "bastarda",
            "pito", "coño", "cagada", "cagar", "tarado", "tarada", "malparido", "malparida",
            "soplapollas", "gilipollas", "capullo",

            "fuck", "fucking", "fucked", "fucker",
            "shit", "bullshit",
            "bitch", "bitches",
            "asshole", "ass",
            "dick", "cock",
            "pussy",
            "bastard",
            "whore",
            "cunt",
            "nigger", "nigga",
            "faggot", "fag",
            "slut",
            "motherfucker",
            "douchebag", "wanker"
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
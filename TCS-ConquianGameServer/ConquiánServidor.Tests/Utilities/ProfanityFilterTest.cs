using Xunit;
using ConquiánServidor.Utilities;

namespace ConquiánServidor.Tests.Utilities
{
    public class ProfanityFilterTest
    {
        [Fact]
        public void CensorMessage_ValidCleanMessage_ReturnsOriginalMessage()
        {
            string message = "Hola mundo todo bien";
            string result = ProfanityFilter.CensorMessage(message);
            Assert.Equal(message, result);
        }

        [Fact]
        public void CensorMessage_NullMessage_ReturnsNull()
        {
            string message = null;
            string result = ProfanityFilter.CensorMessage(message);
            Assert.Null(result);
        }

        [Fact]
        public void CensorMessage_EmptyMessage_ReturnsEmptyString()
        {
            string message = "";
            string result = ProfanityFilter.CensorMessage(message);
            Assert.Equal("", result);
        }

        [Fact]
        public void CensorMessage_MessageWithProfanityLowercase_ReturnsCensoredString()
        {
            string message = "eres un pendejo";
            string result = ProfanityFilter.CensorMessage(message);
            Assert.Equal("eres un *****", result);
        }

        [Fact]
        public void CensorMessage_MessageWithProfanityUppercase_ReturnsCensoredString()
        {
            string message = "ERES UN PENDEJO";
            string result = ProfanityFilter.CensorMessage(message);
            Assert.Equal("ERES UN *****", result);
        }

        [Fact]
        public void CensorMessage_MessageWithMixedCaseProfanity_ReturnsCensoredString()
        {
            string message = "No digas MiErDa por favor";
            string result = ProfanityFilter.CensorMessage(message);
            Assert.Equal("No digas ***** por favor", result);
        }

        [Fact]
        public void CensorMessage_MessageWithMultipleProfanities_ReturnsAllCensored()
        {
            string message = "puta zorra y verga";
            string result = ProfanityFilter.CensorMessage(message);
            Assert.Equal("***** ***** y *****", result);
        }

        [Fact]
        public void CensorMessage_ProfanityInsideLegitimateWord_ReturnsUnchangedString()
        {
            string message = "La computadora es rapida";
            string result = ProfanityFilter.CensorMessage(message);
            Assert.Equal("La computadora es rapida", result);
        }

        [Fact]
        public void CensorMessage_ProfanityWithPunctuation_ReturnsCensoredString()
        {
            string message = "¡Eres un idiota!";
            string result = ProfanityFilter.CensorMessage(message);
            Assert.Equal("¡Eres un *****!", result);
        }

        [Fact]
        public void AddWord_NewWordAdded_ReturnsCensoredNewWord()
        {
            string newBadWord = "palabranueva123";
            ProfanityFilter.AddWord(newBadWord);
            string message = $"No digas {newBadWord} aqui";

            string result = ProfanityFilter.CensorMessage(message);

            Assert.Equal("No digas ***** aqui", result);
        }

        [Fact]
        public void AddWord_ExistingWordAdded_ReturnsCensoredExistingWordWithoutError()
        {
            string existingWord = "puta";
            ProfanityFilter.AddWord(existingWord);
            string message = "puta";

            string result = ProfanityFilter.CensorMessage(message);

            Assert.Equal("*****", result);
        }
    }
}
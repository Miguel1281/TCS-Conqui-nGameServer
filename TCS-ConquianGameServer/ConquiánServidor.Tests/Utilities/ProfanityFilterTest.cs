using ConquiánServidor.Utilities;
using Xunit;

namespace ConquiánServidor.Tests.Utilities
{
    public class ProfanityFilterTest
    {
        [Fact]
        public void CensorMessage_MessageWithBadWord_ReturnsCensoredString()
        {
            string message = "Eres un idiota";

            string result = ProfanityFilter.CensorMessage(message);

            Assert.Equal("Eres un *****", result);
        }

        [Fact]
        public void CensorMessage_MessageWithMultipleBadWords_ReturnsAllCensored()
        {
            string message = "pinche cabron";

            string result = ProfanityFilter.CensorMessage(message);

            Assert.Equal("***** *****", result);
        }

        [Fact]
        public void CensorMessage_MessageWithMixedCaseBadWord_ReturnsCensoredString()
        {
            string message = "No seas PuTo";

            string result = ProfanityFilter.CensorMessage(message);

            Assert.Equal("No seas *****", result);
        }

        [Fact]
        public void CensorMessage_MessageWithoutBadWords_ReturnsOriginalString()
        {
            string message = "Hola amigo como estas";

            string result = ProfanityFilter.CensorMessage(message);

            Assert.Equal(message, result);
        }

        [Fact]
        public void CensorMessage_WordInsideAnotherWord_DoesNotCensor()
        {
            string message = "La computadora es nueva";

            string result = ProfanityFilter.CensorMessage(message);

            Assert.Equal(message, result);
        }

        [Fact]
        public void CensorMessage_NullInput_ReturnsNull()
        {
            string message = null;

            string result = ProfanityFilter.CensorMessage(message);

            Assert.Null(result);
        }

        [Fact]
        public void CensorMessage_EmptyString_ReturnsEmptyString()
        {
            string message = "";

            string result = ProfanityFilter.CensorMessage(message);

            Assert.Equal("", result);
        }

        [Fact]
        public void CensorMessage_WhitespaceString_ReturnsWhitespace()
        {
            string message = "   ";

            string result = ProfanityFilter.CensorMessage(message);

            Assert.Equal("   ", result);
        }

        [Fact]
        public void AddWord_NewWordAdded_CensorsNewWord()
        {
            string newBadWord = "palabranueva";
            string message = "Esto es una palabranueva";

            ProfanityFilter.AddWord(newBadWord);
            string result = ProfanityFilter.CensorMessage(message);

            Assert.Equal("Esto es una *****", result);
        }

        [Fact]
        public void AddWord_ExistingWordAdded_DoesNotCrashAndStillCensors()
        {
            string existingWord = "mierda";
            string message = "Vaya mierda";

            ProfanityFilter.AddWord(existingWord);
            string result = ProfanityFilter.CensorMessage(message);

            Assert.Equal("Vaya *****", result);
        }
    }
}

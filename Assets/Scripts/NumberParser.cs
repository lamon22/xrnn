using System.Collections.Generic;
using System.Linq;

public static class NumberParser
{
    private static readonly Dictionary<string, int> numberWords = new Dictionary<string, int>
    {
        {"zero", 0}, {"one", 1}, {"two", 2}, {"three", 3}, {"four", 4},
        {"five", 5}, {"six", 6}, {"seven", 7}, {"eight", 8}, {"nine", 9},
        {"ten", 10}, {"eleven", 11}, {"twelve", 12}, {"thirteen", 13},
        {"fourteen", 14}, {"fifteen", 15}, {"sixteen", 16}, {"seventeen", 17},
        {"eighteen", 18}, {"nineteen", 19}, {"twenty", 20}, {"twenty-one", 21},
        {"twenty-two", 22}, {"twenty-three", 23}, {"twenty-four", 24},
        {"twenty-five", 25}, {"twenty-six", 26}, {"twenty-seven", 27},
        {"twenty-eight", 28}, {"twenty-nine", 29}, {"thirty", 30},
        {"thirty-one", 31}, {"thirty-two", 32}, {"thirty-three", 33},
        {"thirty-four", 34}, {"thirty-five", 35}, {"thirty-six", 36},
        {"thirty-seven", 37}, {"thirty-eight", 38}, {"thirty-nine", 39},
        {"forty", 40}, {"forty-one", 41}, {"forty-two", 42},
        {"forty-three", 43}, {"forty-four", 44}, {"forty-five", 45},
        {"forty-six", 46}, {"forty-seven", 47}, {"forty-eight", 48},
        {"forty-nine", 49}, {"fifty", 50}
    };

    public static bool TryParse(string word, out int result)
    {
        if (string.IsNullOrEmpty(word))
        {
            result = 0;
            return false;
        }

        char[] charsToTrim = { ',', '.', '?', '!', ';', ':' };
        string cleanedWord = word.Trim(charsToTrim);

        if (int.TryParse(cleanedWord, out result))
        {
            return true;
        }

        if (numberWords.TryGetValue(cleanedWord.ToLower().Replace(" ", "-"), out result))
        {
            return true;
        }

        result = 0;
        return false;
    }

    public static List<int> ParseNumberWordsArray(string[] numberWords)
    {
        var foundNumbers = new List<int>();
        if (numberWords == null)
        {
            return foundNumbers;
        }

        foreach (var word in numberWords)
        {
            if (TryParse(word, out int number))
            {
                foundNumbers.Add(number);
            }
        }

        return foundNumbers;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;

using Microsoft.VisualStudio.TextManager.Interop;
using WordLight.Extensions;

namespace WordLight.Search
{
    /// <remarks>
    /// Modification of Boyer–Moore string search
    /// Based on http://algolist.manual.ru/search/esearch/qsearch.php
    /// </remarks>
    public class BoyerMooreStringSearch
    {
        private bool _caseSensitiveSearch;
        private bool _searchWholeWordsOnly;
        private string _sample;
        private Dictionary<int, int> _badChars;

        public string Sample
        {
            get { return _sample; }
        }

        public BoyerMooreStringSearch(string sample)
        {
            if (string.IsNullOrEmpty(sample)) throw new ArgumentException("sample");

            _sample = sample;

            _caseSensitiveSearch = AddinSettings.Instance.CaseSensitiveSearch;
            _searchWholeWordsOnly = AddinSettings.Instance.SearchWholeWordsOnly;

            _badChars = CalculateDistancesForBadChars(sample);
        }

        private Dictionary<int, int> CalculateDistancesForBadChars(string sample)
        {
            int length = sample.Length;
            var badChars = new Dictionary<int, int>(length);

            for (int i = 0; i < length; i++)
            {
                char c = sample[i];

                if (_caseSensitiveSearch)
                    badChars[c] = length - i;
                else
                {
                    badChars[char.ToLower(c)] = length - i;
                    badChars[char.ToUpper(c)] = length - i;
                }
            }

            return badChars;
        }

        private static bool IsWordCharacter(char c)
        {
            return
                char.IsLetterOrDigit(c) ||
                char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.ConnectorPunctuation;
        }

        public TextOccurences SearchOccurrencesInText(string text, int searchStart, int searchEnd)
        {
			int textLength = text.Length;
            int sampleLength = _sample.Length;

			//Make sure, that the search range is not out of the text
			searchStart = Math.Max(0, searchStart);
			searchEnd = Math.Min(searchEnd, textLength);

            var positions = new TreapBuilder();            

            /* Searching */
            var comparsion = (_caseSensitiveSearch ? 
                StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase);

            int searchLoopEnd = Math.Min(searchEnd, text.Length) - (sampleLength - 1);
			for (int i = searchStart; i < searchLoopEnd; )
            {
                if (text.Substring(i, sampleLength).StartsWith(_sample, comparsion))
                {
                    bool occurrenceFound = true;

                    if (_searchWholeWordsOnly)
                    {
                        int previousCharIndex = i - 1;
                        int nextCharIndex = i + sampleLength;

                        bool isPreviousCharPartOfWord = previousCharIndex >= 0 && IsWordCharacter(text[previousCharIndex]);
                        bool isNextCharPartOfWord = nextCharIndex < textLength && IsWordCharacter(text[nextCharIndex]);

                        occurrenceFound = !isPreviousCharPartOfWord && !isNextCharPartOfWord;
                    }
                    
                    if (occurrenceFound)
                        positions.Add(i);

                    //Don't search inside a found substring (no crossed search marks).
                    i += sampleLength; 
                    continue;
                }

                if (i + sampleLength >= searchEnd)
                    break;

                int key = text[i + sampleLength];
                if (_badChars.ContainsKey(key))
                    i += _badChars[key];
                else
                    i += sampleLength + 1;
            }

            return new TextOccurences(_sample, positions);
        }
    }
}

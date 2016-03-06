using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToriatamaText
{
    public class Validator
    {
        private readonly Extractor _extractor;

        public int MaxTweetLength { get; set; } = 140;
        public int ShortUrlLength { get; set; } = 23;
        // we no longer need to separate ShortUrlLength and ShortUrlLengthHttps

        public Validator(Extractor extractor)
        {
            this._extractor = extractor;
        }

        public Validator() : this(new Extractor()) { }

        public int GetTweetLength(string text)
        {
            throw new NotImplementedException();
        }

        private static readonly char[] InvalidTweetChars =
        {
            '\uFFFE', '\uFEFF', '\uFFFF',
            '\u202A', '\u202B', '\u202C', '\u202D', '\u202E'
        };

        public bool IsValidTweet(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOfAny(InvalidTweetChars) != -1)
                return false;

            return this.GetTweetLength(text) <= this.MaxTweetLength;
        }
    }
}

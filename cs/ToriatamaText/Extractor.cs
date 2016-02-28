using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ToriatamaText.InternalExtractors;

namespace ToriatamaText
{
    public class Extractor
    {
        private readonly Dictionary<int, TldInfo> _tldDictionary = new Dictionary<int, TldInfo>();
        private int _longestTldLength;
        private int _shortestTldLength = int.MaxValue;

        public bool ExtractsUrlWithoutProtocol { get; set; }

        public Extractor(IEnumerable<string> gTlds, IEnumerable<string> ccTlds)
        {
            if (gTlds != null)
                foreach (var x in gTlds) this.AddTld(x, TldType.GTld);

            if (ccTlds != null)
            {
                foreach (var x in ccTlds)
                {
                    if (!x.Equals("co", StringComparison.OrdinalIgnoreCase) && !x.Equals("tv", StringComparison.OrdinalIgnoreCase))
                        this.AddTld(x, TldType.CcTld);
                }
            }

            this.AddTld("co", TldType.SpecialCcTld);
            this.AddTld("tv", TldType.SpecialCcTld);
        }

        public Extractor() : this(DefaultTlds.GTlds, DefaultTlds.CTlds) { }

        private void AddTld(string tld, TldType type)
        {
            var len = tld.Length;
            if (len > this._longestTldLength)
                this._longestTldLength = len;
            else if (len < this._shortestTldLength)
                this._shortestTldLength = len;

            this._tldDictionary.Add(Utils.CaseInsensitiveHashCode(tld), new TldInfo(type, len));
            // ハッシュが被ったら知らん
        }

        public List<EntityInfo> ExtractEntities(string text)
        {
            throw new NotImplementedException();
        }

        public List<EntityInfo> ExtractMentionedScreenNames(string text)
        {
            var result = new List<EntityInfo>();
            if (!string.IsNullOrEmpty(text))
                MentionExtractor.Extract(text, false, result);
            return result;
        }

        public List<EntityInfo> ExtractMentionsOrLists(string text)
        {
            var result = new List<EntityInfo>();
            if (!string.IsNullOrEmpty(text))
                MentionExtractor.Extract(text, true, result);
            return result;
        }

        public EntityInfo? ExtractReplyScreenName(string text)
        {
            throw new NotImplementedException();
        }

        public List<EntityInfo> ExtractUrls(string text)
        {
            var result = new List<EntityInfo>();
            if (!string.IsNullOrEmpty(text))
                UrlExtractor.Extract(text, this._tldDictionary, this._longestTldLength, this._shortestTldLength, result);
            return result;
        }

        public List<EntityInfo> ExtractHashtags(string text)
        {
            throw new NotImplementedException();
        }

        public List<EntityInfo> ExtractCashtags(string text)
        {
            throw new NotImplementedException();
        }
    }
}

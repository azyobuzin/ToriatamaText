using System.Linq;

#pragma warning disable CS0659 // 型は Object.Equals(object o) をオーバーライドしますが、Object.GetHashCode() をオーバーライドしません

namespace ToriatamaText.Test
{
    class ExtractYaml
    {
        public ExtractorTests Tests { get; set; }
    }

    class ExtractorTests
    {
        public TestItem<string[]>[] Mentions { get; set; }
        public TestItem<MentionsWithIndicesExpected[]>[] MentionsWithIndices { get; set; }
        public TestItem<MentionsOrListsWithIndicesExpected[]>[] MentionsOrListsWithIndices { get; set; }
        public TestItem<string>[] Replies { get; set; }
        public TestItem<string[]>[] Urls { get; set; }
        public TestItem<UrlsWithIndicesExpected[]>[] UrlsWithIndices { get; set; }
        public TestItem<string[]>[] Hashtags { get; set; }
        public TestItem<HashtagsWithIndicesExpected[]>[] HashtagsWithIndices { get; set; }
        public TestItem<string[]>[] Cashtags { get; set; }
        public TestItem<CashtagsWithIndicesExpected[]>[] CashtagsWithIndices { get; set; }
    }

    class TestItem<TExpected>
    {
        public string Description { get; set; }
        public string Text { get; set; }
        public TExpected Expected { get; set; }
    }

    class MentionsWithIndicesExpected
    {
        public string ScreenName { get; set; }
        public int[] Indices { get; set; }

        public override bool Equals(object obj)
        {
            var x = obj as MentionsWithIndicesExpected;
            if (x == null) return false;
            return this.ScreenName == x.ScreenName && this.Indices.SequenceEqual(x.Indices);
        }
    }

    class MentionsOrListsWithIndicesExpected
    {
        public string ScreenName { get; set; }
        public string ListSlug { get; set; }
        public int[] Indices { get; set; }

        public override bool Equals(object obj)
        {
            var x = obj as MentionsOrListsWithIndicesExpected;
            if (x == null) return false;
            return this.ScreenName == x.ScreenName && this.ListSlug == x.ListSlug && this.Indices.SequenceEqual(x.Indices);
        }
    }

    class UrlsWithIndicesExpected
    {
        public string Url { get; set; }
        public int[] Indices { get; set; }

        public override bool Equals(object obj)
        {
            var x = obj as UrlsWithIndicesExpected;
            if (x == null) return false;
            return this.Url == x.Url && this.Indices.SequenceEqual(x.Indices);
        }
    }

    class HashtagsWithIndicesExpected
    {
        public string Hashtag { get; set; }
        public int[] Indices { get; set; }

        public override bool Equals(object obj)
        {
            var x = obj as HashtagsWithIndicesExpected;
            if (x == null) return false;
            return this.Hashtag == x.Hashtag && this.Indices.SequenceEqual(x.Indices);
        }
    }

    class CashtagsWithIndicesExpected
    {
        public string Cashtag { get; set; }
        public int[] Indices { get; set; }

        public override bool Equals(object obj)
        {
            var x = obj as CashtagsWithIndicesExpected;
            if (x == null) return false;
            return this.Cashtag == x.Cashtag && this.Indices.SequenceEqual(x.Indices);
        }
    }
}

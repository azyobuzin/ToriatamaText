namespace ToriatamaText.InternalExtractors
{
    using static Utils;

    static class ReplyExtractor
    {
        public static string Extract(string text)
        {
            var startIndex = 0;

            while (true)
            {
                var c = text[startIndex++];
                if (c == '@' || c == '＠')
                {
                    if (startIndex < text.Length)
                    {
                        c = text[startIndex];
                        if (c < AsciiTableLength && (AsciiTable[c] & (CharType.Alnum | CharType.ScreenNameSymbol)) != 0)
                            break;
                    }
                    return null;
                }

                // whitespace チェック
                if ((c >= '\u0009' && c <= '\u000d') || c == '\u0020' || c == '\u0085' || c == '\u00a0' || c == '\u1680'
                    || c == '\u180E' || (c >= '\u2000' && c <= '\u200a') || c == '\u2028' || c == '\u2029'
                    || c == '\u202F' || c == '\u205F' || c == '\u3000')
                {
                    if (text.Length == startIndex) return null;
                    continue;
                }

                return null;
            }

            var nextIndex = startIndex + 1;
            var i = 0;
            while (true)
            {
                if (nextIndex == text.Length)
                    return text.Substring(startIndex);

                var c = text[nextIndex++];
                if (c < AsciiTableLength)
                {
                    if ((AsciiTable[c] & (CharType.Alnum | CharType.ScreenNameSymbol)) != 0)
                    {
                        if (i < 19)
                        {
                            i++;
                            continue;
                        }
                        return null;
                    }

                    if (c == '@' || (c == ':' && nextIndex + 1 < text.Length && text[nextIndex] == '/' && text[nextIndex + 1] == '/'))
                        return null;
                }
                else {
                    if (c == '＠' || IsAccentChar(c))
                        return null;
                }

                return text.Substring(startIndex, nextIndex - startIndex - 1);
            }
        }
    }
}

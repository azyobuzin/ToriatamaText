using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToriatamaText.InternalExtractors
{
    static class Utils
    {
        public static int ToLower(char c)
        {
            return c <= 'Z' && c >= 'A' ? (c + 32) : c;
        }

        public static int CaseInsensitiveHashCode(string str)
        {
            var hash1 = 5381;
            var hash2 = hash1;

            for (var i = 0; i < str.Length;)
            {
                hash1 = ((hash1 << 5) + hash1) ^ ToLower(str[i++]);
                if (i >= str.Length) break;
                hash2 = ((hash2 << 5) + hash2) ^ ToLower(str[i++]);
            }

            return hash1 + hash2 * 1566083941;
        }

        public static bool IsAccentChar(char c)
        {
            return (c >= '\u00c0' && c <= '\u00d6') || (c >= '\u00d8' && c <= '\u00f6') || (c >= '\u00f8' && c <= '\u024f')
                || c == '\u0253' || c == '\u0254' || c == '\u0256' || c == '\u0257' || c == '\u0259' || c == '\u025b' || c == '\u0263' || c == '\u0268' || c == '\u026f' || c == '\u0272' || c == '\u0289' || c == '\u028b'
                || c == '\u02bb'
                || (c >= '\u0300' && c <= '\u036f')
                || (c >= '\u1e00' && c <= '\u1eff');
        }

        [Flags]
        public enum CharType
        {
            None = 0,
            Alphabet = 1,
            Number = 1 << 1,
            At = 1 << 2,
            Alnum = Alphabet | Number,
            AlnumAt = Alnum | At,
            UrlNotPrecedingSymbol = 1 << 3,
            PathEndingSymbol = 1 << 4,
            PathSymbol = 1 << 5,
            QueryEndingSymbol = 1 << 6,
            QuerySymbol = 1 << 7,
            LParen = 1 << 8,
            RParen = 1 << 9,
            DomainSymbol = 1 << 10,
            MentionNotPrecedingSymbol = 1 << 11,
            ScreenNameSymbol = 1 << 12,
            ListSlugSymbol = 1 << 13
        }

        public static readonly CharType[] AsciiTable =
        {
            CharType.None, // NUL
            CharType.None, // SOH
            CharType.None, // STX
            CharType.None, // ETX
            CharType.None, // EOX
            CharType.None, // ENQ
            CharType.None, // ACK
            CharType.None, // BEL
            CharType.None, // BS
            CharType.None, // HT
            CharType.None, // LF
            CharType.None, // VT
            CharType.None, // FF
            CharType.None, // CR
            CharType.None, // SO
            CharType.None, // SI
            CharType.None, // DLE
            CharType.None, // DC1
            CharType.None, // DC2
            CharType.None, // DC3
            CharType.None, // DC4
            CharType.None, // NAK
            CharType.None, // SYN
            CharType.None, // ETB
            CharType.None, // CAN
            CharType.None, // EM
            CharType.None, // SUB
            CharType.None, // ESC
            CharType.None, // FS
            CharType.None, // GS
            CharType.None, // RS
            CharType.None, // US
            CharType.None, // Space
            CharType.PathSymbol | CharType.QuerySymbol | CharType.MentionNotPrecedingSymbol, // !
            CharType.None, // "
            CharType.UrlNotPrecedingSymbol | CharType.PathEndingSymbol | CharType.QueryEndingSymbol | CharType.MentionNotPrecedingSymbol, // #
            CharType.UrlNotPrecedingSymbol | CharType.PathSymbol | CharType.QuerySymbol | CharType.MentionNotPrecedingSymbol, // $
            CharType.PathSymbol | CharType.QuerySymbol | CharType.MentionNotPrecedingSymbol, // %
            CharType.PathSymbol | CharType.QueryEndingSymbol | CharType.MentionNotPrecedingSymbol, // &
            CharType.PathSymbol | CharType.QuerySymbol, // '
            CharType.QuerySymbol | CharType.LParen, // (
            CharType.QuerySymbol | CharType.RParen, // )
            CharType.PathSymbol | CharType.QuerySymbol | CharType.MentionNotPrecedingSymbol, // *
            CharType.PathEndingSymbol | CharType.QuerySymbol, // +
            CharType.PathSymbol | CharType.QuerySymbol, // ,
            CharType.PathEndingSymbol | CharType.QuerySymbol | CharType.DomainSymbol | CharType.ListSlugSymbol, // -
            CharType.PathSymbol | CharType.QuerySymbol, // .
            CharType.PathEndingSymbol | CharType.QueryEndingSymbol, // /
            CharType.Number, // 0
            CharType.Number, // 1
            CharType.Number, // 2
            CharType.Number, // 3
            CharType.Number, // 4
            CharType.Number, // 5
            CharType.Number, // 6
            CharType.Number, // 7
            CharType.Number, // 8
            CharType.Number, // 9
            CharType.PathSymbol | CharType.QuerySymbol, // :
            CharType.PathSymbol | CharType.QuerySymbol, // ;
            CharType.None, // <
            CharType.PathEndingSymbol | CharType.QueryEndingSymbol, // =
            CharType.None, // >
            CharType.QuerySymbol, // ?
            CharType.At | CharType.UrlNotPrecedingSymbol | CharType.PathSymbol | CharType.QuerySymbol, // @
            CharType.Alphabet, // A
            CharType.Alphabet, // B
            CharType.Alphabet, // C
            CharType.Alphabet, // D
            CharType.Alphabet, // E
            CharType.Alphabet, // F
            CharType.Alphabet, // G
            CharType.Alphabet, // H
            CharType.Alphabet, // I
            CharType.Alphabet, // J
            CharType.Alphabet, // K
            CharType.Alphabet, // L
            CharType.Alphabet, // M
            CharType.Alphabet, // N
            CharType.Alphabet, // O
            CharType.Alphabet, // P
            CharType.Alphabet, // Q
            CharType.Alphabet, // R
            CharType.Alphabet, // S
            CharType.Alphabet, // T
            CharType.Alphabet, // U
            CharType.Alphabet, // V
            CharType.Alphabet, // W
            CharType.Alphabet, // X
            CharType.Alphabet, // Y
            CharType.Alphabet, // Z
            CharType.PathSymbol | CharType.QuerySymbol, // [
            CharType.None, // \
            CharType.PathSymbol | CharType.QuerySymbol, // ]
            CharType.None, // ^
            CharType.PathEndingSymbol | CharType.QueryEndingSymbol | CharType.DomainSymbol | CharType.MentionNotPrecedingSymbol | CharType.ScreenNameSymbol | CharType.ListSlugSymbol, // _
            CharType.None, // `
            CharType.Alphabet, // a
            CharType.Alphabet, // b
            CharType.Alphabet, // c
            CharType.Alphabet, // d
            CharType.Alphabet, // e
            CharType.Alphabet, // f
            CharType.Alphabet, // g
            CharType.Alphabet, // h
            CharType.Alphabet, // i
            CharType.Alphabet, // j
            CharType.Alphabet, // k
            CharType.Alphabet, // l
            CharType.Alphabet, // m
            CharType.Alphabet, // n
            CharType.Alphabet, // o
            CharType.Alphabet, // p
            CharType.Alphabet, // q
            CharType.Alphabet, // r
            CharType.Alphabet, // s
            CharType.Alphabet, // t
            CharType.Alphabet, // u
            CharType.Alphabet, // v
            CharType.Alphabet, // w
            CharType.Alphabet, // x
            CharType.Alphabet, // y
            CharType.Alphabet, // z
            CharType.None, // {
            CharType.PathSymbol | CharType.QuerySymbol, // |
            CharType.None, // }
            CharType.PathSymbol | CharType.QuerySymbol, // ~
            CharType.None // DEL
        };

        public const int AsciiTableLength = 128;
    }
}

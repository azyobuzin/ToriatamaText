using System.Diagnostics;
using ToriatamaText.InternalExtractors;

namespace ToriatamaText.UnicodeNormalization
{
    using static Tables;
    using static Utils;

    static class NewSuperNfc
    {
        //TODO: CccAndQcTableにQCの値なくてもわかるじゃん？

        /// <returns><c>true</c>なら<param name="result" />に結果が入る。<c>false</c>なら既に正規化されている。</returns>
        public static bool Compose(string s, out MiniList<char> result)
        {
            // クイックチェック
            bool isFirstCharToNormalizeSurrogatePair;
            var i = IndexOfLastNormalizedChar(s, 0, out isFirstCharToNormalizeSurrogatePair);

            if (i == -1)
            {
                result = new MiniList<char>();
                return false;
            }

            // ここからが本番
            var list = StringToMiniList(s, i);

            while (true)
            {
                var nextQcYes = FindNextNfcQcYes(s, i + (isFirstCharToNormalizeSurrogatePair ? 2 : 1));
                DecomposeInRange(s, i, nextQcYes, ref list);
                if (i + 1 < list.Count)
                {
                    ReorderInRange(ref list, i);
                    ComposeInRange(ref list, i);
                }

                if (nextQcYes == s.Length)
                    break;

                i = IndexOfLastNormalizedChar(s, nextQcYes + 1, out isFirstCharToNormalizeSurrogatePair);

                var len = (i == -1 ? s.Length : i) - nextQcYes;
                list.EnsureCapacity(len);
                s.CopyTo(nextQcYes, list.InnerArray, list.Count, len);
                list.Count += len;

                if (i == -1) break;
            }

            result = list;
            return true;
        }

        private static bool IsHighSurrogate(int code)
        {
            return code >= 0xD800 && code <= 0xDBFF;
        }

        private static bool IsLowSurrogate(int code)
        {
            return code >= 0xDC00 && code <= 0xDFFF;
        }

        private static int ToCodePoint(int hi, int lo)
        {
            return ((hi - 0xD800) * 0x400) + (lo - 0xDC00) + 0x10000;
        }

        private static int IndexOfLastNormalizedChar(string s, int startIndex, out bool isFirstCharToNormalizeSurrogatePair)
        {
            var i = startIndex;
            while (i < s.Length)
            {
                int x = s[i];
                var isSurrogatePair = IsHighSurrogate(x);
                if (isSurrogatePair)
                    x = ToCodePoint(x, s[i + 1]);

                // CccAndQcTable にキーが存在する
                // → CCC が 0 ではない、または NFC_QC が YES ではない
                if (CccAndQcTable.ContainsKey(x))
                {
                    if (i > 0)
                    {
                        // 有効なサロゲートペアじゃなかったら死ね
                        isFirstCharToNormalizeSurrogatePair = IsLowSurrogate(s[i - 1]);
                        i -= isFirstCharToNormalizeSurrogatePair ? 2 : 1;
                    }
                    else
                    {
                        isFirstCharToNormalizeSurrogatePair = isSurrogatePair;
                    }
                    return i;
                }

                i += isSurrogatePair ? 2 : 1;
            }

            isFirstCharToNormalizeSurrogatePair = false;
            return -1;
        }

        private static MiniList<char> StringToMiniList(string s, int count)
        {
            var arr = new char[s.Length * 2];
            s.CopyTo(0, arr, 0, count);
            return new MiniList<char>
            {
                InnerArray = arr,
                Count = count
            };
        }

        private static int FindNextNfcQcYes(string s, int startIndex)
        {
            var i = startIndex;
            while (i < s.Length)
            {
                int x = s[i];
                var isSurrogatePair = IsHighSurrogate(x);
                if (isSurrogatePair)
                    x = ToCodePoint(x, s[i + 1]);
                if (!CccAndQcTable.ContainsKey(x))
                    break;
                i += isSurrogatePair ? 2 : 1;
            }
            return i;
        }

        private static void DecomposeInRange(string s, int startIndex, int endIndex, ref MiniList<char> dest)
        {
            var i = startIndex;
            while (i < endIndex)
            {
                int x = s[i];
                var isSurrogatePair = IsHighSurrogate(x);
                if (isSurrogatePair)
                    x = ToCodePoint(x, s[i + 1]);

                DecompCore(x, ref dest);

                i += isSurrogatePair ? 2 : 1;
            }
        }

        private static void AddCodePoint(ref MiniList<char> result, int code)
        {
            if (code <= char.MaxValue)
            {
                result.Add((char)code);
            }
            else
            {
                result.EnsureCapacity(2);
                code -= 0x10000;
                result.InnerArray[result.Count] = (char)(code / 0x400 + 0xD800);
                result.InnerArray[result.Count + 1] = (char)(code % 0x400 + 0xDC00);
                result.Count += 2;
            }
        }

        private static void DecompCore(int code, ref MiniList<char> result)
        {
            var SIndex = code - SBase;
            if (SIndex >= 0 && SIndex < SCount)
            {
                // ハングル
                var L = LBase + SIndex / NCount;
                var V = VBase + (SIndex % NCount) / TCount;
                var T = TBase + SIndex % TCount;
                AddCodePoint(ref result, L);
                AddCodePoint(ref result, V);
                if (T != TBase) AddCodePoint(ref result, T);
            }
            else
            {
                ulong v;
                if (DecompositionTable.TryGetValue(code, out v))
                {
                    DecompCore((int)(v >> 32), ref result);

                    var second = (int)v;
                    if (second != 0)
                        DecompCore(second, ref result);
                }
                else
                {
                    AddCodePoint(ref result, code);
                }
            }
        }

        private static void ReorderInRange(ref MiniList<char> list, int startIndex)
        {
            var rangeLen = list.Count - startIndex;

            var cccCache = new int[rangeLen];
            const int lowSurrogateCcc = 255;

            var i = 0;
            while (i < rangeLen)
            {
                var hi = list[startIndex + i];
                var lo = '\0';
                var isSurrogatePair = char.IsHighSurrogate(hi);
                var code = isSurrogatePair ? ToCodePoint(hi, lo = list[startIndex + i + 1]) : hi;

                var ccc = GetCanonicalCombiningClass(code);
                cccCache[i] = ccc;
                if (isSurrogatePair)
                    cccCache[i + 1] = lowSurrogateCcc;

                if (ccc != 0)
                {
                    var j = i - 1;
                    while (j >= 0)
                    {
                        var prevCcc = cccCache[j];
                        var isPrevSurrogatePair = prevCcc == lowSurrogateCcc;
                        if (isPrevSurrogatePair)
                            prevCcc = cccCache[j - 1];
                        if (prevCcc == 1 || prevCcc <= ccc) break;

                        var jIndex = startIndex + j;
                        if (isSurrogatePair)
                        {
                            if (isPrevSurrogatePair)
                            {
                                // 両方サロゲートペア
                                list[jIndex + 1] = list[jIndex - 1];
                                list[jIndex + 2] = list[jIndex];
                                list[jIndex - 1] = hi;
                                list[jIndex] = lo;
                                cccCache[j + 1] = prevCcc;
                                cccCache[j - 1] = ccc;
                            }
                            else
                            {
                                // iはサロゲートペア、jはサロゲートペアではない
                                list[jIndex + 2] = list[jIndex];
                                list[jIndex] = hi;
                                list[jIndex + 1] = lo;
                                cccCache[j + 2] = prevCcc;
                                cccCache[j] = ccc;
                                cccCache[j + 1] = lowSurrogateCcc;
                            }
                        }
                        else
                        {
                            if (isPrevSurrogatePair)
                            {
                                // iはサロゲートペアではない、jはサロゲートペア
                                list[jIndex + 1] = list[jIndex];
                                list[jIndex] = list[jIndex - 1];
                                list[jIndex - 1] = hi;
                                cccCache[j + 1] = lowSurrogateCcc;
                                cccCache[j] = prevCcc;
                                cccCache[j - 1] = ccc;
                            }
                            else
                            {
                                // どっちもサロゲートペアではない
                                list[jIndex + 1] = list[jIndex];
                                list[jIndex] = hi;
                                cccCache[j + 1] = prevCcc;
                                cccCache[j] = ccc;
                            }
                        }

                        j -= isPrevSurrogatePair ? 2 : 1;
                    }
                }

                i += isSurrogatePair ? 2 : 1;
            }
        }

        private static void ComposeInRange(ref MiniList<char> list, int startIndex)
        {
            int last = list[startIndex];
            var isLastSurrogatePair = IsHighSurrogate(last);
            if (isLastSurrogatePair)
                last = ToCodePoint(last, list[startIndex + 1]);
            var starterIndex = startIndex;
            var starter = ((ulong)last) << 32;
            var isStarterSurrogatePair = isLastSurrogatePair;
            var i = startIndex + (isLastSurrogatePair ? 2 : 1);
            var insertIndex = i;
            var lastCcc = 0;

            for (; i < list.Count; i++)
            {
                var hi = list[i];
                var lo = '\0';
                var isSurrogatePair = char.IsHighSurrogate(hi);
                var c = isSurrogatePair ? ToCodePoint(hi, lo = list[++i]) : hi;

                // ハングル
                if (!isLastSurrogatePair && !isSurrogatePair) // このifあってる？？
                {
                    var LIndex = last - LBase;
                    if (LIndex >= 0 && LIndex < LCount)
                    {
                        var VIndex = c - VBase;
                        if (VIndex >= 0 && VIndex < VCount)
                        {
                            last = SBase + (LIndex * VCount + VIndex) * TCount;
                            list[insertIndex - 1] = (char)last;
                            lastCcc = 0;
                            continue;
                        }
                    }

                    var SIndex = last - SBase;
                    if (SIndex >= 0 && SIndex < SCount && (SIndex % TCount) == 0)
                    {
                        var TIndex = c - TBase;
                        if (0 < TIndex && TIndex < TCount)
                        {
                            last += TIndex;
                            list[insertIndex - 1] = (char)last;
                            lastCcc = 0;
                            continue;
                        }
                    }
                }
                // ハングルここまで

                var ccc = GetCanonicalCombiningClass(c);
                if (ccc != 0 && lastCcc == ccc)
                {
                    // ブロック
                    list[insertIndex++] = hi;
                    if (isSurrogatePair)
                        list[insertIndex++] = lo;
                    last = c;
                    continue;
                }

                var key = starter | (ulong)c;
                int composed;
                if ((ccc != 0 || (ccc == 0 && lastCcc == 0)) && CompositionTable.TryGetValue(key, out composed))
                {
                    if (composed <= char.MaxValue)
                    {
                        if (isStarterSurrogatePair)
                        {
                            // 下位サロゲートのスペースを埋める
                            Debug.Assert(insertIndex < i);
                            for (var j = starterIndex + 1; j < --insertIndex; j++)
                                list[j] = list[j + 1];
                        }

                        list[starterIndex] = (char)composed;
                        isStarterSurrogatePair = false;
                    }
                    else
                    {
                        var tmp = composed - 0x10000;
                        var composedHi = (char)(tmp / 0x400 + 0xD800);
                        var composedLo = (char)(tmp % 0x400 + 0xDC00);

                        if (!isStarterSurrogatePair)
                        {
                            // 下位サロゲートを入れるスペースをつくる
                            Debug.Assert(insertIndex < i);
                            var starterLoIndex = starterIndex + 1;
                            for (var j = insertIndex; j > starterLoIndex; j--)
                                list[j] = list[j - 1];
                            insertIndex++;
                        }

                        list[starterIndex] = composedHi;
                        list[starterIndex + 1] = composedLo;
                        isStarterSurrogatePair = true;
                    }

                    starter = ((ulong)composed) << 32;
                    ccc = 0; // これでいい？？
                }
                else
                {
                    if (ccc == 0)
                    {
                        starterIndex = insertIndex;
                        starter = ((ulong)c) << 32;
                        isStarterSurrogatePair = isSurrogatePair;
                    }
                    list[insertIndex++] = hi;
                    if (isSurrogatePair)
                        list[insertIndex++] = lo;
                }

                last = c;
                isLastSurrogatePair = isSurrogatePair;
                lastCcc = ccc;
            }

            list.Count = insertIndex;
        }
    }
}

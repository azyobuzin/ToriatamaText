using System;
using System.Collections.Generic;
using ToriatamaText.InternalExtractors;

namespace ToriatamaText.UnicodeNormalization
{
    using static Tables;
    using static Utils;

    static class NewNfc
    {
        // メモ: Y について
        // for NFKC and NFC, the character may compose with a following character, but it never composes with a previous character.

        // つまり N/M の一個前の Y から始めて、次の Y の前まで NFD -> NFC すればいける

        // TODO: Compose にもクイックチェックを

        public static MiniList<int> Compose(string s)
        {
            var list = Decompose(s);

            // 最初の CCC 0 までスキップ
            var i = 0;
            var starterIndex = 0;
            ulong starter = 0;
            var last = 0;
            for (; i < list.Count; i++)
            {
                last = list[i];
                list[i] = last;
                if (GetCanonicalCombiningClass(last) == 0)
                {
                    starterIndex = i;
                    starter = ((ulong)last) << 32;
                    break;
                }
            }

            var insertIndex = ++i;
            var lastCcc = 0;
            for (; i < list.Count; i++)
            {
                var c = list[i];

                // ハングル
                var LIndex = last - LBase;
                if (LIndex >= 0 && LIndex < LCount)
                {
                    var VIndex = c - VBase;
                    if (VIndex >= 0 && VIndex < VCount)
                    {
                        last = SBase + (LIndex * VCount + VIndex) * TCount;
                        list[insertIndex - 1] = last;
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
                        list[insertIndex - 1] = last;
                        lastCcc = 0;
                        continue;
                    }
                }
                // ハングルここまで

                var ccc = GetCanonicalCombiningClass(c);
                if (ccc != 0 && lastCcc == ccc)
                {
                    // ブロック
                    list[insertIndex++] = c;
                    last = c;
                    continue;
                }

                var key = starter | (ulong)c;
                int composed;
                if ((ccc != 0 || (ccc == 0 && lastCcc == 0)) && CompositionTable.TryGetValue(key, out composed))
                {
                    list[starterIndex] = composed;
                    starter = ((ulong)composed) << 32;
                    ccc = 0;
                }
                else
                {
                    if (ccc == 0)
                    {
                        starterIndex = insertIndex;
                        starter = ((ulong)c) << 32;
                    }
                    list[insertIndex++] = c;
                }

                last = c;
                lastCcc = ccc;
            }

            if (insertIndex < list.Count)
                list.Count = insertIndex;
            return list;
        }

        private static MiniList<int> Decompose(string s)
        {
            var list = new MiniList<int>();
            list.SetCapacity(s.Length * 2);

            var lastNotDecomposedChar = -1;
            for (var i = 0; i < s.Length; i++)
            {
                int x = s[i];
                if (x >= 0xD800 && x <= 0xDBFF)
                    x = ((x - 0xD800) * 0x400) + (s[++i] - 0xDC00) + 0x10000;

                if (!CccAndQcTable.ContainsKey(x))
                {
                    list.Add(x);
                    lastNotDecomposedChar = x;
                }
                else
                {
                    if (lastNotDecomposedChar != -1)
                    {
                        list.Count--;
                        DecompCore(lastNotDecomposedChar, ref list);
                    }
                    DecompCore(x, ref list);
                    lastNotDecomposedChar = -1;
                }
            }

            var cccCache = new int[list.Count]; // 実際の CCC の値 + 1
            for (var i = 0; i < list.Count; i++)
            {
                var code = list[i];
                var ccc = GetCanonicalCombiningClass(list[i]) + 1;
                cccCache[i] = ccc;

                if (ccc != 1)
                {
                    var j = i - 1;
                    while (j >= 0)
                    {
                        var prevCcc = cccCache[j];
                        if (prevCcc == 1 || prevCcc <= ccc) break;

                        list[j + 1] = list[j];
                        list[j] = code;
                        cccCache[j + 1] = prevCcc;
                        cccCache[j] = ccc;
                        j--;
                    }
                }
            }

            return list;
        }

        private static void DecompCore(int code, ref MiniList<int> result)
        {
            var SIndex = code - SBase;
            if (SIndex >= 0 && SIndex < SCount)
            {
                // ハングル
                var L = LBase + SIndex / NCount;
                var V = VBase + (SIndex % NCount) / TCount;
                var T = TBase + SIndex % TCount;
                result.Add(L);
                result.Add(V);
                if (T != TBase) result.Add(T);
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
                    result.Add(code);
                }
            }
        }
    }
}

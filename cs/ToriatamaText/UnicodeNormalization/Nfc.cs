using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToriatamaText.InternalExtractors;

namespace ToriatamaText.UnicodeNormalization
{
    using static Utils;
    using static Tables;

    static class Nfc
    {
        public static MiniList<int> Compose(string s)
        {
            var list = Nfd.Decompose(s);

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
    }
}

using ToriatamaText.InternalExtractors;

namespace ToriatamaText.UnicodeNormalization
{
    using static Utils;
    using static Tables;

    static class Nfd
    {
        public static MiniList<int> Decompose(string s)
        {
            var list = new MiniList<int>();
            list.SetCapacity(s.Length * 2);

            for (var i = 0; i < s.Length; i++)
            {
                int x = s[i];
                if (x >= 0xD800 && x <= 0xDBFF)
                    x = ((x - 0xD800) * 0x400) + (s[++i] - 0xDC00) + 0x10000;

                DecompCore(x, ref list);
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

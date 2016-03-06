namespace ToriatamaText.UnicodeNormalization
{
    static class Utils
    {
        public const int SBase = 0xAC00,
          LBase = 0x1100, VBase = 0x1161, TBase = 0x11A7,
          LCount = 19, VCount = 21, TCount = 28,
          NCount = VCount * TCount,
          SCount = LCount * NCount;

        public static int GetCanonicalCombiningClass(int code)
        {
            ulong v;
            if (Tables.DecompositionTable.TryGetValue(code, out v))
                return (int)(v >> 48); // & 0xFF
            return 0;
        }
    }
}

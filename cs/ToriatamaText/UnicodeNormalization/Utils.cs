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
            short v;
            Tables.CccAndQcTable.TryGetValue(code, out v);
            return v & 0xFF;
        }
    }
}

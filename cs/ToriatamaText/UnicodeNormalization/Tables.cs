using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ToriatamaText.UnicodeNormalization
{
    static partial class Tables
    {
        private const uint CccAndQcTableMask = CccAndQcTableCapacity - 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LookupCccAndQcTable(uint key)
        {
            var i = CccAndQcTableBuckets[key & CccAndQcTableMask];
            while (i != -1)
            {
                var entry = CccAndQcTableEntries[i];
                if (((uint)entry) == key)
                    return true;
                i = (short)(entry >> 32);
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LookupCccAndQcTable(uint key, out int ccc)
        {
            var i = CccAndQcTableBuckets[key & CccAndQcTableMask];
            while (i != -1)
            {
                var entry = CccAndQcTableEntries[i];
                if (((uint)entry) == key)
                {
                    ccc = (int)(entry >> 48);
                    return true;
                }
                i = (short)(entry >> 32);
            }
            ccc = 0;
            return false;
        }

        public static int GetCanonicalCombiningClass(uint code)
        {
            int v;
            LookupCccAndQcTable(code, out v);
            return v;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;

GenerateDefaultTlds();
GenerateUnicodeNormalizationTables();

void GenerateDefaultTlds()
{
    Console.WriteLine(nameof(GenerateDefaultTlds));

    using (var client = new WebClient())
    using (var reader = new StreamReader(client.OpenRead("https://github.com/twitter/twitter-text/raw/master/conformance/tld_lib.yml")))
    using (var writer = new StreamWriter("DefaultTlds.g.cs"))
    {
        writer.WriteLine("using System.Collections.Generic;");
        writer.WriteLine();
        writer.WriteLine("namespace ToriatamaText");
        writer.WriteLine('{');
        writer.WriteLine("    public static class DefaultTlds");
        writer.WriteLine("    {");

        reader.ReadLine(); // ---

        var line = reader.ReadLine();
        if (line != "country:") throw new Exception();

        writer.WriteLine("        public static IReadOnlyList<string> CTlds { get; } = new[]");
        writer.WriteLine("        {");

        Action write = () =>
        {
            writer.WriteLine(
                "            \"{0}\",",
                line[2] == '"'
                    ? line.Substring(3, line.Length - 4)
                    : line.Substring(2)
            );
        };

        while ((line = reader.ReadLine()).StartsWith("- ", StringComparison.Ordinal))
            write();

        writer.WriteLine("        };");
        writer.WriteLine();

        if (line != "generic:") throw new Exception();

        writer.WriteLine("        public static IReadOnlyList<string> GTlds { get; } = new[]");
        writer.WriteLine("        {");

        while (!string.IsNullOrEmpty(line = reader.ReadLine()))
            write();

        writer.WriteLine("        };");
        writer.WriteLine("    }");
        writer.WriteLine('}');
    }
}

class UnicodeDataRow
{
    public int Code;
    public int CanonicalCombiningClass;
    public string[] DecompositionMapping;
}

int ParseHex(string x) => int.Parse(x, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

void ForEachCodePoint(string range, Action<int> action)
{
    var s = range.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
    if (s.Length == 1)
        action(ParseHex(s[0]));
    else if (s.Length == 2)
    {
        var end = ParseHex(s[1]);
        for (var i = ParseHex(s[0]); i <= end; i++)
            action(i);
    }
    else
    {
        throw new Exception("Invalid DerivedNormalizationProps.txt");
    }
}

uint ToUtf16(int code)
{
    var s = char.ConvertFromUtf32(code);
    return s.Length == 1 ? s[0] : ((uint)s[0]) << 16 | s[1];
}

void GenerateUnicodeNormalizationTables()
{
    Console.WriteLine(nameof(GenerateUnicodeNormalizationTables));

    var data = new List<UnicodeDataRow>();
    var compositionExclusions = new List<int>();
    var nfcQcNorM = new HashSet<int>();

    using (var client = new WebClient())
    {
        using (var reader = new StreamReader(client.OpenRead("http://www.unicode.org/Public/UCD/latest/ucd/UnicodeData.txt")))
        {
            string line;
            while (!string.IsNullOrEmpty(line = reader.ReadLine()))
            {
                var s = line.Split(';');
                var mapping = s[5].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                data.Add(new UnicodeDataRow
                {
                    Code = ParseHex(s[0]),
                    CanonicalCombiningClass = int.Parse(s[3], CultureInfo.InvariantCulture),
                    DecompositionMapping = mapping.Length == 0 ? null : mapping
                });
            }
        }

        using (var reader = new StreamReader(client.OpenRead("http://www.unicode.org/Public/UCD/latest/ucd/DerivedNormalizationProps.txt")))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Length != 0 && line[0] != '#')
                {
                    var s = line.Split(';');
                    if (s[1].TrimStart(null).StartsWith("Full_Composition_Exclusion", StringComparison.Ordinal))
                    {
                        ForEachCodePoint(s[0], compositionExclusions.Add);
                    }
                    else if (s[1].Trim() == "NFC_QC")
                    {
                        ForEachCodePoint(s[0], x => nfcQcNorM.Add(x));
                    }
                }
            }
        }
    }

    compositionExclusions.Sort();

    var dataDic = new Dictionary<int, UnicodeDataRow>(data.Count);
    foreach (var x in data) dataDic.Add(x.Code, x);

    // Value: [マッピング1文字目(4bytes)][マッピング2文字目(4bytes)]
    var decompTableItems = new List<KeyValuePair<uint, ulong>>();

    // Key: [1文字目(4bytes)][2文字目(4bytes)]
    var compTableItems = new List<KeyValuePair<ulong, uint>>();

    var cccAndQcTableItems = new List<KeyValuePair<uint, int>>();

    foreach (var x in data)
    {
        var mapping = x.DecompositionMapping;
        if (mapping != null && mapping[0][0] == '<')
            mapping = null;

        if (mapping != null)
        {
            var value = (ulong)ToUtf16(ParseHex(mapping[0])) << 32;
            if (mapping.Length == 2)
            {
                value |= ToUtf16(ParseHex(mapping[1]));

                if (compositionExclusions.BinarySearch(x.Code) < 0)
                {
                    compTableItems.Add(new KeyValuePair<ulong, uint>(value, ToUtf16(x.Code)));
                }
            }
            else if (mapping.Length > 2)
            {
                throw new Exception($"\\u{Convert.ToString(x.Code, 16)} has too many elements in the decomposition mapping.");
            }

            decompTableItems.Add(new KeyValuePair<uint, ulong>(ToUtf16(x.Code), value));
        }

        if (x.CanonicalCombiningClass != 0 || nfcQcNorM.Contains(x.Code))
            cccAndQcTableItems.Add(new KeyValuePair<uint, int>(ToUtf16(x.Code), x.CanonicalCombiningClass));
    }

    var cccAndQcTableCapacity = (uint)Math.Pow(2, 10);
    var cccAndQcTableBuckets = new int[cccAndQcTableCapacity];
    for (var i = 0; i < cccAndQcTableCapacity; i++) cccAndQcTableBuckets[i] = -1;
    // [value(1byte)][next(2bytes)][key(4bytes)]
    var cccAndQcTableEntries = new ulong[cccAndQcTableItems.Count];
    uint cccAndQcTableEntryIndex = 0;

    Console.WriteLine("CccAndQcTable");
    foreach (var group in cccAndQcTableItems.GroupBy(x => x.Key & (cccAndQcTableCapacity - 1)))
    {
        Console.Write(group.Count() + " ");
        ulong next = ushort.MaxValue;
        foreach (var x in group)
        {
            cccAndQcTableEntries[cccAndQcTableEntryIndex] = ((ulong)x.Value) << 48 | next << 32 | ((ulong)x.Key);
            next = cccAndQcTableEntryIndex++;
        }
        cccAndQcTableBuckets[group.Key] = (int)next;
    }

    Console.WriteLine();
    Console.WriteLine(cccAndQcTableBuckets.Count(x => x == -1));

    using (var writer = new StreamWriter(Path.Combine("UnicodeNormalization", "Tables.g.cs")))
    {
        writer.WriteLine("using System.Collections.Generic;");
        writer.WriteLine();
        writer.WriteLine("namespace ToriatamaText.UnicodeNormalization");
        writer.WriteLine('{');
        writer.WriteLine("    partial class Tables");
        writer.WriteLine("    {");
        writer.WriteLine("        public static Dictionary<uint, ulong> DecompositionTable {{ get; }} = new Dictionary<uint, ulong>({0})", decompTableItems.Count);
        writer.WriteLine("        {");

        foreach (var x in decompTableItems)
            writer.WriteLine("            [0x{0:X4}] = 0x{1:X13},", x.Key, x.Value);

        writer.WriteLine("        };");
        writer.WriteLine();
        writer.WriteLine("        public static Dictionary<ulong, uint> CompositionTable {{ get; }} = new Dictionary<ulong, uint>({0})", compTableItems.Count);
        writer.WriteLine("        {");

        foreach (var x in compTableItems)
            writer.WriteLine("            [0x{0:X12}] = 0x{1:X4},", x.Key, x.Value);

        writer.WriteLine("        };");
        writer.WriteLine();
        writer.WriteLine("        public const int CccAndQcTableCapacity = {0};", cccAndQcTableCapacity);
        writer.WriteLine();
        writer.Write("        public static short[] CccAndQcTableBuckets { get; } = { ");

        foreach (var x in cccAndQcTableBuckets)
            writer.Write(x.ToString(CultureInfo.InvariantCulture) + ", ");

        writer.WriteLine("};");
        writer.WriteLine();
        writer.Write("        public static ulong[] CccAndQcTableEntries { get; } = { ");

        foreach (var x in cccAndQcTableEntries)
            writer.Write("0x{0:X14}, ", x);

        writer.WriteLine("};");
        writer.WriteLine();
        writer.WriteLine("    }");
        writer.WriteLine("}");
    }
}

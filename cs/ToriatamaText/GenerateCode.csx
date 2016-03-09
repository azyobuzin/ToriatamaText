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

void GenerateUnicodeNormalizationTables()
{
    Console.WriteLine(nameof(GenerateUnicodeNormalizationTables));

    var data = new List<UnicodeDataRow>();
    var compositionExclusions = new List<int>();
    var nfcQcNorM = new List<int>();

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
                        ForEachCodePoint(s[0], nfcQcNorM.Add);
                    }
                }
            }
        }
    }

    compositionExclusions.Sort();
    nfcQcNorM.Sort();

    var dataDic = new Dictionary<int, UnicodeDataRow>(data.Count);
    foreach (var x in data) dataDic.Add(x.Code, x);

    // Value: [マッピング1文字目(4bytes)][マッピング2文字目(4bytes)]
    var decompTableItems = new List<KeyValuePair<int, ulong>>();

    // Key: [1文字目(4bytes)][2文字目(4bytes)]
    var compTableItems = new List<KeyValuePair<ulong, int>>();

    var cccAndQcTableItems = new List<KeyValuePair<int, int>>();

    foreach (var x in data)
    {
        var mapping = x.DecompositionMapping;
        if (mapping != null && mapping[0][0] == '<')
            mapping = null;

        if (mapping != null)
        {
            var value = ((ulong)ParseHex(mapping[0])) << 32;
            if (mapping.Length == 2)
            {
                value |= (ulong)ParseHex(mapping[1]);

                if (compositionExclusions.BinarySearch(x.Code) < 0)
                {
                    compTableItems.Add(new KeyValuePair<ulong, int>(value, x.Code));
                }
            }
            else if (mapping.Length > 2)
            {
                throw new Exception($"\\u{Convert.ToString((int)x.Code, 16)} has too many elements in the decomposition mapping.");
            }

            decompTableItems.Add(new KeyValuePair<int, ulong>(x.Code, value));
        }

        var nfcQc = nfcQcNorM.BinarySearch(x.Code) >= 0 ? (1 << 8) : 0;
        if (x.CanonicalCombiningClass != 0 || nfcQc != 0)
            cccAndQcTableItems.Add(new KeyValuePair<int, int>(x.Code, x.CanonicalCombiningClass | nfcQc));
    }

    using (var writer = new StreamWriter(Path.Combine("UnicodeNormalization", "Tables.g.cs")))
    {
        writer.WriteLine("using System.Collections.Generic;");
        writer.WriteLine();
        writer.WriteLine("namespace ToriatamaText.UnicodeNormalization");
        writer.WriteLine('{');
        writer.WriteLine("    static class Tables");
        writer.WriteLine("    {");
        writer.WriteLine("        public static Dictionary<int, ulong> DecompositionTable {{ get; }} = new Dictionary<int, ulong>({0})", decompTableItems.Count);
        writer.WriteLine("        {");

        foreach (var x in decompTableItems)
            writer.WriteLine("            [0x{0:X4}] = 0x{1:X13},", x.Key, x.Value);

        writer.WriteLine("        };");
        writer.WriteLine();
        writer.WriteLine("        public static Dictionary<ulong, int> CompositionTable {{ get; }} = new Dictionary<ulong, int>({0})", compTableItems.Count);
        writer.WriteLine("        {");

        foreach (var x in compTableItems)
            writer.WriteLine("            [0x{0:X13}] = 0x{1:X4},", x.Key, x.Value);

        writer.WriteLine("        };");
        writer.WriteLine();
        writer.WriteLine("        public static Dictionary<int, short> CccAndQcTable {{ get; }} = new Dictionary<int, short>({0})", cccAndQcTableItems.Count);
        writer.WriteLine("        {");

        foreach (var x in cccAndQcTableItems)
            writer.WriteLine("            [0x{0:X4}] = 0x{1:X3},", x.Key, x.Value);

        writer.WriteLine("        };");
        writer.WriteLine();
        writer.WriteLine("    }");
        writer.WriteLine("}");
    }
}

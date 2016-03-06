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

void GenerateUnicodeNormalizationTables()
{
    Console.WriteLine(nameof(GenerateUnicodeNormalizationTables));

    var data = new List<UnicodeDataRow>();
    var compositionExclusions = new List<int>();

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
                        var range = s[0].Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                        if (range.Length == 1)
                            compositionExclusions.Add(ParseHex(range[0]));
                        else if (range.Length == 2)
                        {
                            var end = ParseHex(range[1]);
                            for (var i = ParseHex(range[0]); i <= end; i++)
                                compositionExclusions.Add(i);
                        }
                        else
                        {
                            throw new Exception("Invalid DerivedNormalizationProps.txt");
                        }
                    }
                }
            }
        }
    }

    compositionExclusions.Sort();

    var dataDic = new Dictionary<int, UnicodeDataRow>(data.Count);
    foreach (var x in data) dataDic.Add(x.Code, x);

    // Value: 0(1byte)[CCC(1byte)][マッピング2文字目(3bytes)][マッピング1文字目(3bytes)]
    var decompTableItems = new List<KeyValuePair<int, ulong>>();

    foreach (var x in data)
    {
        var mapping = x.DecompositionMapping;
        if (mapping != null && mapping[0][0] == '<')
            mapping = null;

        if (x.CanonicalCombiningClass != 0 || mapping != null)
        {
            var value = ((ulong)x.CanonicalCombiningClass) << 48;

            if (mapping != null && mapping.Length >= 1)
            {
                value |= (ulong)ParseHex(mapping[0]);

                if (mapping.Length == 2)
                    value |= ((ulong)ParseHex(mapping[1])) << 24;
                else if (mapping.Length > 2)
                    throw new Exception($"\\u{Convert.ToString((int)x.Code, 16)} has too many elements in the decomposition mapping.");
            }

            decompTableItems.Add(new KeyValuePair<int, ulong>(x.Code, value));
        }
    }

    // Key: [1文字目(4bytes)][2文字目(4bytes)]
    var compTableItems = new List<KeyValuePair<ulong, int>>();

    foreach (var x in data)
    {
        var mapping = x.DecompositionMapping;
        if (mapping == null || mapping.Length != 2 || mapping[0][0] == '<' || compositionExclusions.BinarySearch(x.Code) >= 0)
            continue;

        var key = (((ulong)ParseHex(mapping[0])) << 32) | ((ulong)ParseHex(mapping[1]));
        compTableItems.Add(new KeyValuePair<ulong, int>(key, x.Code));
    }

    using (var writer = new StreamWriter(Path.Combine("UnicodeNormalization", "Tables.g.cs")))
    {
        writer.WriteLine("using System.Collections.Generic;");
        writer.WriteLine();
        writer.WriteLine("namespace ToriatamaText.UnicodeNormalization");
        writer.WriteLine('{');
        writer.WriteLine("    static class Tables");
        writer.WriteLine("    {");
        writer.WriteLine("        public static Dictionary<int, ulong> DecompositionTable { get; }");
        writer.WriteLine("        public static Dictionary<ulong, int> CompositionTable { get; }");
        writer.WriteLine();
        writer.WriteLine("        static Tables()");
        writer.WriteLine("        {");
        writer.WriteLine("            DecompositionTable = new Dictionary<int, ulong>({0})", decompTableItems.Count);
        writer.WriteLine("            {");

        foreach (var x in decompTableItems)
            writer.WriteLine("                [0x{0:X4}] = 0x{1:X14},", x.Key, x.Value);

        writer.WriteLine("            };");
        writer.WriteLine();
        writer.WriteLine("            CompositionTable = new Dictionary<ulong, int>({0})", compTableItems.Count);
        writer.WriteLine("            {");

        foreach (var x in compTableItems)
            writer.WriteLine("                [0x{0:X13}] = 0x{1:X4},", x.Key, x.Value);

        writer.WriteLine("            };");
        writer.WriteLine("        }");
        writer.WriteLine("    }");
        writer.WriteLine("}");
    }
}

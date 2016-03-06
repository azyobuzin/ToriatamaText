using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using ToriatamaText.InternalExtractors;
using ToriatamaText.UnicodeNormalization;

namespace ToriatamaText.Test
{
    static class UnicodeNormalizationTest
    {
        public static void Run()
        {
            if (!File.Exists(testFile))
                DownloadTests();

            using (var sr = new StreamReader(testFile))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Length == 0 || line[0] == '#') continue;
                    if (line[0] == '@')
                    {
                        Console.WriteLine(line);
                        continue;
                    }

                    var s = line.Split(';');
                    var c1 = CodePointListToString(s[0]);
                    var c2 = CodePointListToString(s[1]);
                    var c3 = CodePointListToString(s[2]);
                    var c4 = CodePointListToString(s[3]);
                    var c5 = CodePointListToString(s[4]);

                    // NFD
                    // c3 == toNFD(c1) == toNFD(c2) == toNFD(c3)
                    // c5 == toNFD(c4) == toNFD(c5)
                    if (!(ToString(Nfd.Decompose(c1)) == c3 && ToString(Nfd.Decompose(c2)) == c3 && ToString(Nfd.Decompose(c3)) == c3
                        && ToString(Nfd.Decompose(c4)) == c5 && ToString(Nfd.Decompose(c5)) == c5))
                    {
                        Debugger.Break();
                    }

                    // NFC
                    // c2 == toNFC(c1) == toNFC(c2) == toNFC(c3)
                    // c4 == toNFC(c4) == toNFC(c5)
                    if (!(ToString(Nfc.Compose(c1)) == c2 && ToString(Nfc.Compose(c2)) == c2 && ToString(Nfc.Compose(c3)) == c2
                        && ToString(Nfc.Compose(c4)) == c4 && ToString(Nfc.Compose(c5)) == c4))
                    {
                        Debugger.Break();
                    }
                }
            }
        }

        private const string testFile = "NormalizationTest.txt";

        static void DownloadTests()
        {
            Console.WriteLine("Downloading NormalizationTest.txt");
            new WebClient().DownloadFile(
                "http://www.unicode.org/Public/UCD/latest/ucd/NormalizationTest.txt",
                testFile);
        }

        static string ToString(MiniList<int> miniList)
        {
            var sb = new StringBuilder(miniList.Count);
            for (var i = 0; i < miniList.Count; i++)
            {
                var x = miniList[i];
                if (x <= char.MaxValue)
                    sb.Append((char)x);
                else
                {
                    x -= 0x10000;
                    sb.Append((char)((x / 0x400) + 0xD800)).Append((char)((x % 0x400) + 0xDC00));
                }
            }
            return sb.ToString();
        }

        static string CodePointListToString(string s)
        {
            return string.Concat(
                s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => char.ConvertFromUtf32(int.Parse(x, NumberStyles.HexNumber, CultureInfo.InvariantCulture))));
        }
    }
}

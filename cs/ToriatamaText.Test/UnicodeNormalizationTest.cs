using System;
using System.Collections.Generic;
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

            var tests = LoadTests();

            foreach (var x in tests)
            {
                // NFD
                // c3 == toNFD(c1) == toNFD(c2) == toNFD(c3)
                // c5 == toNFD(c4) == toNFD(c5)
                //if (!(ToString(Nfd.Decompose(x[0])) == x[2] && ToString(Nfd.Decompose(x[1])) == x[2] && ToString(Nfd.Decompose(x[2])) == x[2]
                //    && ToString(Nfd.Decompose(x[3])) == x[4] && ToString(Nfd.Decompose(x[4])) == x[4]))
                //{
                //    Debugger.Break();
                //}

                // NFC
                // c2 == toNFC(c1) == toNFC(c2) == toNFC(c3)
                // c4 == toNFC(c4) == toNFC(c5)
                if (!(ToString(NewNfc.Compose(x[0])) == x[1] && ToString(NewNfc.Compose(x[1])) == x[1] && ToString(NewNfc.Compose(x[2])) == x[1]
                    && ToString(NewNfc.Compose(x[3])) == x[3] && ToString(NewNfc.Compose(x[4])) == x[3]))
                {
                    Debugger.Break();
                }
            }

            var stopwatch = new Stopwatch();
            const int ntimes = 100;

            stopwatch.Start();
            for (var i = 0; i < ntimes; i++)
            {
                foreach (var x in tests)
                {
                    foreach (var y in x)
                        NewNfc.Compose(y);
                }
            }
            stopwatch.Stop();
            Console.WriteLine("ToriatamaText.UnicodeNormalization: {0}", stopwatch.Elapsed);

            stopwatch.Restart();
            for (var i = 0; i < ntimes; i++)
            {
                foreach (var x in tests)
                {
                    foreach (var y in x)
                        y.Normalize();
                }
            }
            stopwatch.Stop();
            Console.WriteLine("String.Normalize: {0}", stopwatch.Elapsed);
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

        static string[][] LoadTests()
        {
            if (!File.Exists(testFile))
                DownloadTests();

            var tests = new List<string[]>();
            using (var sr = new StreamReader(testFile))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Length == 0 || line[0] == '#' || line[0] == '@') continue;
                    tests.Add(
                        line.Split(';').Take(5)
                        .Select(s => string.Concat(s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => char.ConvertFromUtf32(int.Parse(x, NumberStyles.HexNumber, CultureInfo.InvariantCulture)))
                        ))
                        .ToArray()
                    );
                }
            }
            return tests.ToArray();
        }
    }
}

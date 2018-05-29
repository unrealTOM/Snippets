/*
 * TODO: compare with VectorizedCopy https://github.com/IllyriadGames/ByteArrayExtensions
*/
using System;
using System.Diagnostics;

namespace Snippets
{
    class DataCopyTest
    {
        public static int NN = 2048;
        public static int IT = 100000;
        public static void Main()
        {
            for (int N = 1; N < NN; ++N)
            {
                int[] pa = new int[N];
                int[] pb = new int[N];

                Stopwatch sw1 = new Stopwatch();
                sw1.Start();
                for (int i = 0; i < IT; ++i)
                    Array.Copy(pa, 0, pb, 0, N);
                sw1.Stop();

                Stopwatch sw2 = new Stopwatch();
                sw2.Start();
                for (int i = 0; i < IT; ++i)
                    System.Buffer.BlockCopy(pa, 0, pb, 0, N);
                sw2.Stop();

                Console.WriteLine("{0} {1} {2}", N, sw1.Elapsed, sw2.Elapsed);
            }
        }
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDBGenerator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Media.Animation;
using static UnitTest.GCMonitor;

namespace UnitTest
{
    [TestClass]
    public class PDBCodecTest
    {
        private bool CompareSequences(byte[] seq1, byte[] seq2)
        {
            if (seq1.Length != seq2.Length)
                return false;
            for (int i = 0; i < seq1.Length; i++)
            {
                if (seq1[i] != seq2[i])
                    return false;
            }
            return true;
        }
        private bool CompareSequences(Span<byte> seq1, Span<byte> seq2)
        {
            if (seq1.Length != seq2.Length)
                return false;
            Span<byte> span1 = seq1;
            Span<byte> span2 = seq2;
            for (int i = 0; i < seq1.Length; i++)
            {
                if (span1[i] != span2[i])
                    return false;
            }
            return true;
        }
        [TestMethod]
        public void TestCodec_Correctness()
        {
            Span<byte> sequence1 = [0, 1, 2, 3, 4, 5];
            Span<byte> sequence2 = [5, 4, 3, 2, 1, 0];
            Span<byte> sequence3 = [0, 1, 3, 2, 4, 5];
            Assert.AreEqual(sequence1.Length, sequence2.Length);
            Assert.AreEqual(sequence2.Length, sequence3.Length);
            Codec codec = new Codec(15, sequence1.Length);
            long index1 = codec.Encode(sequence1);
            long index2 = codec.Encode(sequence2);
            long index3 = codec.Encode(sequence3);

            Span<byte> resultSequence1 = new byte[sequence1.Length];
            Span<byte> resultSequence2 = new byte[sequence1.Length];
            Span<byte> resultSequence3 = new byte[sequence1.Length];
            codec.DecodeMem(index1, resultSequence1);
            codec.DecodeMem(index2, resultSequence2);
            codec.DecodeMem(index3, resultSequence3);
            Assert.IsTrue(CompareSequences(sequence1, resultSequence1), "Decoded sequence1 does not match original");
            Assert.IsTrue(CompareSequences(sequence2, resultSequence2), "Decoded sequence2 does not match original");
            Assert.IsTrue(CompareSequences(sequence3, resultSequence3), "Decoded sequence3 does not match original");
        }

        [TestMethod]
        public void TestCodecPerformance()
        {
            int loopCount = 1000000;
            Span<byte> sequence1 = [6, 5, 4, 3, 2, 1, 0 ];
            Codec codec = new Codec(15, sequence1.Length);
            long index = 0;

            long gen0Before = GC.GetTotalMemory(false);
            int gen0CountBefore = GC.CollectionCount(0);
            int gen1CountBefore = GC.CollectionCount(1);
            int gen2CountBefore = GC.CollectionCount(2);

            GCStatistics stats = new();
            Stopwatch sw = new();
            using (GCMonitor mon = new(stats))
            {
                sw.Start();
                for (int i = 0; i < loopCount; i++)
                {
                    index = codec.Encode(sequence1);
                }
                sw.Stop();
            }
            Debug.WriteLine($"\n--- GC Statistics ---");
            Debug.WriteLine($"Gen0 Collections: {stats.gen0Collections}");
            Debug.WriteLine($"Gen1 Collections: {stats.gen1Collections}");
            Debug.WriteLine($"Gen2 Collections: {stats.gen2Collections}");
            Debug.WriteLine($"Memory Growth: {stats.memoryGrowth:N0} bytes");


            Console.WriteLine($"Encoding completed in {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"({Math.Floor((double)loopCount / sw.ElapsedMilliseconds) } encodes / ms)");
            sw.Restart();
            Span<byte> decodeSpan = new byte[sequence1.Length];
            for (int i = 0; i < loopCount; i++)
            {
                codec.DecodeMem(index, decodeSpan);
            }
            sw.Stop();
            Console.WriteLine($"Decoding completed in {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"({Math.Floor((double)loopCount / sw.ElapsedMilliseconds)} decodes / ms)");
        }
    }
}

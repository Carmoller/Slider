using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDBGenerator;

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
        [TestMethod]
        public void TestCodec()
        {
            Codec codec = new Codec(15, 6);
            // Example test cases
            byte[] sequence1 = { 0, 1, 2, 3, 4, 5 };
            byte[] sequence2 = { 5, 4, 3, 2, 1, 0 };
            byte[] sequence3 = { 0, 1, 3, 2, 4, 5 };
            long index1 = codec.Encode(sequence1, 14);
            long index2 = codec.Encode(sequence2, 14);
            long index3 = codec.Encode(sequence3, 14);

            DecodeResult result1 = codec.Decode(index1);
            DecodeResult result2 = codec.Decode(index2);
            DecodeResult result3 = codec.Decode(index3);
            Assert.IsTrue(CompareSequences(sequence1, result1.TilePositions), "Decoded sequence1 does not match original");
            Assert.IsTrue(CompareSequences(sequence2, result2.TilePositions), "Decoded sequence2 does not match original");
            Assert.IsTrue(CompareSequences(sequence3, result3.TilePositions), "Decoded sequence3 does not match original");
        }

        [TestMethod]
        public void TestCodecPerformance()
        {
            Codec codec = new Codec(15, 6);
            int loopCount = 1000000;
            byte[] sequence1 = { 5, 4, 3, 2, 1, 0 };
            long index = 0;
            Stopwatch sw = new();
            sw.Start();
            for (int i = 0; i< loopCount; i++)
            {
                index = codec.Encode(sequence1, 14);
                DecodeResult result = codec.Decode(index);
            }
            sw.Stop();
            Console.WriteLine($"Encoding completed in {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"({Math.Floor((double)loopCount / sw.ElapsedMilliseconds) } encodes / ms)");
            sw.Restart();
            for (int i = 0; i < loopCount; i++)
            {
                DecodeResult result = codec.Decode(index);
            }
            sw.Stop();
            Console.WriteLine($"Decoding completed in {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"({Math.Floor((double)loopCount / sw.ElapsedMilliseconds)} decodes / ms)");
        }
    }
}

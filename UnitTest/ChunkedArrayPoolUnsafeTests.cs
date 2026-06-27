using Microsoft.VisualStudio.TestTools.UnitTesting;
using Slider.Common;

namespace UnitTest
{
    [TestClass]
    public class ChunkedArrayPoolUnsafeTests
    {
        [TestMethod]
        public void Get_ReturnsValidIndex()
        {
            ChunkedArrayPoolUnsafe pool = new(chunkSize: 10, arraySize: 256);

            int index = pool.Get();

            Assert.AreNotEqual(ChunkedArrayPoolUnsafe.NoIndex, index);
            pool.Dispose();
        }

        [TestMethod]
        public void GetArray_ReturnsCorrectSizedArray()
        {
            ChunkedArrayPoolUnsafe pool = new(chunkSize: 10, arraySize: 256);

            PointerToken token = pool.GetToken();
            Span<byte> bytes = token.AsSpan();
            Assert.AreEqual(256, bytes.Length);
            pool.Dispose();
        }

        [TestMethod]
        public void MultipleArrays_AreIndependent()
        {
            ChunkedArrayPoolUnsafe pool = new(chunkSize: 10, arraySize: 256);

            PointerToken token1 = pool.GetToken();
            PointerToken token2 = pool.GetToken();

            token1.AsSpan()[0] = 42;
            token1.AsSpan()[100] = 99;

            token2.AsSpan()[0] = 13;
            token2.AsSpan()[100] = 27;

            Assert.AreEqual(42, token1.AsSpan()[0]);
            Assert.AreEqual(99, token1.AsSpan()[100]);
            Assert.AreEqual(13, token2.AsSpan()[0]);
            Assert.AreEqual(27, token2.AsSpan()[100]);
            pool.Dispose();
        }

        [TestMethod]
        public void MultipleIndices_WithinChunk_AreIndependent()
        {
            ChunkedArrayPoolUnsafe pool = new(chunkSize: 5, arraySize: 100);

            List<PointerToken> tokenList = new();
            for (int i = 0; i < 5; i++)
            {
                PointerToken token = pool.GetToken();
                token.AsSpan()[0] = (byte)(10 * i);
                token.AsSpan()[50] = (byte)(100 + i);
                tokenList.Add(token);
            }

            for (int i = 0; i < 5; i++)
            {
                PointerToken token = tokenList[i];
                Assert.AreEqual((byte)(10 * i), token.AsSpan()[0], $"Array {i} slot 0 corrupted");
                Assert.AreEqual((byte)(100 + i), token.AsSpan()[50], $"Array {i} slot 50 corrupted");
            }
            pool.Dispose();
        }

        [TestMethod]
        public void MultipleChunks_AreIndependent()
        {
            ChunkedArrayPoolUnsafe pool = new(chunkSize: 3, arraySize: 50);

            List<PointerToken> tokenList = new();
            for (int i = 0; i < 7; i++)
            {
                PointerToken token = pool.GetToken();
                token.AsSpan()[0] = (byte)(20 * i);
                tokenList.Add(token);
            }

            for (int i = 0; i < 7; i++)
            {
                PointerToken token = tokenList[i];
                Assert.AreEqual((byte)(20 * i), token.AsSpan()[0], $"Array {i} was overwritten");
            }
            pool.Dispose();
        }

        [TestMethod]
        public void Release_MakesIndexAvailableForReuse()
        {
            ChunkedArrayPoolUnsafe pool = new(chunkSize: 5, arraySize: 100);

            PointerToken token1 = pool.GetToken();
            int index1 = token1.Index;
            pool.Release(token1);
            PointerToken token2 = pool.GetToken();

            Assert.AreEqual(index1, token2.Index, index1, "Released index should be reused");
            pool.Dispose();
        }

        [TestMethod]
        public void PoolExpands_WhenAllSlotsExhausted()
        {
            ChunkedArrayPoolUnsafe pool = new(chunkSize: 3, arraySize: 50);

            for (int i = 0; i < 6; i++)
            {
                PointerToken token = pool.GetToken();
                Assert.AreEqual(50, token.AsSpan().Length, $"Array {i} has wrong size");
            }
            pool.Dispose();
        }

        [TestMethod]
        public void SequentialModification_DoesNotCrossPollinate()
        {
            ChunkedArrayPoolUnsafe pool = new(chunkSize: 4, arraySize: 10);

            PointerToken token1 = pool.GetToken();
            PointerToken token2 = pool.GetToken();
            PointerToken token3 = pool.GetToken();

            for (int i = 0; i < 10; i++)
            {
                token1.AsSpan()[i] = (byte)(20 + i);
                token2.AsSpan()[i] = (byte)(40 + i);
                token3.AsSpan()[i] = (byte)(60 + i);
            }

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(20 + i, token1.AsSpan()[i], $"Array1[{i}] corrupted");
                Assert.AreEqual(40 + i, token2.AsSpan()[i], $"Array2[{i}] corrupted");
                Assert.AreEqual(60 + i, token3.AsSpan()[i], $"Array3[{i}] corrupted");
            }
            pool.Dispose();
        }
    }
}

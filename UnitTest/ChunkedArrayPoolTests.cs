using Microsoft.VisualStudio.TestTools.UnitTesting;
using Slider.Common;

namespace UnitTest
{
    [TestClass]
    public class ChunkedArrayPoolTests
    {
        [TestMethod]
        public void Get_ReturnsValidIndex()
        {
            ChunkedArrayPool<byte> pool = new(chunkSize: 10, arraySize: 256);

            int index = pool.Get();

            Assert.AreNotEqual(ChunkedArrayPool<byte>.NoIndex, index);
        }

        [TestMethod]
        public void GetArray_ReturnsCorrectSizedArray()
        {
            ChunkedArrayPool<byte> pool = new(chunkSize: 10, arraySize: 256);

            int index = pool.Get();
            byte[] array = pool.GetArray(index);

            Assert.HasCount(256, array);
        }

        [TestMethod]
        public void MultipleArrays_AreIndependent()
        {
            ChunkedArrayPool<byte> pool = new(chunkSize: 10, arraySize: 256);

            int index1 = pool.Get();
            int index2 = pool.Get();

            byte[] array1 = pool.GetArray(index1);
            byte[] array2 = pool.GetArray(index2);

            array1[0] = 42;
            array1[100] = 99;

            array2[0] = 13;
            array2[100] = 27;

            Assert.AreEqual(42, array1[0]);
            Assert.AreEqual(99, array1[100]);
            Assert.AreEqual(13, array2[0]);
            Assert.AreEqual(27, array2[100]);
        }

        [TestMethod]
        public void MultipleIndices_WithinChunk_AreIndependent()
        {
            ChunkedArrayPool<byte> pool = new(chunkSize: 5, arraySize: 100);

            int[] indices = new int[5];
            for (int i = 0; i < 5; i++)
            {
                indices[i] = pool.Get();
            }

            for (int i = 0; i < 5; i++)
            {
                byte[] array = pool.GetArray(indices[i]);
                array[0] = (byte)(10 * i);
                array[50] = (byte)(100 + i);
            }

            for (int i = 0; i < 5; i++)
            {
                byte[] array = pool.GetArray(indices[i]);
                Assert.AreEqual((byte)(10 * i), array[0], $"Array {i} slot 0 corrupted");
                Assert.AreEqual((byte)(100 + i), array[50], $"Array {i} slot 50 corrupted");
            }
        }

        [TestMethod]
        public void MultipleChunks_AreIndependent()
        {
            ChunkedArrayPool<byte> pool = new(chunkSize: 3, arraySize: 50);

            int[] indices = new int[7];
            for (int i = 0; i < 7; i++)
            {
                indices[i] = pool.Get();
            }

            for (int i = 0; i < 7; i++)
            {
                byte[] array = pool.GetArray(indices[i]);
                array[0] = (byte)(20 * i);
            }

            for (int i = 0; i < 7; i++)
            {
                byte[] array = pool.GetArray(indices[i]);
                Assert.AreEqual((byte)(20 * i), array[0], $"Array {i} was overwritten");
            }
        }

        [TestMethod]
        public void Release_MakesIndexAvailableForReuse()
        {
            ChunkedArrayPool<byte> pool = new(chunkSize: 5, arraySize: 100);

            int index1 = pool.Get();
            pool.Release(index1);
            int index2 = pool.Get();

            Assert.AreEqual(index1, index2, "Released index should be reused");
        }

        [TestMethod]
        public void PoolExpands_WhenAllSlotsExhausted()
        {
            ChunkedArrayPool<byte> pool = new(chunkSize: 3, arraySize: 50);

            int[] indices = new int[6];
            for (int i = 0; i < 6; i++)
            {
                indices[i] = pool.Get();
            }

            for (int i = 0; i < 6; i++)
            {
                byte[] array = pool.GetArray(indices[i]);
                Assert.HasCount(50, array, $"Array {i} has wrong size");
            }
        }

        [TestMethod]
        public void GetArray_MultipleTypes_Works()
        {
            ChunkedArrayPool<int> intPool = new(chunkSize: 5, arraySize: 100);
            ChunkedArrayPool<string> stringPool = new(chunkSize: 5, arraySize: 50);

            int intIndex = intPool.Get();
            int stringIndex = stringPool.Get();

            int[] intArray = intPool.GetArray(intIndex);
            string[] stringArray = stringPool.GetArray(stringIndex);

            Assert.HasCount(100, intArray);
            Assert.HasCount(50, stringArray);

            intArray[0] = 42;
            stringArray[0] = "test";

            Assert.AreEqual(42, intArray[0]);
            Assert.AreEqual("test", stringArray[0]);
        }

        [TestMethod]
        public void SequentialModification_DoesNotCrossPollinate()
        {
            ChunkedArrayPool<int> pool = new(chunkSize: 4, arraySize: 10);

            int index1 = pool.Get();
            int index2 = pool.Get();
            int index3 = pool.Get();

            int[] array1 = pool.GetArray(index1);
            int[] array2 = pool.GetArray(index2);
            int[] array3 = pool.GetArray(index3);

            for (int i = 0; i < 10; i++)
            {
                array1[i] = 100 + i;
                array2[i] = 200 + i;
                array3[i] = 300 + i;
            }

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(100 + i, array1[i], $"Array1[{i}] corrupted");
                Assert.AreEqual(200 + i, array2[i], $"Array2[{i}] corrupted");
                Assert.AreEqual(300 + i, array3[i], $"Array3[{i}] corrupted");
            }
        }
    }
}

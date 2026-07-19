using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace UnitTest
{
    struct TestStruct
    {
        public int x;
        public int y;
    }
    [TestClass]
    public class MultiMapTests
    {
        [TestMethod]
        public void MultiMap_MustRetrieveAfterAdding()
        {
            TestStruct ts1 = new TestStruct { x = 2, y = 3 };
            MultiMap<TestStruct> testObject = new(100, 100);
            testObject.AddState(23, ref ts1);

            ReadOnlySpan<TestStruct> readSpan = testObject.Get(23);
            Assert.AreEqual(1, readSpan.Length);
            TestStruct readTest = readSpan[0];
            Assert.AreEqual(2, readTest.x);
            Assert.AreEqual(3, readTest.y);
        }

        [TestMethod]
        public void MultiMap_HashCollision_MustRetrieveArray()
        {
            TestStruct ts1 = new TestStruct { x = 2, y = 3 };
            TestStruct ts2 = new TestStruct { x = 4, y = 5 };
            MultiMap<TestStruct> testObject = new(100, 100);
            testObject.AddState(23, ref ts1);
            testObject.AddState(23, ref ts2);

            ReadOnlySpan<TestStruct> readSpan = testObject.Get(23);

            Assert.AreEqual(2, readSpan.Length);

            TestStruct readTest = readSpan[0];
            Assert.AreEqual(2, readTest.x);
            Assert.AreEqual(3, readTest.y);

            TestStruct readTest2 = readSpan[1];
            Assert.AreEqual(4, readTest2.x);
            Assert.AreEqual(5, readTest2.y);
        }

        [TestMethod]
        public void MultiMap_MustRetrieveAfterAdding_AllColliding()
        {
            int numberToTest = 1000000;
            // Add 1000 structs to the MultiMap, and iterate through all of them, then do the same to a dictionary, and compare the time spent
            MultiMap<TestStruct> testObject = new(numberToTest, numberToTest);

            TestStruct ts1 = new TestStruct { x = 2, y = 3 };
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < numberToTest; i++)
            {
                testObject.AddState(23, ref ts1);
            }

            for (int i=0; i< numberToTest; i++)
            {
                ReadOnlySpan<TestStruct> readSpan = testObject.Get(23);
                Assert.AreEqual(numberToTest, readSpan.Length);
                TestStruct testRead = readSpan[i];
                Assert.AreEqual(2, testRead.x);
                Assert.AreEqual(3, testRead.y);
            }
            sw.Stop();
            long elapsedTicksMap = sw.ElapsedTicks;
            Console.Write($"Map ticks: {elapsedTicksMap}");
        }
        [TestMethod]
        public void MultiMap_MustRetrieveAfterAdding_NoColliding()
        {
            int numberToTest = 1000000;
            // Add 1000 structs to the MultiMap, and iterate through all of them, then do the same to a dictionary, and compare the time spent
            MultiMap<TestStruct> testObject = new(numberToTest, numberToTest);

            TestStruct ts1 = new TestStruct { x = 2, y = 3 };
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < numberToTest; i++)
            {
                testObject.AddState(i+1, ref ts1);
            }

            for (int i = 0; i < numberToTest; i++)
            {
                ReadOnlySpan<TestStruct> readSpan = testObject.Get(i+1);
                Assert.AreEqual(1, readSpan.Length);
                TestStruct testRead = readSpan[0];
                Assert.AreEqual(2, testRead.x);
                Assert.AreEqual(3, testRead.y);
            }
            sw.Stop();
            long elapsedTicksMap = sw.ElapsedTicks;

            Console.Write($"Map ticks: {elapsedTicksMap}");
        }

    }
}

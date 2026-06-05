using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDBGenerator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace UnitTest
{
    [TestClass]
    public class BufferPoolTests
    {
        [TestMethod]
        public void RentAndReturn_BasicOperation_Success()
        {
            // Arrange
            BufferPool pool = new(capacity: 10, size: 64);

            // Act
            BufferPool.Slot slot = pool.Rent();
            pool.Return(slot);
            BufferPool.Slot slot2 = pool.Rent();

            // Assert
            Assert.AreEqual(slot.Index, slot2.Index, "Returned slot should be reused");
            Assert.AreEqual(64, slot.Size);
        }

        [TestMethod]
        public void GetMemory_ReturnsValidMemory()
        {
            // Arrange
            BufferPool pool = new(capacity: 5, size: 128);
            BufferPool.Slot slot = pool.Rent();

            // Act
            Memory<byte> memory = pool.GetMemory(slot);

            // Assert
            Assert.AreEqual(128, memory.Length);
            Assert.AreEqual(0, memory.Span[0], "Memory should be initialized");
        }

        [TestMethod]
        public void WriteAndReadMemory_DataPersists()
        {
            // Arrange
            BufferPool pool = new(capacity: 5, size: 128);
            BufferPool.Slot slot = pool.Rent();
            Memory<byte> memory = pool.GetMemory(slot);

            // Act
            memory.Span[0] = 42;
            memory.Span[100] = 99;

            // Assert
            Assert.AreEqual(42, memory.Span[0]);
            Assert.AreEqual(99, memory.Span[100]);
        }

        [TestMethod]
        public void Rent_PoolEmpty_ThrowsException()
        {
            // Arrange
            BufferPool pool = new(capacity: 2, size: 64);

            // Act & Assert
            pool.Rent();
            pool.Rent();

            try
            {
                pool.Rent();
                Assert.Fail("Expected InvalidOperationException");
            }
            catch (InvalidOperationException)
            {
                // Expected
            }
        }

        [TestMethod]
        public void MultipleRentReturn_CorrectSlotReuse()
        {
            // Arrange
            BufferPool pool = new(capacity: 3, size: 32);
            BufferPool.Slot[] slots = new BufferPool.Slot[3];

            // Act - Rent all
            for (int i = 0; i < 3; i++)
            {
                slots[i] = pool.Rent();
            }

            // Write different values to identify slots
            for (int i = 0; i < 3; i++)
            {
                Memory<byte> mem = pool.GetMemory(slots[i]);
                mem.Span[0] = (byte)i;
            }

            // Return in different order
            pool.Return(slots[1]);
            pool.Return(slots[0]);
            pool.Return(slots[2]);

            // Rent again - should reuse in LIFO order
            BufferPool.Slot reused1 = pool.Rent();
            Memory<byte> mem1 = pool.GetMemory(reused1);
            Assert.AreEqual(2, mem1.Span[0], "Should reuse most recently returned slot");

            BufferPool.Slot reused2 = pool.Rent();
            Memory<byte> mem2 = pool.GetMemory(reused2);
            Assert.AreEqual(0, mem2.Span[0]);
        }

        [TestMethod]
        public void MillionAllocations_MonitorGC()
        {
            // Arrange
            int capacity = 10000;
            int size = 128;
            int allocations = 1000000;

            BufferPool pool = new(capacity, size);

            // Force collection to get clean baseline
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long gen0Before = GC.GetTotalMemory(false);
            int gen0CountBefore = GC.CollectionCount(0);
            int gen1CountBefore = GC.CollectionCount(1);
            int gen2CountBefore = GC.CollectionCount(2);

            Stopwatch sw = Stopwatch.StartNew();

            // Act - Allocate and return slots many times
            for (int i = 0; i < allocations; i++)
            {
                BufferPool.Slot slot = pool.Rent();
                Memory<byte> memory = pool.GetMemory(slot);

                // Write to ensure memory is touched
                memory.Span[0] = (byte)(i % 256);

                pool.Return(slot);

                // Periodically cycle through all slots
                if ((i + 1) % capacity == 0)
                {
                    // Natural cycle - all slots have been returned
                }
            }

            sw.Stop();

            long gen0After = GC.GetTotalMemory(false);
            int gen0CountAfter = GC.CollectionCount(0);
            int gen1CountAfter = GC.CollectionCount(1);
            int gen2CountAfter = GC.CollectionCount(2);

            // Assert & Report
            int gen0Collections = gen0CountAfter - gen0CountBefore;
            int gen1Collections = gen1CountAfter - gen1CountBefore;
            int gen2Collections = gen2CountAfter - gen2CountBefore;
            long memoryGrowth = gen0After - gen0Before;

            Debug.WriteLine($"\n=== BufferPool Performance Report ===");
            Debug.WriteLine($"Allocations: {allocations:N0}");
            Debug.WriteLine($"Pool Capacity: {capacity}");
            Debug.WriteLine($"Slot Size: {size} bytes");
            Debug.WriteLine($"Total Pool Size: {capacity * size:N0} bytes");
            Debug.WriteLine($"\nExecution Time: {sw.ElapsedMilliseconds}ms");
            Debug.WriteLine($"Allocations per second: {allocations / sw.Elapsed.TotalSeconds:N0}");
            Debug.WriteLine($"\n--- GC Statistics ---");
            Debug.WriteLine($"Gen0 Collections: {gen0Collections}");
            Debug.WriteLine($"Gen1 Collections: {gen1Collections}");
            Debug.WriteLine($"Gen2 Collections: {gen2Collections}");
            Debug.WriteLine($"Memory Growth: {memoryGrowth:N0} bytes");

            // Verify pool is still functional
            BufferPool.Slot testSlot = pool.Rent();
            Memory<byte> testMem = pool.GetMemory(testSlot);
            Assert.AreEqual(size, testMem.Length, "Pool should still be functional");

            // GC should be minimal with buffering strategy
            Assert.IsLessThan(100, gen0Collections, "Gen0 collections should be minimal with buffer pool");
        }

        [TestMethod]
        public void LargeAllocations_LargeBuffers()
        {
            // Arrange
            int capacity = 100;
            int size = 255; // Max size that fits in byte (Slot.Size is byte)
            BufferPool pool = new(capacity, size);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long memBefore = GC.GetTotalMemory(false);
            int gen0Before = GC.CollectionCount(0);

            Stopwatch sw = Stopwatch.StartNew();

            // Act - Perform 10,000 allocations with large buffers
            for (int i = 0; i < 10000; i++)
            {
                BufferPool.Slot slot = pool.Rent();
                Memory<byte> memory = pool.GetMemory(slot);

                // Write to multiple locations to ensure memory is functional
                memory.Span[0] = (byte)(i % 256);
                memory.Span[size / 2] = (byte)((i + 1) % 256);
                memory.Span[size - 1] = (byte)((i + 2) % 256);

                pool.Return(slot);
            }

            sw.Stop();

            long memAfter = GC.GetTotalMemory(false);
            int gen0After = GC.CollectionCount(0);

            // Assert & Report
            Debug.WriteLine($"\n=== Large Buffer Pool Test ===");
            Debug.WriteLine($"Buffer Size: {size} bytes");
            Debug.WriteLine($"Pool Capacity: {capacity}");
            Debug.WriteLine($"Total Pool Size: {capacity * size:N0} bytes");
            Debug.WriteLine($"Allocations: 10,000");
            Debug.WriteLine($"Execution Time: {sw.ElapsedMilliseconds}ms");
            Debug.WriteLine($"Memory Growth: {(memAfter - memBefore) / 1024:N0}KB");
            Debug.WriteLine($"Gen0 Collections: {gen0After - gen0Before}");

            Assert.IsLessThan(50, gen0After - gen0Before, "Large buffers should minimize GC pressure");
        }

        [TestMethod]
        public void ConcurrentRentReturn_ThreadSafe()
        {
            // Arrange
            BufferPool pool = new(capacity: 1000, size: 64);
            int threadCount = 8;
            int allocationsPerThread = 50000;
            List<Exception> exceptions = new();

            // Act
            Thread[] threads = new Thread[threadCount];
            for (int t = 0; t < threadCount; t++)
            {
                threads[t] = new Thread(() =>
                {
                    try
                    {
                        for (int i = 0; i < allocationsPerThread; i++)
                        {
                            BufferPool.Slot slot = pool.Rent();
                            Memory<byte> memory = pool.GetMemory(slot);
                            memory.Span[0] = (byte)Thread.CurrentThread.ManagedThreadId;
                            pool.Return(slot);
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                });

                threads[t].Start();
            }

            // Wait for all threads
            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            // Assert
            Assert.IsEmpty(exceptions, "No exceptions should occur in concurrent access");
            Debug.WriteLine($"\n=== Concurrent Access Test ===");
            Debug.WriteLine($"Threads: {threadCount}");
            Debug.WriteLine($"Allocations per thread: {allocationsPerThread:N0}");
            Debug.WriteLine($"Total allocations: {threadCount * allocationsPerThread:N0}");
            Debug.WriteLine($"Success: All threads completed without exception");
        }
    }
}

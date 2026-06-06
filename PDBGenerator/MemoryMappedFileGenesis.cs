using System;
using System.IO;
using System.IO.MemoryMappedFiles;

// NOTE; Build action set to None for now!!!
namespace PDBGenerator
{
    internal class MemoryMappedFileGenesis
    {
        public void Test()
        {

            // Define file size (e.g., 64 GB)
            long capacity = 64L * 1024L * 1024L * 1024L;
            string filePath = @"D:\temp\virtual_ram.dat";

            // Create the file backing store
            // Use FileOptions.RandomAccess to tell the OS NOT to prefetch sequential data
            using var fileStream = new FileStream(
                filePath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None,
                bufferSize: 4096,
                options: FileOptions.RandomAccess // <-- Instructs OS to optimize for random seek paths
            );

            using var mmf = MemoryMappedFile.CreateFromFile(
                fileStream,
                mapName: null,
                capacity,
                MemoryMappedFileAccess.ReadWrite,
                HandleInheritability.None,
                leaveOpen: false
            );

            using var accessor = mmf.CreateViewAccessor(0, capacity, MemoryMappedFileAccess.ReadWrite);
            // Extract the raw OS pointer safely
            try
            {
                unsafe
                {
                    byte* pointer = null;
                    accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);

                    // Apply the offset (pointer points to byte 0 of the map)
                    byte* baseAddress = pointer + accessor.PointerOffset;

                    // --- HIGH FREQUENCY OPERATIONS LOOP ---
                    // Example: Billions of reads and writes via native pointers
                    long targetIndex = 50_000_000_000L; // 50 GB offset

                    // Direct write (Takes nanoseconds, no managed overhead)
                    baseAddress[targetIndex] = 0xFF;

                    // Direct read
                    byte value = baseAddress[targetIndex];
                }
            }
            finally
            {
                //  CRITICAL: Always release the handle to prevent leaks
                accessor.SafeMemoryMappedViewHandle.ReleasePointer();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace PDBGenerator
{
    /// <summary>
    /// Pattern database backed by a memory-mapped file for unlimited scalability.
    /// Allows generating very large PDBs (10×10 and beyond) without RAM exhaustion.
    /// </summary>
    public class MemoryMappedPatternDatabase : IDisposable
    {
        public int GridSize { get; private set; }
        public int K { get; private set; }
        public long TotalStates { get; private set; }
        public string FilePath { get; private set; }

        private readonly int _chunkShift;
        private readonly int _chunkSize;
        private readonly int _chunkMask;
        private MemoryMappedFile? _mmf;
        private Dictionary<long, MemoryMappedViewAccessor>? _chunkAccessors;
        private HashSet<long> _initializedChunks;

        private const string MetadataHeader = "PDBV1";
        private const int HeaderSize = 5 + 4 + 4 + 8 + 4; // Magic(5) + gridSize(4) + k(4) + totalStates(8) + chunkShift(4) = 25 bytes

        public MemoryMappedPatternDatabase(int gridSize, int k, long totalStates, int chunkShift, string filePath, bool initialize = true)
        {
            GridSize = gridSize;
            K = k;
            TotalStates = totalStates;
            _chunkShift = chunkShift;
            _chunkSize = 1 << chunkShift;
            _chunkMask = _chunkSize - 1;
            FilePath = filePath;
            _chunkAccessors = new Dictionary<long, MemoryMappedViewAccessor>();
            _initializedChunks = new HashSet<long>();

            if (initialize)
                InitializeFile();
        }

        private void InitializeFile()
        {
            // Calculate total file size needed (header + all possible chunks)
            long numChunks = (TotalStates + _chunkSize - 1) >> _chunkShift;
            long fileSize = HeaderSize + (numChunks * (long)_chunkSize);

            // Delete old file if it exists
            if (File.Exists(FilePath))
            {
                try { File.Delete(FilePath); } catch { }
                System.Threading.Thread.Sleep(100); // Give Windows time to release locks
            }

            // Create file with required size and write header in one go
            using (var fs = new FileStream(FilePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                // Set file size
                fs.SetLength(fileSize);

                // Write header
                byte[] header = new byte[HeaderSize];
                System.Buffer.BlockCopy(System.Text.Encoding.ASCII.GetBytes(MetadataHeader), 0, header, 0, 5);
                System.Buffer.BlockCopy(BitConverter.GetBytes(GridSize), 0, header, 5, 4);
                System.Buffer.BlockCopy(BitConverter.GetBytes(K), 0, header, 9, 4);
                System.Buffer.BlockCopy(BitConverter.GetBytes(TotalStates), 0, header, 13, 8);
                System.Buffer.BlockCopy(BitConverter.GetBytes(_chunkShift), 0, header, 21, 4);

                fs.Write(header, 0, HeaderSize);
                fs.Flush();
            }

            // Give Windows time to fully release the file
            System.Threading.Thread.Sleep(100);

            // Now open the memory-mapped file
            _mmf = MemoryMappedFile.CreateFromFile(FilePath, FileMode.Open,
                Path.GetFileName(FilePath), fileSize, MemoryMappedFileAccess.ReadWrite);
        }

        /// <summary>
        /// Set distance for a specific state index
        /// </summary>
        public void SetDistance(long index, byte distance)
        {
            long chunkIdx = index / _chunkSize;
            int offset = (int)(index % _chunkSize);

            // Mark chunk as initialized on first write
            if (!_initializedChunks!.Contains(chunkIdx))
            {
                _initializedChunks.Add(chunkIdx);
                // Initialize chunk with MaxValue
                var accessor = GetChunkAccessor(chunkIdx);
                byte[] initChunk = new byte[_chunkSize];
                Array.Fill(initChunk, byte.MaxValue);
                accessor.WriteArray(0, initChunk, 0, _chunkSize);
                accessor.Flush();
            }

            var writeAccessor = GetChunkAccessor(chunkIdx);
            writeAccessor.Write(offset, distance);
        }

        /// <summary>
        /// Get distance for a specific state index
        /// </summary>
        public byte GetDistance(long index)
        {
            long chunkIdx = index / _chunkSize;
            int offset = (int)(index % _chunkSize);

            // If chunk hasn't been initialized yet, it's unvisited
            if (!_initializedChunks!.Contains(chunkIdx))
                return byte.MaxValue;

            var accessor = GetChunkAccessor(chunkIdx);
            return accessor.ReadByte(offset);
        }

        private MemoryMappedViewAccessor GetChunkAccessor(long chunkIdx)
        {
            if (_mmf == null)
                throw new ObjectDisposedException("MemoryMappedPatternDatabase");

            if (_chunkAccessors!.TryGetValue(chunkIdx, out var accessor))
                return accessor;

            // Calculate offset for this chunk in the file
            long offset = HeaderSize + (chunkIdx * (long)_chunkSize);

            // Create accessor for this chunk
            var newAccessor = _mmf.CreateViewAccessor(offset, _chunkSize, MemoryMappedFileAccess.ReadWrite);
            _chunkAccessors[chunkIdx] = newAccessor;
            return newAccessor;
        }

        /// <summary>
        /// Close the memory-mapped file and save
        /// </summary>
        public void Close()
        {
            if (_chunkAccessors != null)
            {
                foreach (var accessor in _chunkAccessors.Values)
                {
                    accessor?.Flush();
                    accessor?.Dispose();
                }
                _chunkAccessors.Clear();
            }
            _mmf?.Dispose();
            _mmf = null;
        }

        public void Dispose()
        {
            Close();
        }

        /// <summary>
        /// Load a previously saved memory-mapped PDB
        /// </summary>
        public static MemoryMappedPatternDatabase LoadFromFile(string filePath)
        {
            // Give the file time to be released if it was just written
            System.Threading.Thread.Sleep(100);

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new BinaryReader(fs))
            {
                // Read header
                byte[] magicBytes = reader.ReadBytes(5);
                string magic = System.Text.Encoding.ASCII.GetString(magicBytes);
                if (magic != MetadataHeader)
                    throw new InvalidOperationException("Invalid PDB file format");

                int gridSize = reader.ReadInt32();
                int k = reader.ReadInt32();
                long totalStates = reader.ReadInt64();
                int chunkShift = reader.ReadInt32();

                MemoryMappedPatternDatabase pdb = new MemoryMappedPatternDatabase(gridSize, k, totalStates, chunkShift, filePath, false);
                pdb.OpenExistingFile();
                return pdb;
            }
        }

        /// <summary>
        /// Opens an existing memory-mapped file and marks all chunks as initialized
        /// </summary>
        private void OpenExistingFile()
        {
            // Open the existing memory-mapped file for reading
            long fileSize = new FileInfo(FilePath).Length;
            _mmf = MemoryMappedFile.CreateFromFile(FilePath, FileMode.Open,
                Path.GetFileName(FilePath), fileSize, MemoryMappedFileAccess.ReadWrite);

            // Calculate total number of chunks in the file and mark them all as initialized
            long numChunks = (TotalStates + _chunkSize - 1) >> _chunkShift;
            for (long chunkIdx = 0; chunkIdx < numChunks; chunkIdx++)
            {
                _initializedChunks!.Add(chunkIdx);
            }
        }
    }
}

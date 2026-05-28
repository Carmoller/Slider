using System;
using System.Collections.Generic;
using System.Text;

namespace PDBGenerator
{
    using System;
    using System.IO;

    public class PatternDatabase
    {
        public int GridSize { get; private set; }
        public int K { get; private set; }
        public long TotalStates { get; private set; }

        private readonly int _chunkShift;
        private readonly int _chunkSize;
        private readonly int _chunkMask;
        private Dictionary<long, byte[]>? _pdbChunks;
        private MemoryMappedPatternDatabase? _mmPdb;
        private bool _isMemoryMapped;

        // Constructor for in-memory dictionary
        public PatternDatabase(int gridSize, int k, long totalStates, Dictionary<long, byte[]> chunks, int chunkShift)
        {
            GridSize = gridSize;
            K = k;
            TotalStates = totalStates;
            _pdbChunks = chunks;
            _chunkShift = chunkShift;
            _chunkSize = 1 << chunkShift;
            _chunkMask = _chunkSize - 1;
            _isMemoryMapped = false;
        }

        // Constructor for memory-mapped file
        public PatternDatabase(int gridSize, int k, long totalStates, string mmfFilePath)
        {
            GridSize = gridSize;
            K = k;
            TotalStates = totalStates;
            _mmPdb = MemoryMappedPatternDatabase.LoadFromFile(mmfFilePath);
            _chunkShift = 20;
            _chunkSize = 1 << _chunkShift;
            _chunkMask = _chunkSize - 1;
            _isMemoryMapped = true;
        }

        /// <summary>
        /// The critical O(1) heuristic lookup function for your A* solver loop.
        /// </summary>
        public byte GetDistance(long combinadicIndex)
        {
            if (_isMemoryMapped)
            {
                return _mmPdb!.GetDistance(combinadicIndex);
            }

            long chunkIdx = combinadicIndex / _chunkSize;
            int offset = (int)(combinadicIndex % _chunkSize);

            // If a chunk was never allocated by the BFS, the state is unreachable.
            if (!_pdbChunks!.TryGetValue(chunkIdx, out byte[]? chunk))
                return byte.MaxValue;

            return chunk[offset];
        }

        /// <summary>
        /// Streams the pattern database directly to a compact binary file on your disk.
        /// </summary>
        public void SaveToFile(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            using (var writer = new BinaryWriter(fs))
            {
                // Write standard schema metadata header
                writer.Write(GridSize);
                writer.Write(K);
                writer.Write(TotalStates);
                writer.Write(_chunkShift);

                // Write number of allocated chunks
                writer.Write(_pdbChunks.Count);

                // Stream each chunk with its index
                foreach (var kvp in _pdbChunks)
                {
                    writer.Write(kvp.Key);
                    writer.Write(kvp.Value);
                }
            }
        }

        /// <summary>
        /// Loads a pre-generated PDB binary payload back into RAM instantly.
        /// </summary>
        public static PatternDatabase LoadFromFile(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new BinaryReader(fs))
            {
                int gridSize = reader.ReadInt32();
                int k = reader.ReadInt32();
                long totalStates = reader.ReadInt64();
                int chunkShift = reader.ReadInt32();
                int chunkSize = 1 << chunkShift;

                int numChunksStored = reader.ReadInt32();
                Dictionary<long, byte[]> chunks = new Dictionary<long, byte[]>(numChunksStored);

                for (int i = 0; i < numChunksStored; i++)
                {
                    long chunkIdx = reader.ReadInt64();
                    byte[] chunkData = reader.ReadBytes(chunkSize);
                    chunks[chunkIdx] = chunkData;
                }

                return new PatternDatabase(gridSize, k, totalStates, chunks, chunkShift);
            }
        }
    }
}

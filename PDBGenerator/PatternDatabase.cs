using System;
using System.Collections.Generic;
using System.Text;

namespace PDBGenerator
{
    using System;
    using System.IO;
    using System.Reflection.Metadata;

    public class PatternDatabase
    {
        public int GridSize { get; private set; }
        public int K { get; private set; }
        public long TotalStates { get; private set; }
        public byte[] TrackedTiles { get; private set; }
        private bool _includeBlank;

        private readonly int _chunkShift;
        private readonly int _chunkSize;
        private readonly int _chunkMask;
        public Dictionary<long, byte[]>? _pdbChunks;
        private MemoryMappedPatternDatabase? _mmPdb;
        private bool _isMemoryMapped;
        // Constructor for in-memory dictionary
        public PatternDatabase(int gridSize, int k, bool includeBlank, long totalStates, byte[] goalPositions, Dictionary<long, byte[]> chunks, int chunkShift)
        {
            GridSize = gridSize;
            K = k;
            _includeBlank = includeBlank;
            TotalStates = totalStates;
            _pdbChunks = chunks;
            _chunkShift = chunkShift;
            _chunkSize = 1 << chunkShift;
            _chunkMask = _chunkSize - 1;
            _isMemoryMapped = false;
            TrackedTiles = new byte[goalPositions.GetLength(0)];
            for (int i = 0; i < goalPositions.Length; i++)
            {
                TrackedTiles[i] = (byte)(goalPositions[i] + 1); // Positions are zero-based, tile numbers are 1-based
            }
        }

        // Constructor for memory-mapped file
        public PatternDatabase(int gridSize, int k, bool includeBlank, long totalStates, byte[] goalPositions, string mmfFilePath)
        {
            GridSize = gridSize;
            K = k;
            _includeBlank = includeBlank;
            TotalStates = totalStates;
            _mmPdb = MemoryMappedPatternDatabase.LoadFromFile(mmfFilePath);
            _chunkShift = 20;
            _chunkSize = 1 << _chunkShift;
            _chunkMask = _chunkSize - 1;
            _isMemoryMapped = true;
            TrackedTiles = new byte[goalPositions.GetLength(0)];
            for (int i = 0; i < goalPositions.Length; i++)
            {
                TrackedTiles[i] = (byte)(goalPositions[i] + 1); // Positions are zero-based, tile numbers are 1-based
            }
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
                writer.Write("PDB");
                int headerLength = 25 + TrackedTiles.Length;
                writer.Write(headerLength);
                writer.Write(GridSize);
                writer.Write(K);
                writer.Write(_includeBlank);
                writer.Write(TotalStates);
                writer.Write(TrackedTiles.Length);
                for (int i = 0; i < TrackedTiles.Length; i++)
                {
                    writer.Write(TrackedTiles[i]);
                }
                writer.Write(_chunkShift);

                // Write number of allocated chunks
                writer.Write(_pdbChunks.Count);

                // Stream each chunk with its index
                foreach (KeyValuePair<long, byte[]> kvp in _pdbChunks)
                {
                    writer.Write(kvp.Key);
                    writer.Write(kvp.Value);
                }
            }
        }

        /// <summary>
        /// Loads a pre-generated PDB binary payload back into RAM
        /// </summary>
        public static PatternDatabase? LoadFromFile(string filePath)
        {
            using (FileStream fs = new (filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (BinaryReader reader = new (fs))
            {
                string header = reader.ReadString();// reader.ReadBoundedString(3);
                if (header != "PDB")
                    return null;
                int headerLength = reader.ReadInt32();
                int gridSize = reader.ReadInt32();
                int k = reader.ReadInt32();
                bool includeBlank = reader.ReadBoolean();
                long totalStates = reader.ReadInt64();
                int trackedTilesCount = reader.ReadInt32();
                byte[] trackedTiles = new byte[trackedTilesCount];
                for (int i=0; i<trackedTilesCount; i++)
                {
                    trackedTiles[i] = (byte)(reader.ReadByte() - 1); // Bit of a hack: We know the constructor expects the POSITION, not the tile number
                }
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

                return new PatternDatabase(gridSize, k, includeBlank, totalStates, trackedTiles, chunks, chunkShift);
            }
        }

        public static int GetSizeFromPdb(string filename)
        {
            using (FileStream fs = new(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (BinaryReader reader = new(fs))
            {
                string header = reader.ReadString();// reader.ReadBoundedString(3);
                if (header != "PDB")
                    return -1;
                reader.ReadInt32();
                int headerLength = reader.ReadInt32();
                return headerLength;
            }

        }
    }
}

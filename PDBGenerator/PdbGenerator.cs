using Slider.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace PDBGenerator
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using static BufferPool;

    public class PdbGenerator
    {
        public long ElapsedMs { get; private set; }
        public long StatesProcessed { get; private set; }
        public byte MaxCost { get; private set; }
        // Configuration for the target system
        private int GridSize;
        private int TotalPositions;
        private int K = 6;
        public long MaxQueueLength { get; private set; }

        // Chunk configuration to avoid huge contiguous allocations
        private const int ChunkShift = 20; // 1MB per chunk
        private const int ChunkSize = 1 << ChunkShift;
        private const int ChunkMask = ChunkSize - 1;

        private Dictionary<long, byte[]>? _pdbChunks;
        private MemoryMappedPatternDatabase? _mmPdb;
        private long _totalStates;
        private Codec _pdbCodec;
        private bool _useMemoryMappedFile;
        private byte[] _stateBits; // One bit corresponds to one state (ie position of all tracked tiles + the position of the blank)
        /// <summary>
        /// Creates a Generator for pattern database creation.
        /// 
        /// Two storage modes:
        /// 1. In-Memory (useMemoryMappedFile=false, default):
        ///    - Stores all states in RAM (chunks up to 1MB each)
        ///    - Faster access but limited by available RAM
        ///    - Suitable for 5x5, 6x6 puzzles
        ///    - Memory: ~128 MB for 5x5 with K=6
        ///    
        /// 2. Memory-Mapped File (useMemoryMappedFile=true):
        ///    - Stores states in a file on disk, OS pages into RAM as needed
        ///    - Unlimited size (limited by disk space)
        ///    - Slightly slower access but avoids RAM exhaustion
        ///    - Suitable for 10x10+ puzzles
        ///    - File path: %TEMP%/pdb_{gridSize}x{gridSize}_k{k}.mmf
        /// </summary>
        public PdbGenerator(byte gridSize, byte k, bool useMemoryMappedFile = false)
        {
            GridSize = gridSize;
            _pdbCodec = new(gridSize, k);
            TotalPositions = GridSize * GridSize;
            K = k;
            _totalStates = CalculateTotalStates();
            // Max to come out of the Lehmer encode, when we include the blank
            _stateBits = new byte[_totalStates / 8 + 1]; // Guaranteed to be zeros all the way
            _useMemoryMappedFile = useMemoryMappedFile;

            if (_useMemoryMappedFile)
            {
                // Memory-mapped approach: unlimited size, limited by disk space
                // Usage: new Generator(10, 6, useMemoryMappedFile: true)
                string dbPath = Path.Combine(Path.GetTempPath(), $"pdb_{gridSize}x{gridSize}_k{k}.mmf");
                _mmPdb = new MemoryMappedPatternDatabase(GridSize, K, _totalStates, ChunkShift, dbPath);
            }
            else
            {
                // In-memory approach: limited by available RAM, faster access
                // Default for smaller puzzles (5x5, 6x6)
                InitializeChunks();
            }
        }

        private void SetStateVisited(long stateIndex)
        {
            long bytePosition = stateIndex / 8;
            byte bitPosition = (byte)(stateIndex % 8);
            byte mask = (byte)(1 << bitPosition);
            _stateBits[bytePosition] |= mask;
        }

        private bool IsStateVisited(long stateIndex)
        {
            long bytePosition = stateIndex / 8;
            int bitPosition = (byte)(stateIndex % 8);
            byte mask = (byte)(1 << bitPosition);
            return (_stateBits[bytePosition] & mask) != 0;
        }

        private long CalculateTotalStates()
        {
            long total = 1;
            for (int i = 0; i < K ; i++)
            {
                total *= (TotalPositions - i);
            }
            return total; 
        }

        private void InitializeChunks()
        {
            _pdbChunks = new Dictionary<long, byte[]>();
            // Chunks are allocated lazily and stored in dictionary
        }

        private byte GetDistance(long index)
        {
            if (_useMemoryMappedFile)
            {
                return _mmPdb!.GetDistance(index);
            }

            long chunkIdx = index >> ChunkShift;
            int offset = (int)(index & ChunkMask);

            if (!_pdbChunks!.TryGetValue(chunkIdx, out byte[]? chunk))
                return byte.MaxValue;
            return chunk[offset];
        }

        private void SetDistance(long index, byte distance)
        {
            if (_useMemoryMappedFile)
            {
                _mmPdb!.SetDistance(index, distance);
                return;
            }

            long chunkIdx = index >> ChunkShift;
            int offset = (int)(index & ChunkMask);

            if (!_pdbChunks!.TryGetValue(chunkIdx, out byte[]? chunk))
            {
                chunk = new byte[ChunkSize];
                Array.Fill(chunk, byte.MaxValue);
                _pdbChunks[chunkIdx] = chunk;
            }
            chunk[offset] = distance;
        }

        /// <summary>
        /// Executes the reverse BFS loop starting from the goal pattern configuration.
        /// </summary>
        public PatternDatabase GeneratePdb(byte[] goalTiles, int blankIndex, Action<long, long>? Verify = null)
        {
            Span<byte> goalTileSpan = goalTiles;
            Stopwatch sw = new();
            sw.Start();
            PriorityQueue<long, byte> queue = new();

            // Copy the goal state into the pool to ensure it's managed by the pool
            long goalIndex = _pdbCodec.Encode(goalTileSpan);

            // Encode and store the starting goal state
            queue.Enqueue(goalIndex, 0);

            StatesProcessed = 0;
            byte currentCost = 0;
            Span<byte> currentTileSpan = new byte[goalTiles.Length];
            while (queue.TryDequeue(out long currentIndex, out currentCost))
            {
                MaxQueueLength = Math.Max(MaxQueueLength, queue.Count);
                _pdbCodec.DecodeMem(currentIndex, currentTileSpan);

                byte currentDistance = GetDistance(currentIndex);
                if (currentCost < currentDistance)
                {
                    SetDistance(currentIndex, currentCost);
                }
                else if (currentCost == currentDistance)// currentCost == currentDistance. Same state has been queued by two different parents, and has already been evaluated
                {
                    continue;
                }
                else
                    throw new InvalidOperationException($"Trying to re-examine a state with a higher cost second time round. Index {currentIndex}, Current Cost {currentCost}, Stored cost {currentDistance}");
                // Generate physical movements of the blank tile
                Span<byte> nextTiles = new byte[goalTiles.Length];
                foreach (byte nextBlank in GetValidMoves(currentTileSpan[blankIndex]))
                {
                    byte neighborCost = currentCost;
                    // Create a clone for the neighbor state
                    currentTileSpan.CopyTo(nextTiles);
                    nextTiles[blankIndex] = nextBlank;

                    // If the blank tile moves into a slot occupied by one of our tracked tiles,
                    // that tracked tile physically shifts into the old blank space slot.
                    int shiftedTileIndex = -1;
                    for (int i = 0; i < nextTiles.Length; i++)
                    {
                        if (i == blankIndex)
                            continue;

                        if (nextTiles[i] == nextBlank)
                        {
                            shiftedTileIndex = i;
                            break;
                        }
                    }
                    if (shiftedTileIndex != -1)
                    {
                        nextTiles[shiftedTileIndex] = currentTileSpan[blankIndex];
                        neighborCost++;  // Only increase if this neighbor move affects a tracked tile
                    }

                    long neighborIndex = _pdbCodec.Encode(nextTiles);
                    // If this pattern layout hasn't been visited yet, update and enqueue
                    bool isNeighborVisited = IsStateVisited(neighborIndex);

                    if (!isNeighborVisited)
                    {
                        if (Verify != null)
                        {
                            Verify(neighborIndex, neighborCost);
                        }
                        queue.Enqueue(neighborIndex, neighborCost);
                        MaxCost = Math.Max(MaxCost, neighborCost);
                    }
                }
                StatesProcessed++;
                SetStateVisited(currentIndex);

            }
            sw.Stop();
            ElapsedMs = sw.ElapsedMilliseconds;

            return new PatternDatabase(GridSize, K,  _totalStates, goalTiles, blankIndex, _pdbChunks!, ChunkShift);
        }
        private IEnumerable<int> GetValidMoves(int blankPos)
        {
            int row = blankPos / GridSize;
            int col = blankPos % GridSize;

            if (row > 0) yield return blankPos - GridSize; // Move Up
            if (row < GridSize - 1) yield return blankPos + GridSize; // Move Down
            if (col > 0) yield return blankPos - 1; // Move Left
            if (col < GridSize - 1) yield return blankPos + 1; // Move Right
        }
    }

}

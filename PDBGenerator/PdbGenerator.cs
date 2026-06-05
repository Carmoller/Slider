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

    public class PdbGenerator
    {
        public long ElapsedMs { get; private set; }
        public long StatesProcessed { get; private set; }
        // Configuration for the target system
        private int GridSize;
        private int TotalPositions;
        private int K = 6;
        private bool _includeBlank = true;
        private long _maxQueueLength;

        // Chunk configuration to avoid huge contiguous allocations
        private const int ChunkShift = 20; // 1MB per chunk
        private const int ChunkSize = 1 << ChunkShift;
        private const int ChunkMask = ChunkSize - 1;

        private Dictionary<long, byte[]>? _pdbChunks;
        private MemoryMappedPatternDatabase? _mmPdb;
        private int _totalStates;
        private Codec _codec;
        private bool _useMemoryMappedFile;

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
            _codec = new(gridSize, k);
            TotalPositions = GridSize * GridSize;
            K = k;
            _totalStates = CalculateTotalStates();
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

        private int CalculateTotalStates()
        {
            // We ignore the blank tile position in the math
            int total = 1;
            for (int i = 0; i < K; i++)
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
        /// Represents the positions of our K target tiles on the board, plus the blank space.
        /// </summary>
        public struct PatternState
        {
            // Stores the 0-indexed board positions of the K tracked tiles
            public Memory<byte> TilePositions;
            public BufferPool.Slot Slot;
            public byte BlankPosition;
        }

        /// <summary>
        /// Executes the reverse BFS loop starting from the goal pattern configuration.
        /// </summary>
        public PatternDatabase GeneratePdb(PatternState goalState, int totalStates)
        {
            _totalStates = totalStates;
            return GeneratePdb(goalState);
        }
        public PatternDatabase GeneratePdb(PatternState goalState)
        {
            BufferPool pool = new(_totalStates + 1, K);
            Stopwatch sw = new();
            sw.Start();
            PriorityQueue<PatternState, byte> queue = new();

            // Copy the goal state into the pool to ensure it's managed by the pool
            BufferPool.Slot goalSlot = pool.Rent();
            Memory<byte> goalTiles = pool.GetMemory(goalSlot);
            goalState.TilePositions.CopyTo(goalTiles);

            PatternState pooledGoalState = new PatternState
            {
                TilePositions = goalTiles,
                BlankPosition = goalState.BlankPosition,
                Slot = goalSlot
            };

            // Encode and store the starting goal state
            long goalIndex = EncodePattern(pooledGoalState.TilePositions, pooledGoalState.BlankPosition);
            SetDistance(goalIndex, 0);
            queue.Enqueue(pooledGoalState, 0);

            long processedStates = 0;
            byte currentCost;
            while (queue.TryDequeue(out PatternState current, out currentCost))
            {
                _maxQueueLength = Math.Max(_maxQueueLength, queue.Count);
                byte neighborCost = currentCost;
                long currentIndex = EncodePattern(current.TilePositions, current.BlankPosition);
                // Skip if we have already calculated this state with a lower cost (can happen due to multiple paths to the same state)
                if (currentCost > GetDistance(currentIndex))
                {
                    pool.Return(current.Slot);
                    continue;
                }

                processedStates++;
                if (processedStates % 1000000 == 0)
                {
                    Console.WriteLine($"Processed {processedStates} states. Queue size: {queue.Count}");
                }

                // Generate physical movements of the blank tile
                foreach (byte nextBlank in GetValidMoves(current.BlankPosition))
                {
                    // Create a clone for the neighbor state
                    BufferPool.Slot slot = pool.Rent();
                    Memory<byte> nextTiles = pool.GetMemory(slot);
                    current.TilePositions.CopyTo(nextTiles);

                    // If the blank tile moves into a slot occupied by one of our tracked tiles,
                    // that tracked tile physically shifts into the old blank space slot.
                    int shiftedTileIndex = -1;
                    Span<byte> nextTilesSpan = nextTiles.Span;
                    for (int i = 0; i < nextTiles.Length; i++)
                    {
                        if (nextTilesSpan[i] == nextBlank)
                        {
                            shiftedTileIndex = i;
                            break;
                        }
                    }
                    if (shiftedTileIndex != -1)
                    {
                        nextTilesSpan[shiftedTileIndex] = current.BlankPosition;
                        neighborCost++;  // Only increase if this neighbor move affects a tracked tile
                    }

                    long neighborIndex = EncodePattern(nextTiles, nextBlank);
                    if (neighborIndex == -1)
                    {
                        pool.Return(slot);
                        continue;
                    }
                    // If this pattern layout hasn't been visited yet, update and enqueue
                    byte neighborDist = GetDistance(neighborIndex);
                    if (neighborDist == byte.MaxValue || neighborDist > neighborCost)
                    {
                        SetDistance(neighborIndex, neighborCost);
                        queue.Enqueue(new PatternState
                        {
                            TilePositions = nextTiles,
                            BlankPosition = nextBlank,
                            Slot = slot
                        }, neighborCost);
                    }
                    else
                    {
                        pool.Return(slot);
                    }
                }

                // Return current slot only after we're done processing all neighbors
                pool.Return(current.Slot);
            }
            sw.Stop();
            Console.WriteLine($"Processed {processedStates} states. Queue size: {queue.Count}. Time spent {sw.Elapsed}");
            Console.WriteLine($"Max queue length {_maxQueueLength}");
            StatesProcessed = processedStates;
            ElapsedMs = sw.ElapsedMilliseconds;

            byte[] tilePositionsByte = goalState.TilePositions.Span.ToArray();
            if (_useMemoryMappedFile)
            {
                _mmPdb!.Close();
                return new PatternDatabase(GridSize, K, _includeBlank,  _totalStates, tilePositionsByte, _mmPdb.FilePath);
            }
            else
            {

                return new PatternDatabase(GridSize, K, _includeBlank,  _totalStates, tilePositionsByte, _pdbChunks!, ChunkShift);
            }
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

        private long EncodePattern(Memory<byte> positions, byte blankPosition)
        {
            return _codec.Encode(positions, blankPosition);
        }
    }

}

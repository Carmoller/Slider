using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace PDBGenerator
{
    public class Codec
    {
        private readonly int N;
        private readonly int K;
        private readonly long[] Factorials;
        private readonly long[,] BinomialCoefficients;

        public Codec(int boardSize, int sequenceLength)
        {
            N = boardSize*boardSize;
            K = sequenceLength;
            // 1. Precompute factorials for Lehmer code mapping
            Factorials = new long[K + 1];
            Factorials[0] = 1;
            for (int i = 1; i <= K; i++)
            {
                Factorials[i] = Factorials[i - 1] * i;
            }

            // 2. Precompute Pascal's Triangle (n choose k) for the combinadic system
            BinomialCoefficients = new long[N + 1, K + 1];
            for (int i = 0; i <= N; i++)
            {
                for (int j = 0; j <= Math.Min(i, K); j++)
                {
                    if (j == 0 || j == i)
                        BinomialCoefficients[i, j] = 1;
                    else
                        BinomialCoefficients[i, j] = BinomialCoefficients[i - 1, j - 1] + BinomialCoefficients[i - 1, j];
                }
            }
        }

        /// <summary>
        /// Encodes tracked tile positions AND the blank tile position into a single unified database index.
        /// </summary>
        /// <param name="tilePositions">An array of exactly K integers representing the 0-indexed positions of the tiles.</param>
        /// <param name="blankPosition">The 0-indexed board position of the empty/blank tile (0 to 99).</param>
        public long Encode(byte[] tilePositions, byte blankPosition)
        {
            if (tilePositions == null || tilePositions.Length != K)
                throw new ArgumentException($"Array must contain exactly {K} elements.");
            if (blankPosition < 0 || blankPosition >= N)
                throw new ArgumentOutOfRangeException(nameof(blankPosition), $"Blank position must be between 0 and {N - 1}.");

            for (int i = 0; i < K; i++)
            {
                if (tilePositions[i] == blankPosition)
                {
                    // Return a special indicator value.
                    return -1;
                }
            }

            // 1. Extract combination footprint by sorting the positions
            int[] sortedPositions = new int[K];
            for (int i = 0; i < K; i++) sortedPositions[i] = tilePositions[i];
            Array.Sort(sortedPositions);

            // 2. Compute Combinadic Rank (Combination space)
            long combinationRank = 0;
            for (int i = K; i >= 1; i--)
            {
                int positionValue = sortedPositions[i - 1];
                combinationRank += BinomialCoefficients[positionValue, i];
            }

            // 3. Compute Permutation Rank via Lehmer Code using stackalloc to avoid GC thrashing
            long permutationRank = 0;
            Span<int> availablePool = stackalloc int[K];
            for (int i = 0; i < K; i++) availablePool[i] = sortedPositions[i];

            for (int i = 0; i < K; i++)
            {
                int targetPos = tilePositions[i];
                int relativeIndex = 0;
                int poolSize = K - i;

                for (int j = 0; j < poolSize; j++)
                {
                    if (availablePool[j] == targetPos)
                    {
                        relativeIndex = j;
                        for (int m = j; m < poolSize - 1; m++)
                        {
                            availablePool[m] = availablePool[m + 1];
                        }
                        break;
                    }
                }
                permutationRank += relativeIndex * Factorials[K - 1 - i];
            }

            // 4. Combine the pure tile layout rank with the blank tile space
            long pureTileRank = (combinationRank * Factorials[K]) + permutationRank;

            // Multiply by N to shift the index, then safely embed the blank position offset
            return (pureTileRank * N) + blankPosition;
        }

        /// <summary>
        /// Decodes a unified database index back into the original 6 tile positions and the blank tile position.
        /// </summary>
        public DecodeResult Decode(long dynamicDatabaseIndex)
        {
            // 1. Extract the blank position using remainder math, and isolate the pure tile rank
            byte blankPosition = (byte)(dynamicDatabaseIndex % N);
            long pureTileRank = dynamicDatabaseIndex / N;

            long combinationRank = pureTileRank / Factorials[K];
            long permutationRank = pureTileRank % Factorials[K];

            // 2. Unrank Combinadic
            byte[] chosenPositions = new byte[K];
            byte nextSlot = (byte)(N - 1);
            long remainingCombRank = combinationRank;

            for (int i = K; i >= 1; i--)
            {
                while (BinomialCoefficients[nextSlot, i] > remainingCombRank)
                {
                    nextSlot--;
                }
                chosenPositions[i - 1] = nextSlot;
                remainingCombRank -= BinomialCoefficients[nextSlot, i];
            }

            // 3. Unrank Permutation via Lehmer code
            byte[] resultPositions = new byte[K];
            List<byte> availablePool = new (chosenPositions);
            long remainingPermRank = permutationRank;

            for (int i = 0; i < K; i++)
            {
                long factSpace = Factorials[K - 1 - i];
                int poolIndex = (int)(remainingPermRank / factSpace);
                remainingPermRank %= factSpace;

                resultPositions[i] = availablePool[poolIndex];
                availablePool.RemoveAt(poolIndex);
            }

            return new DecodeResult(resultPositions, blankPosition);
        }
    }
}

using Slider.Common.Interfaces;
using Slider.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Slider
{
    public class PuzzleGenerator : IGenerator
    {
        private IEnumerable<int> GetAdjacentTiles(int gridSize, int row, int column)
        {
            if (row != 0)
                yield return ((row - 1) * gridSize) + column; // One row up
            if (row != gridSize - 1)
                yield return ((row + 1) * gridSize) + column; // One row down
            if (column != 0)
                yield return (row * gridSize + column - 1);
            if (column != gridSize - 1)
                yield return (row * gridSize + column + 1);
        }

        private int CountMovableAdjacentTiles(GeneratedTile[] board, int gridSize, int row, int column)
        {
            int count = 0;

            foreach (int newIndex in GetAdjacentTiles(gridSize, row, column))
            {
                if (!board[newIndex].IsLocked)
                    count++;
            }
            return count;
        }

        private bool CanLock(GeneratedTile[] board, int gridSize, int index)
        {
            (int row, int col) = Math.DivRem(index, gridSize);
            bool oldLock = board[index].IsLocked;

            try
            {
                board[index].IsLocked = true;
                foreach (int adjacentIndex in GetAdjacentTiles(gridSize, row, col))
                {
                    (int adjacentRow, int adjacentColumn) = Math.DivRem(adjacentIndex, gridSize);
                    int count = CountMovableAdjacentTiles(board, gridSize, adjacentRow, adjacentColumn);
                    if (count < 2)
                        return false;
                }
                return true;
            }
            finally
            {
                board[index].IsLocked = oldLock;
            }
        }

        private void SwapTiles(GeneratedTile[] board, int index1, int index2)
        {
            GeneratedTile old = board[index1];
            board[index1] = board[index2];
            board[index2] = old;
        }

        private void LockCandidate(Random rnd, GeneratedTile[] board, int indexToLock)
        {
            // Locking a tile is a multi-step process:
            // 1. board[indexToLock] must be swapped for the tile that is supposed to end up at that index
            // 2. Set the new til to IsLocked = true
            // 3. Ensure that the board is still solvable from the parity rules
            // 3a. Swap tiles until 3. is achieved
            int expectedValue = indexToLock + 1;
            if (board[indexToLock].TileValue != expectedValue)
            {
                // Find index of the tile where the expected value is currently residing
                int index = board.Select((item, idx) => new { item, idx })
                    .FirstOrDefault(p => p.item.TileValue == expectedValue) ?.idx ?? -1; // Returns -1 if no match is found
                // Swap the two tiles
                SwapTiles(board, index, indexToLock);
            }
            // Lock the tile
            board[indexToLock].IsLocked = true;

            // We expect the board to fail the parity rule now
            if (!IsSolvable(board))
            {
                int swapIndex1 = -1;
                do
                {
                    swapIndex1 = rnd.Next(board.Length);
                    if (!board[swapIndex1].IsLocked && board[swapIndex1].TileValue != 0)
                        break;
                } while (true);
                int swapIndex2 = -1;
                do
                {
                    swapIndex2 = rnd.Next(board.Length);
                    if (swapIndex2 != swapIndex1 && !board[swapIndex2].IsLocked && board[swapIndex2].TileValue != 0)
                        break;
                } while (true);
                SwapTiles(board, swapIndex1, swapIndex2);
                if (!IsSolvable(board))
                    throw new InvalidOperationException("Board is unsolvable even after swap");
            }
        }
        private void LockTiles(GeneratedTile[] board, int gridSize, int tilesToLock)
        {
            Random rnd = new();
            for (int i = 0; i < tilesToLock; i++)
            {
                bool validLockedTile = false;
                while (!validLockedTile)
                {
                    // Find a random, non-locked tile
                    int candidateLock = 0;
                    do
                    {
                        candidateLock = rnd.Next(board.Length - 1); // Do not lock the tile where the blank should end up
                    } while (board[candidateLock].IsLocked);
                    validLockedTile = CanLock(board, gridSize, candidateLock);
                    if (!validLockedTile)
                        continue;
                    LockCandidate(rnd, board, candidateLock);
                }
            }
        }

        public static bool IsSolvable(GeneratedTile[] board)
        {
            int gridSize = (int)Math.Sqrt(board.Length);
            return IsSolvable(board.Select(p=>p.TileValue).ToArray(), gridSize);
        }
        public static bool IsSolvable(byte[] board, int gridSize)
        {
            int inversions = 0;
            int blankIndex = 0;
            for (int i = 0; i < board.Length; i++)
            {
                if (board[i] == 0)
                    blankIndex = i;
                for (int j = i + 1; j < board.Length; j++)
                {
                    if (board[i] > board[j] && board[i] != 0 && board[j] != 0)
                    {
                        inversions++;
                    }
                }
            }
            if (gridSize % 2 == 1)
            {
                // Odd grid size: solvable if inversions count is even
                return inversions % 2 == 0;
            }
            else
            {
                // Even grid size: solvable if blank is on an even row counting from the bottom and inversions count is odd,
                // or if blank is on an odd row counting from the bottom and inversions count is even
                int blankRowFromBottom = gridSize - (blankIndex / gridSize);
                return (blankRowFromBottom % 2 == 0) == (inversions % 2 == 1);
            }
        }

        private bool IsSolved(GeneratedTile[] board)
        {
            for (int i = 0; i < board.Length - 1; i++)
            {
                if (board[i].TileValue != i + 1)
                {
                    return false;
                }
            }
            return board[board.Length - 1].TileValue == 0;
        }
        public GeneratedTile[] Generate(int gridSize, int tilesToLock)
        {
            // Generate a random solvable configuration of the sliding puzzle
            GeneratedTile[] board = new GeneratedTile[gridSize*gridSize];
            for (int i = 0; i < board.Length; i++)
            {
                board[i] = new GeneratedTile { TileValue = (byte)i };
            }

            Random rand = new Random();
            do
            {
                // Shuffle the tiles using Fisher-Yates algorithm
                for (int i = board.Length - 1; i > 0; i--)
                {
                    int j = rand.Next(0, i + 1);
                    (board[j], board[i]) = (board[i], board[j]);
                }
            } while (!IsSolvable(board) || IsSolved(board));
            LockTiles(board, gridSize, tilesToLock);
            return board;
        }
    }
}

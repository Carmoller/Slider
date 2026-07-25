using Slider;
using Slider.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTest
{
    internal class BoardHelper
    {
        public static int[] GetBoardPositionsFromBoardValues(Span<byte> boardValues)
        {
            int[] boardPositions = new int[boardValues.Length];
            for (int i = 0; i < boardValues.Length; i++)
            {
                boardPositions[boardValues[i]] = i;
            }
            return boardPositions;
        }
        public static bool IsSolvable(byte[] board)
        {
            bool valid = true;
            foreach (IGrouping<byte, byte> value in board.GroupBy(p => p))
            {
                if (value.Count() > 1)
                {
                    valid = false;
                    Console.WriteLine($"{value.Key} occurs {value.Count()} times");
                }
            }
            for (int i = 0; i < board.Length; i++)
            {
                if (!board.Any(p => p == i))
                {
                    valid = false;
                    Console.Write($"Tile {i} not found");
                }
            }
            if (!valid)
            {
                // No reason to check further than this
                return false;
            }

            int gridSize = (int)(Math.Sqrt(board.Length));
            return PuzzleGenerator.IsSolvable(board, gridSize);
        }

        public static void DoMove(byte[] board, Move move, int count)
        {
            int gridSize = (int)Math.Sqrt(board.Length);
            int PositionFromRowCol(int row, int column)
            {
                return row * gridSize + column;
            }
            int fromPosition = PositionFromRowCol(move.FromRow, move.FromColumn);
            Assert.IsLessThan(board.Length, fromPosition, $"Move #{count}: Moving from (row={move.FromRow}, col={move.FromColumn}), which is outside of the board");

            int toPosition = PositionFromRowCol(move.ToRow, move.ToColumn);
            Assert.IsLessThan(board.Length, toPosition, $"Move #{count}: Moving from (row={move.FromRow}, col={move.FromColumn}), which is outside of the board");

            if (board[toPosition] != 0)
            {
                Assert.Fail($"Move #{count}: Moving to (row={move.ToRow}, col={move.ToColumn}), which does not contain the blank");
            }
            byte temp = board[toPosition];
            board[toPosition] = board[fromPosition];
            board[fromPosition] = temp;
        }

        public static void VerifyMoves(byte[] board, SolveResult result)
        {
            int size = (int)Math.Sqrt(board.Length);
            int count = 0;

            foreach (Move move in result.Moves)
            {
                count++;
                DoMove(board, move, count);
            }
        }

        public static Span<byte> GetDefaultTargetBoard(Span<byte> startBoard)
        {
            int count = startBoard.Length;
            Span<byte> targetBoard = new byte[count];
            for (int i = 1; i < count; i++)
            {
                targetBoard[i - 1] = (byte)i;
            }
            return targetBoard;
        }
        public static void VerifySolvedBoard(byte[] board, Span<byte> targetBoard)
        {
            int size = (int)Math.Sqrt(board.Length);
            if (targetBoard == Span<byte>.Empty)
            {
                targetBoard = GetDefaultTargetBoard(board);
            }

            for (int i = 0; i < board.Length; i++)
            {
                Assert.AreEqual(board[i], targetBoard[i], $"Tile {board[i]} is not in correct spot");
            }
            Console.WriteLine("Verified board is solved");
        }
        public static void VerifySolvedRow(byte[] board, int row)
        {
            int size = (int)Math.Sqrt(board.Length);
            for (int col = 0; col < size; col++)
            {
                Assert.AreEqual((row * size + col) + 1, board[row*size + col]);
            }
        }
        public static void VerifySolvedColumn(byte[] board, int column)
        {
            int size = (int)Math.Sqrt(board.Length);
            for (int row = 0; row < size; row++)
            {
                Assert.AreEqual((row * size + column) + 1, board[row * size + column]);
            }
        }
    }
}

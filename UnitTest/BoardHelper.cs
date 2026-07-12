using Slider;
using Slider.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTest
{
    internal class BoardHelper
    {
        public static List<BoardTile> GetBoardFromArray(byte[] byteBoard)
        {
            List<BoardTile> board = new();
            int gridSize = (int)(Math.Sqrt(byteBoard.Length));
            for (int i = 0; i < byteBoard.Length; i++)
            {
                board.Add(new BoardTile { Value = byteBoard[i], Row = i / gridSize, Column = i % gridSize });
            }
            return board.OrderBy(p => p.Row).ThenBy(p => p.Column).ToList();
        }

        public static int[] GetBoardPositionsFromBoardValues(Span<byte> boardValues)
        {
            int[] boardPositions = new int[boardValues.Length];
            for (int i = 0; i < boardValues.Length; i++)
            {
                boardPositions[boardValues[i]] = i;
            }
            return boardPositions;
        }
        public static bool IsSolvable(List<BoardTile> board)
        {
            bool valid = true;
            foreach (IGrouping<byte, BoardTile> value in board.GroupBy(p => p.Value))
            {
                if (value.Count() > 1)
                {
                    valid = false;
                    Console.WriteLine($"{value.Key} occurs {value.Count()} times");
                }
            }
            for (int i = 0; i < board.Count; i++)
            {
                if (!board.Any(p => p.Value == i))
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

            int gridSize = (int)(Math.Sqrt(board.Count));
            List<byte> puzzle = board.OrderBy(p => p.Row).ThenBy(p => p.Column).Select(p => p.Value).ToList();
            return PuzzleGenerator.IsSolvable(puzzle, gridSize);
        }

        public static void DoMove(List<BoardTile> board, Move move, int count)
        {
            BoardTile? fromTile = board.FirstOrDefault(p => p.Row == move.FromRow && p.Column == move.FromColumn);
            if (fromTile == null)
            {
                Assert.Fail($"Move #{count}: Moving from (row={move.FromRow}, col={move.FromColumn}), which is outside of the board");
            }
            BoardTile? toTile = board.FirstOrDefault(p => p.Row == move.ToRow && p.Column == move.ToColumn);
            if (toTile == null)
            {
                Assert.Fail($"Move #{count}: Moving from (row={move.FromRow}, col={move.FromColumn}), which is outside of the board");
            }

            if (toTile.Value != 0)
            {
                Assert.Fail($"Move #{count}: Moving to (row={move.ToRow}, col={move.ToColumn}), which does not contain the blank");
            }
            int tempRow = toTile.Row;
            int tempCol = toTile.Column;

            toTile.Row = fromTile.Row;
            toTile.Column = fromTile.Column;

            fromTile.Row = tempRow;
            fromTile.Column = tempCol;
        }

        public static void VerifyMoves(List<BoardTile> board, SolveResult result)
        {
            int size = (int)Math.Sqrt(board.Count);
            int count = 0;

            foreach (Move move in result.Moves)
            {
                count++;
                DoMove(board, move, count);
            }
        }

        private static Span<byte> GetDefaultTargetBoard(int count)
        {
            Span<byte> targetBoard = new byte[count];
            for (int i = 1; i < count; i++)
            {
                targetBoard[i - 1] = (byte)i;
            }
            return targetBoard;
        }
        public static void VerifySolvedBoard(List<BoardTile> board, Span<byte> targetBoard)
        {
            int size = (int)Math.Sqrt(board.Count);
            if (targetBoard == Span<byte>.Empty)
            {
                targetBoard = GetDefaultTargetBoard(board.Count);
            }

            for (int i= 0; i<board.Count; i++)
            {
                (int row, int col) = Math.DivRem(i, size);
                BoardTile tile = board.First(p=>p.Row == row && p.Column == col);
                Assert.AreEqual(tile.Value, targetBoard[i], $"Tile {tile.Value} is not in correct spot");
            }
            Console.Write("Verified board is solved");
        }
        public static void VerifySolvedRow(List<BoardTile> board, int row)
        {
            int size = (int)Math.Sqrt(board.Count);
            for (int col = 0; col < size; col++)
            {
                Assert.AreEqual((row*size + col) + 1, board.First(p => p.Row == row && p.Column == col).Value);
            }
        }
        public static void VerifySolvedColumn(List<BoardTile> board, int column)
        {
            int size = (int)Math.Sqrt(board.Count);
            for (int row = 0; row < size; row++)
            {
                Assert.AreEqual((row * size + column) + 1, board.First(p => p.Row == row&& p.Column == column).Value);
            }
        }
    }
}

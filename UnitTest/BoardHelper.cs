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
        public static bool IsSolvable(List<BoardTile> board)
        {
            int gridSize = (int)(Math.Sqrt(board.Count));
            List<byte> puzzle = board.OrderBy(p => p.Row).ThenBy(p => p.Column).Select(p => p.Value).ToList();
            return PuzzleGenerator.IsSolvable(puzzle, gridSize);
        }

        public static void VerifyMoves(List<BoardTile> board, SolveResult result)
        {
            int size = (int)Math.Sqrt(board.Count);
            int count = 0;

            foreach (Move move in result.Moves)
            {
                count++;
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
        }

        public static void VerifySolvedBoard(List<BoardTile> board)
        {
            int size = (int)Math.Sqrt(board.Count);
            foreach (BoardTile tile in board)
            {
                if (tile.Value == 0) // Must be in bottom right corner
                {
                    Assert.AreEqual(size - 1, tile.Row, $"Empty tile should be at ({size}, {size}), but was at ({tile.Row}, {tile.Column}) ");
                    Assert.AreEqual(size - 1, tile.Column, $"Empty tile should be at ({size}, {size}), but was at ({tile.Row},{tile.Column}) ");
                }
                else
                    Assert.AreEqual((tile.Row * size + tile.Column) + 1, tile.Value, $"Tile {tile.Value} is not in correct spot");
            }
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

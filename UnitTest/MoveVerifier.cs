using Slider.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTest
{
    public static class MoveVerifier
    {
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
    }
}

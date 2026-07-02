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
            return board.OrderBy(p=>p.Row).ThenBy(p=>p.Column).ToList();
        }
        public static bool IsSolvable(List<BoardTile> board)
        {
            int gridSize = (int)(Math.Sqrt(board.Count));
            List<byte> puzzle = board.OrderBy(p => p.Row).ThenBy(p => p.Column).Select(p => p.Value).ToList();
            return PuzzleGenerator.IsSolvable(puzzle, gridSize);
        }
    }
}

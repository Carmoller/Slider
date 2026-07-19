using Slider.Common.Interfaces;
using Slider.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Slider
{
    public class PuzzleGenerator : IGenerator
    {
        public static bool IsSolvable(byte[] board, int gridSize)
        {
            int inversions = 0;
            for (int i = 0; i < board.Length; i++)
            {
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
                int blankRowFromBottom = gridSize - (board.IndexOf((byte)0) / gridSize);
                return (blankRowFromBottom % 2 == 0) == (inversions % 2 == 1);
            }
        }

        private bool IsSolved(byte[] board)
        {
            for (int i = 0; i < board.Length - 1; i++)
            {
                if (board[i] != i + 1)
                {
                    return false;
                }
            }
            return board[board.Length - 1] == 0;
        }
        public byte[] Generate(int gridSize)
        {
            // Generate a random solvable configuration of the sliding puzzle
            byte[] board = new byte[gridSize*gridSize];
            for (int i = 0; i < board.Length; i++)
            {
                board[i] = (byte)i;
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
            } while (!IsSolvable(board, gridSize) || IsSolved(board));

            return board;
        }
    }
}

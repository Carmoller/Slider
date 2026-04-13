using Slider.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Slider
{
    public class Generator : IGenerator
    {
        private bool IsSolvable(List<int> tiles, int gridSize)
        {
            int inversions = 0;
            for (int i = 0; i < tiles.Count; i++)
            {
                for (int j = i + 1; j < tiles.Count; j++)
                {
                    if (tiles[i] > tiles[j] && tiles[i] != 0 && tiles[j] != 0)
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
                int blankRowFromBottom = gridSize - (tiles.IndexOf(0) / gridSize);
                return (blankRowFromBottom % 2 == 0) == (inversions % 2 == 1);
            }
        }

        private bool IsSolved(List<int> tiles)
        {
            for (int i = 0; i < tiles.Count - 1; i++)
            {
                if (tiles[i] != i + 1)
                {
                    return false;
                }
            }
            return tiles[tiles.Count - 1] == 0;
        }
        public List<int> Generate(int gridSize)
        {
            // Generate a random solvable configuration of the sliding puzzle
            List<int> tiles = new List<int>();
            for (int i = 0; i < gridSize * gridSize; i++)
            {
                tiles.Add(i);
            }

            Random rand = new Random();
            do
            {
                // Shuffle the tiles using Fisher-Yates algorithm
                for (int i = tiles.Count - 1; i > 0; i--)
                {
                    int j = rand.Next(0, i + 1);
                    int temp = tiles[i];
                    tiles[i] = tiles[j];
                    tiles[j] = temp;
                }
            } while (!IsSolvable(tiles, gridSize) || IsSolved(tiles));

            return tiles;
        }
    }
}

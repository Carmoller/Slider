using Slider.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider
{
    public class PuzzleChecker
    {
        private static SolvableStatus CheckCompleteness(byte[] board, int gridSize)
        {
            // Check that all numbers are present exactly once, and only one blank
            Dictionary<int, bool> encounteredValues = new();
            for (int i = 1; i < gridSize * gridSize; i++)
            {
                encounteredValues[i] = false;
            }
            bool blankFound = false;
            for (int i = 0; i < board.Length; i++)
            {
                int value = board[i];
                if (value == 0)
                {
                    if (blankFound == true)
                        return SolvableStatus.Incomplete;
                    blankFound = true;
                    continue;
                }
                if (encounteredValues[value] == true)
                    return SolvableStatus.DuplicateTiles;
                encounteredValues[value] = true;
            }
            // if we get here, logic says that all values should be accounted for - but we still check it
            if (encounteredValues.Values.Any(p => p == false))
                return SolvableStatus.Incomplete;
            return SolvableStatus.Solvable;
        }

        // The IEnumerable<ITileControlViewModel> is used throughout the GUI
        public static SolvableStatus IsSolvable(IEnumerable<ITileControlViewModel> vmList, int gridSize)
        {
            return IsSolvable(vmList.Select(p => (byte)p.Value).ToArray(), gridSize);
        }


        public static SolvableStatus IsSolvable(byte[] board, int gridSize)
        {
            SolvableStatus completenesStatus = CheckCompleteness(board, gridSize);
            if (completenesStatus != SolvableStatus.Solvable)
                return completenesStatus;
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
                return inversions % 2 == 0 ? SolvableStatus.Solvable : SolvableStatus.NotSolvable;
            }
            else
            {
                // Even grid size: solvable if blank is on an even row counting from the bottom and inversions count is odd,
                // or if blank is on an odd row counting from the bottom and inversions count is even
                int blankRowFromBottom = gridSize - (board.IndexOf((byte)0) / gridSize);
                return (blankRowFromBottom % 2 == 0) == (inversions % 2 == 1) ? SolvableStatus.Solvable : SolvableStatus.NotSolvable;
            }
        }

    }
}

using Slider.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Heuristics
{
    public class HeuristicInversionDistance : HeuristicElementBase, IHeuristicElement
    {
        private int[] _tree;
        private int[] _activeTargets;

        public string Name { get { return "Inversion Counter"; } }
        public bool IsAdditive { get { return false; ; } }

        public HeuristicInversionDistance(Span<int> targetPositions, int gridSize) : base(targetPositions, gridSize)
        {
            _tree = new int[gridSize * gridSize + 1];
            _activeTargets = new int[gridSize * gridSize];
        }
        public int Calculate(Span<byte> currentBoard, int gridSize)
        {
            int count = 0;
            for (int i = 0; i < currentBoard.Length; i++)
            {
                byte rawTile = currentBoard[i];
                if (rawTile != 0)
                {
                    // Fenwick trees require 1-based indexing, so add 1
                    _activeTargets[count++] = TargetPositions[rawTile] + 1;
                }
            }

            // Fast clear of our reuse tracking tree array
            Array.Clear(_tree, 0, count + 1);
            int inversions = 0;

            // Purely iterative backward tracking scan
            for (int i = count - 1; i >= 0; i--)
            {
                int val = _activeTargets[i];

                // Query the sum of elements smaller than 'val'
                int idx = val - 1;
                while (idx > 0)
                {
                    inversions += _tree[idx];
                    idx -= idx & -idx; // Bitwise shortcut jumps directly up the tree steps
                }

                idx = val;
                while (idx <= count)
                {
                    _tree[idx]++;
                    idx += idx & -idx; // Bitwise shortcut jumps down the tree
                }
            }

            return inversions;
        }

        public void UpdateTargetPositionsFromBoard(Span<byte> board)
        {
            TargetPositionsUpdateFromBoard(board);
        }
    }

}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Slider.Solver
{
    internal class ArrayPool2D
    {
        private readonly Stack<byte[,]> _scratchBoardPool = new();
        public byte[,] RentScratchBoard(int gridSize)
        {
            return _scratchBoardPool.Count > 0 ?
                _scratchBoardPool.Pop() :
                new byte[gridSize, gridSize];
        }

        public void ReturnScratchBoard(byte[,] board)
        {
            _scratchBoardPool.Push(board);
        }
    }
}

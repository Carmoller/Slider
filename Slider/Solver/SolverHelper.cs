using Slider.Common;
using Slider.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Solver
{
    internal class SolverHelper
    {
        public static byte[] CreateGoalBoard(int gridSize)
        {
            byte[] goalBoard = new byte[gridSize * gridSize];
            for (int i = 1; i < goalBoard.Length; i++)
            {
                goalBoard[i - 1] = (byte)i;
            }
            return goalBoard;
        }

        public static StateInfo CreateStateInfoFromBoard(Span<byte> board,
            ChunkedArrayPoolUnsafe arrayPool,
            ChunkedStructPool<StateInfo> stateInfoPool,
            IHeuristicCalculator heuristicsCalculator,
            int gridSize,
            Func<StateInfo, double> CalculateF,
            Func<IHeuristicCalculator, Span<byte>, int, int> GetHeuristics,
            Func<StateInfo, long> GetHashCode,
            int startBlank = -1
            )
        {
            if (startBlank == -1)
            {
                for (int i = 0; i < board.Length; i++)
                {
                    if (board[i] == 0)
                    {
                        startBlank = i;
                        break;
                    }
                }
            }
            StateInfo state = new()
            {
                ParentIndex = ChunkedStructPool<StateInfo>.NoIndex,
                BlankPos = startBlank,
                BestG = 0,
                CurrentG = 0,
                PreviousMove = MoveDirection.None,
                BoardToken = arrayPool.GetToken(),
                CurrentH = GetHeuristics(heuristicsCalculator, board, gridSize)
            };

            state.CurrentF = CalculateF(state);
            state.Hash = GetHashCode(state);

            state.NodeIndex = stateInfoPool.Get(state, (ref state, source) =>
            {
                state = source;
            });
            board.CopyTo(state.BoardToken.AsSpan());
            return state;
        }

    }
}

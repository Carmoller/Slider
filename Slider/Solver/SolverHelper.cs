using Slider.Common;
using Slider.Common.Interfaces;
using Slider.Heuristics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            state.NodeIndex = stateInfoPool.Get(state, (ref state, ref source) =>
            {
                state = source;
            });
            board.CopyTo(state.BoardToken.AsSpan());
            return state;
        }
        public static Move GetMove(StateInfo goal, StateInfo start, int gridSize)
        {
            (int fromRow, int fromCol) = Math.DivRem(start.BlankPos, gridSize);
            (int toRow, int toCol) = Math.DivRem(goal.BlankPos, gridSize);
            return new Move
            {
                FromRow = fromRow,
                ToRow = toRow,
                FromColumn = fromCol,
                ToColumn = toCol,
                NodeIndex = start.NodeIndex,
                CurrentH = start.CurrentH,
            };
        }

        public static List<Move> ReconstructPath(StateInfo goalState, ChunkedStructPool<StateInfo> stateInfoPool, int gridSize)
        {
            List<Move> moves = [];
            int nodeIndex = goalState.NodeIndex;
            while (nodeIndex != -1)
            {
                ref StateInfo current = ref stateInfoPool.GetRef(nodeIndex);
                if (current.ParentIndex == -1)
                {
                    moves.Reverse();
                    return moves;
                }
                ref StateInfo parent = ref stateInfoPool.GetRef(current.ParentIndex);
                moves.Add(GetMove(parent, current, gridSize));
                nodeIndex = parent.NodeIndex;
            }
            throw new InvalidOperationException("Shouldn't get here");
        }

        public static long GetHashCode(StateInfo state)
        {
            return StateHashes.FastHash(state.BoardToken.AsSpan());
        }

    }
}

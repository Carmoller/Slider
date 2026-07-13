using Slider.Common;
using Slider.Common.Interfaces;
using Slider.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Slider.Solver
{
    public class StateInfoFactory : IStateInfoFactory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref StateInfo GetNewState(
            byte newBlankPosition,
            MoveDirection direction,
            ref StateInfo currentState,
            IChunkedStructPool<StateInfo> stateInfoPool,
            IChunkedArrayPoolUnsafe arrayPool)
        {
            int nodeIndex = stateInfoPool.Get(currentState, (ref state, currentState) =>
            {
                state = currentState;
            });
            ref StateInfo newState = ref stateInfoPool.GetRef(nodeIndex);
            newState.BoardToken = arrayPool.GetToken();
            newState.BoardArrayIndex = newState.BoardToken.Index;
            currentState.BoardToken.AsSpan().CopyTo(newState.BoardToken.AsSpan());
            // Swap the tiles
            Span<byte> currentBoard = newState.BoardToken.AsSpan();
            currentBoard[currentState.BlankPos] = currentBoard[newBlankPosition];
            currentBoard[newBlankPosition] = 0;

            newState.CurrentG = currentState.CurrentG + 1;
            newState.BestG = currentState.CurrentG;
            newState.NodeIndex = nodeIndex;
            newState.ParentIndex = currentState.NodeIndex;
            newState.BlankPos = newBlankPosition;
            newState.PreviousMove = direction;
#if DIAGNOSE
            if (newState.BoardToken.AsSpan()[newState.BlankPos] != 0)
            {
                throw new InvalidOperationException("Invalid BlankPos");
            }
            if (newState.BoardToken.AsSpan().ToArray().Where(p => p == 0).Count() > 1)
            {
                throw new InvalidOperationException("More than one blank!");
            }
#endif
            return ref newState;
        }

        public void GetAvailableMoves(ref StateInfo currentState,
            int gridSize,
            IChunkedStructPool<StateInfo> stateInfoPool,
            IChunkedArrayPoolUnsafe arrayPool,
            RefAction<StateInfo> processState)
        {
            int blankRow = currentState.BlankPos / gridSize;
            int blankCol = currentState.BlankPos % gridSize;

            if ((currentState.PreviousMove != MoveDirection.Down) && (blankRow != 0))
            {
                processState(ref GetNewState((byte)(currentState.BlankPos - gridSize), MoveDirection.Up, ref currentState, stateInfoPool, arrayPool));
            }
            if ((currentState.PreviousMove != MoveDirection.Up) && (blankRow != gridSize - 1))
            {
                processState(ref GetNewState((byte)(currentState.BlankPos + gridSize), MoveDirection.Down, ref currentState, stateInfoPool, arrayPool));
            }
            if ((currentState.PreviousMove != MoveDirection.Left) && (blankCol != gridSize - 1))
            {
                processState(ref GetNewState((byte)(currentState.BlankPos + 1), MoveDirection.Right, ref currentState, stateInfoPool, arrayPool));
            }
            if ((currentState.PreviousMove != MoveDirection.Right) && (blankCol != 0))
            {
                processState(ref GetNewState((byte)(currentState.BlankPos - 1), MoveDirection.Left, ref currentState, stateInfoPool, arrayPool));
            }
        }
    }
}

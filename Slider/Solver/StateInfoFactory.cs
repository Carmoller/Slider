using Slider.Common;
using Slider.Common.Interfaces;
using Slider.Interfaces;
using System;
using System.Collections.Generic;
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
            IChunkedArrayPool<byte> arrayPool)
        {
            int nodeIndex = stateInfoPool.Get(currentState, (ref StateInfo state, StateInfo currentState) =>
            {
                state = currentState;
            });
            ref StateInfo newState = ref stateInfoPool.GetRef(nodeIndex);
            newState.BoardArrayIndex = arrayPool.Get();
            newState.Board = arrayPool.GetArray(newState.BoardArrayIndex);
            currentState.Board.CopyTo(newState.Board);

            // Swap the tiles
            newState.Board[currentState.BlankPos] = currentState.Board[newBlankPosition];
            newState.Board[newBlankPosition] = 0;

            newState.CurrentG = currentState.CurrentG + 1;
            newState.BestG = currentState.CurrentG;
            newState.NodeIndex = nodeIndex;
            newState.ParentIndex = currentState.NodeIndex;
            newState.BlankPos = newBlankPosition;
            newState.PreviousMove = direction;
            return ref newState;
        }

        public void GetAvailableMoves(StateInfo currentState, 
            int gridSize, 
            IChunkedStructPool<StateInfo> stateInfoPool,
            IChunkedArrayPool<byte> arrayPool,
            RefAction<StateInfo> processState)
        {
            int blankRow = currentState.BlankPos / gridSize;
            int blankCol = currentState.BlankPos % gridSize;

            if ((currentState.PreviousMove != MoveDirection.Down) && (blankRow != 0))
            {
                processState(ref GetNewState((byte)(currentState.BlankPos - gridSize), MoveDirection.Up, ref currentState, stateInfoPool, arrayPool));
            }
            if ((currentState.PreviousMove != MoveDirection.Up) && (blankRow != gridSize-1))
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

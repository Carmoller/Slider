using Slider.Common;
using Slider.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Slider.Solver
{
    public class GreedyBfsSolver
    {
        private PriorityQueue<StateInfo, double> _openQueue = new();
        private SolveStateDictionary<StateInfo> _closed = new();
        private int _gridSize;
        private ChunkedObjectPool<StateInfo>? _stateInfoPool;
        private IHeuristicCalculator? _heuristicsCalculator;
        private const double H_Scale = 1.2;
        private int _min_h;
        private int _startNodeIndex;

        public List<Move>? SprintSolve(StateInfo startState, 
            ChunkedObjectPool<StateInfo> stateInfoPool,
            IHeuristicCalculator heuristicsCalculator,
            int gridSize, 
            int maxNodes = 5000)
        {
            _gridSize = gridSize;
            _stateInfoPool = stateInfoPool;
            _heuristicsCalculator = heuristicsCalculator;
            _startNodeIndex = startState.NodeIndex;
            _openQueue.Enqueue(startState, startState.CurrentF);
            int nodesExplored = 0;
            _min_h = startState.CurrentH;
            while (_openQueue.TryDequeue(out StateInfo currentState, out double f_current) && nodesExplored < maxNodes)
            {
                bool found = _closed.TryGetState(currentState.Hash, currentState, out StateInfo closedState);
                if (found)
                {
                    if (closedState.BestG <= currentState.CurrentG)
                    {
                        _stateInfoPool.Release(currentState.NodeIndex);
                        continue;
                    }
                    closedState.ParentIndex = currentState.NodeIndex;
                    closedState.BestG = currentState.CurrentG;
                }

                if (currentState.CurrentH < _min_h)
                {
                    Debug.WriteLine($"Greedy BFS: State #{nodesExplored}: h:{currentState.CurrentH}");
                    _min_h = currentState.CurrentH;
                }
                nodesExplored++;
                if (currentState.CurrentH == 0)
                {
                    return ReconstructPath(currentState);

                }

                if (!found)
                    _closed.AddState(currentState.Hash, currentState);

                HandleNewState(currentState, MoveUp(currentState));
                HandleNewState(currentState, MoveDown(currentState));
                HandleNewState(currentState, MoveLeft(currentState));
                HandleNewState(currentState, MoveRight(currentState));

            }
            return null;
        }

        private List<Move> ReconstructPath(StateInfo goalState)
        {
            List<Move> moves = new();
            int nodeIndex = goalState.NodeIndex;
            while (nodeIndex != -1)
            {
                ref StateInfo current = ref _stateInfoPool.GetRef(nodeIndex);
                if (current.NodeIndex == _startNodeIndex)
                {
                    Debug.WriteLine($"Greedy BFS: Used {moves.Count} moves");
                    if (moves.Count > 100)
                    {
                        int a = 1;
                    }
                }
                if (current.ParentIndex == -1)
                {
                    moves.Reverse();
  //                  Cleanup();
                    return moves;
                }

                ref StateInfo parent = ref _stateInfoPool.GetRef(current.ParentIndex);
                moves.Add(GetMove(parent, current));
                nodeIndex = parent.NodeIndex;
            }
            throw new InvalidOperationException("Should get here");
        }

        private Move GetMove(StateInfo goal, StateInfo start)
        {
            return new Move
            {
                FromRow = start.BlankPos / _gridSize,
                ToRow = goal.BlankPos / _gridSize,
                FromColumn = start.BlankPos % _gridSize,
                ToColumn = goal.BlankPos % _gridSize
            };
        }

        private long GetHashCode(StateInfo state)
        {
            return StateHashes.FastHash(state.Board);
        }

        private void HandleNewState(StateInfo currentState, int newStateIndex)
        {
            if (newStateIndex == ChunkedObjectPool<StateInfo>.NoIndex)
                return;
            ref StateInfo newState = ref _stateInfoPool.GetRef(newStateIndex);
            int tentative_g = currentState.CurrentG + 1;
            newState.Hash = GetHashCode(newState);
            if (_closed.TryGetState(newState.Hash, newState, out StateInfo closedNeighbor))
            {
                if (closedNeighbor.CurrentG <= tentative_g)
                {
                    _stateInfoPool.Release(newState.NodeIndex);
                    return;
                }
            }
            newState.CurrentG = tentative_g;
            newState.BestG = int.MaxValue;
            newState.CurrentH = GetHeuristics(newState.Board, _gridSize);
            newState.CurrentF = newState.CurrentG + (newState.CurrentH);
            if (newState.BestG > currentState.CurrentG)
            {
                newState.BestG = currentState.CurrentG;
            }
            double priority = (newState.CurrentH * H_Scale + currentState.CurrentG) - (currentState.CurrentG * 0.0001);
            _openQueue.Enqueue(newState, priority);

        }
        private int GetHeuristics(byte[] board, int gridSize)
        {
            return _heuristicsCalculator.GetHeuristic(board, gridSize);
        }

        private void SwapTiles(byte[] board, int tile1, int tile2)
        {
            byte temp = board[tile2];
            board[tile2] = board[tile1];
            board[tile1] = temp;
        }

        private int GetNewState(byte newBlankPosition, MoveDirection direction, StateInfo currentState)
        {
            int nodeIndex = _stateInfoPool.Get(currentState, (ref StateInfo state, StateInfo currentState) =>
            {
                state = currentState;
            });
            ref StateInfo newState = ref _stateInfoPool.GetRef(nodeIndex);
            newState.Board = (byte[])currentState.Board.Clone();
            SwapTiles(newState.Board, currentState.BlankPos, newBlankPosition);
            newState.NodeIndex = nodeIndex;
            newState.ParentIndex = currentState.NodeIndex;
            newState.BlankPos = newBlankPosition;
            newState.PreviousMove = direction;
            return nodeIndex;
        }

        private int MoveUp(StateInfo state)
        {
            if (state.PreviousMove == MoveDirection.Down)
                return ChunkedObjectPool<StateInfo>.NoIndex;
            int blankRow = state.BlankPos / _gridSize;
            if (blankRow == 0)
                return ChunkedObjectPool<StateInfo>.NoIndex;
            byte newBlank = (byte)(state.BlankPos - _gridSize);
            return GetNewState(newBlank, MoveDirection.Up, state);
        }
        private int MoveDown(StateInfo state)
        {
            if (state.PreviousMove == MoveDirection.Up)
                return ChunkedObjectPool<StateInfo>.NoIndex;
            int blankRow = state.BlankPos / _gridSize;
            if (blankRow == _gridSize - 1)
                return ChunkedObjectPool<StateInfo>.NoIndex;
            byte newBlank = (byte)(state.BlankPos + _gridSize);
            return GetNewState(newBlank, MoveDirection.Down, state);
        }
        private int MoveLeft(StateInfo state)
        {
            if (state.PreviousMove == MoveDirection.Right)
                return ChunkedObjectPool<StateInfo>.NoIndex;
            int blankCol = state.BlankPos % _gridSize;
            if (blankCol == 0)
                return ChunkedObjectPool<StateInfo>.NoIndex;
            byte newBlank = (byte)(state.BlankPos - 1);
            return GetNewState(newBlank, MoveDirection.Left, state);
        }
        private int MoveRight(StateInfo state)
        {
            if (state.PreviousMove == MoveDirection.Left)
                return ChunkedObjectPool<StateInfo>.NoIndex;
            int blankCol = state.BlankPos % _gridSize;
            if (blankCol == _gridSize - 1)
                return ChunkedObjectPool<StateInfo>.NoIndex;
            byte newBlank = (byte)(state.BlankPos + 1);
            return GetNewState(newBlank, MoveDirection.Right, state);
        }

    }
}

using Slider.Common;
using Slider.Common.Interfaces;
using Slider.Interfaces;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Slider.Solver
{
    public class GreedyBfsSolver
    {
        private readonly PriorityQueue<StateInfo, double> _openQueue = new();
        private readonly SolveStateDictionary<StateInfo> _closed = new();
        private int _gridSize;
        private ChunkedStructPool<StateInfo>? _stateInfoPool;
        private ChunkedArrayPool<byte>? _arrayPool;
        private IHeuristicCalculator? _heuristicsCalculator;
        private const double H_Scale = 1.2;
        private int _min_h;
        private int _startNodeIndex;
        private IOptions _options;
        private IStateInfoFactory _stateInfoFactory;

        public GreedyBfsSolver(IOptions options, IStateInfoFactory stateInfoFactory)
        {
            _options = options;
            _stateInfoFactory = stateInfoFactory;
        }

        public List<Move>? SprintSolve(
            SolveResult result,
            StateInfo startState, 
            ChunkedStructPool<StateInfo> stateInfoPool,
            ChunkedArrayPool<byte> arrayPool,
            IHeuristicCalculator heuristicsCalculator,
            int gridSize, 
            int maxNodes = 5000)
        {
            _gridSize = gridSize;
            _stateInfoPool = stateInfoPool;
            _arrayPool = arrayPool;
            _heuristicsCalculator = heuristicsCalculator;
            _startNodeIndex = startState.NodeIndex;
            _openQueue.Enqueue(startState, startState.CurrentF);
            int nodesExplored = 0;
            _min_h = startState.CurrentH;
            while (_openQueue.TryDequeue(out StateInfo currentState, out double _) && nodesExplored < maxNodes)
            {
                bool found = _closed.TryGetState(currentState.Hash, currentState, out StateInfo closedState);
                if (found)
                {
                    if (closedState.BestG <= currentState.CurrentG)
                    {
                        _stateInfoPool.Release(currentState.NodeIndex, (ref p) => { _arrayPool.Release(p.BoardArrayIndex);  });
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
                result.TotalStatesConsidered++;
                if (currentState.CurrentH == 0)
                {
                    return ReconstructPath(currentState);
                }

                if (!found)
                    _closed.AddState(currentState.Hash, currentState);

                _stateInfoFactory.GetAvailableMoves(currentState, _gridSize, _stateInfoPool, arrayPool, (ref p) => { HandleNewState(ref currentState, ref p); });
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

        private static long GetHashCode(StateInfo state)
        {
            return StateHashes.FastHash(state.Board);
        }

        private void HandleNewState(ref StateInfo currentState, ref StateInfo newState)
        {
            int tentative_g = currentState.CurrentG + 1;
            newState.Hash = GetHashCode(newState);
            if (_closed.TryGetState(newState.Hash, newState, out StateInfo closedNeighbor))
            {
                if (closedNeighbor.CurrentG <= tentative_g)
                {
                    _arrayPool.Release(closedNeighbor.BoardArrayIndex);
                    _stateInfoPool.Release(newState.NodeIndex, (ref p) => { _arrayPool.Release(p.BoardArrayIndex); });
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

    }
}

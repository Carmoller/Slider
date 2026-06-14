using Microsoft.Extensions.ObjectPool;
using Slider.Common;
using Slider.Heuristics;
using Slider.Interfaces;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Drawing.Text;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Windows.Documents;
using System.Xml.Linq;

namespace Slider.Solver
{
    public class WeightedAStarSolver : ISolver
    {
        const int F_Scale = 1000;
        const int G_Scale = 10;
        private double w = 2;

        private struct StateInfo
        {
            public int NodeIndex { get; set; }
            public int ParentIndex { get; set; }
            public byte[] Board { get; set; }
            public int BlankPos { get; set; }
            public long Hash { get; set; }
            public int BestG { get; set; }
            public int CurrentG { get; set; }
            public int CurrentH { get; set; }
            public double CurrentF { get; set; }
            public MoveDirection PreviousMove { get; set; }

            public override bool Equals(object? obj)
            {
                if (obj == null)
                    return false;
                StateInfo other = (StateInfo)obj;
                if (BlankPos != other.BlankPos) return false;
                return Enumerable.SequenceEqual(Board, other.Board);
            }
            public override int GetHashCode()
            {
                return (int)StateHashes.FastHash(Board);
            }
        }

        private PriorityQueue<StateInfo, double> _openQueue = new();
        private SolveStateDictionary<StateInfo> _closed = new();
        private int _gridSize;
        private IHeuristicCalculator? _heuristicCalculator;
        private long _discardedStates = 0;
        private ChunkedObjectPool<StateInfo>? _stateInfoPool;
        private IOptions _options;

        public WeightedAStarSolver(IOptions options)
        {
            _options = options;
        }
        public SolveResult Solve(List<BoardTile> board, SolverOptions solverOptions, IHeuristicElementFactory heuristicElementFactory)
        {
            int bestHValueIndex = -1;
            _stateInfoPool = new(1000000);
            SolveResult result = new();
            Stopwatch sw = Stopwatch.StartNew();
            _gridSize = (int)Math.Sqrt(board.Count);

            _heuristicCalculator = heuristicElementFactory.CreateHeuristicCalculator(_options, solverOptions, _gridSize);    
            byte[] startBoard = new byte[board.Count];
            byte startBlank = byte.MaxValue;
            foreach (BoardTile tile in board)
            {
                if (tile.Value == 0)
                    startBlank = (byte)(tile.Row * _gridSize + tile.Column);
                startBoard[tile.Row * _gridSize + tile.Column] = tile.Value;
            }

            StateInfo startState = new StateInfo
            {
                ParentIndex = ChunkedObjectPool<StateInfo>.NoIndex,
                BlankPos = startBlank,
                BestG = int.MaxValue,
                CurrentG = 0,
                PreviousMove = MoveDirection.None,
                Board = (byte[])startBoard.Clone()
            };
            startState.NodeIndex = _stateInfoPool.Get(startState, (ref StateInfo state, StateInfo bluePrint) =>
            {
                state = bluePrint;
            });
            startBoard.CopyTo(startState.Board);

            startState.CurrentH = GetHeuristics(startState.Board, _gridSize);
            startState.CurrentF = (w * startState.CurrentH);
            startState.Hash = GetHashCode(startState);
            _openQueue.Enqueue(startState, startState.CurrentF);
            int min_h = int.MaxValue;

            while (_openQueue.TryDequeue(out StateInfo currentState, out double f_current))
            {
                int h_Current = currentState.CurrentH;
                if (h_Current < min_h)
                {
                    bestHValueIndex = currentState.NodeIndex;
                    Debug.WriteLine($"{result.TotalStatesConsidered}: {h_Current}");
                    min_h = h_Current;
                }
                // Adjust w to avoid getting stuck at a low h-value, and refusing to climb back up the tree
                w = h_Current < 30 ? 1 : 2;
                if ((h_Current == 0) || (sw.Elapsed > _options.SolveTimeout))
                {
                    sw.Stop();
                    result.TimeSpent = sw.Elapsed;
                    if (h_Current != 0)
                    {
                        result.Result = SolveResultType.Timeout;
                        ref StateInfo bestState = ref _stateInfoPool.GetRef(bestHValueIndex);
                        result.Moves = ReconstructPath(bestState);

                    }
                    else
                    {
                        result.Result = SolveResultType.Solved;
                        result.Moves = ReconstructPath(currentState);
                    }
                    return result;
                }
                if (currentState.Hash == 0)
                {
                    throw new InvalidOperationException("Hash is 0");
                }
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
                result.TotalStatesConsidered++;
                if (!found)
                    _closed.AddState(currentState.Hash, currentState);
                HandleNewState(currentState, MoveUp(currentState));
                HandleNewState(currentState, MoveDown(currentState));
                HandleNewState(currentState, MoveLeft(currentState));
                HandleNewState(currentState, MoveRight(currentState));
            }
            result.Result = SolveResultType.Unsolvable;
            return result;
        }

        private void Cleanup()
        {
            _openQueue.Clear();

            _closed.Clear();

            _stateInfoPool = null;
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

        private List<Move> ReconstructPath(StateInfo goalState)
        {
            List<Move> moves = new();
            int nodeIndex = goalState.NodeIndex;
            while (nodeIndex != -1)
            {
                ref StateInfo current = ref _stateInfoPool.GetRef(nodeIndex);
                if (current.ParentIndex == -1)
                {
                    moves.Reverse();
                    Cleanup();
                    return moves;
                }

                ref StateInfo parent = ref _stateInfoPool.GetRef(current.ParentIndex);
                moves.Add(GetMove(parent, current));
                nodeIndex = parent.NodeIndex;
            }
            throw new InvalidOperationException("Should get here");
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
                    _discardedStates++;
                    _stateInfoPool.Release(newState.NodeIndex);
                    return;
                }
            }
            newState.CurrentG = tentative_g;
            newState.BestG = int.MaxValue;
            newState.CurrentH = GetHeuristics(newState.Board, _gridSize);
            newState.CurrentF = newState.CurrentG + (w * newState.CurrentH);
            if (newState.BestG > currentState.CurrentG)
            {
                newState.BestG = currentState.CurrentG;
            }
            double priority = newState.CurrentF * F_Scale - (-newState.CurrentG * G_Scale) + newState.CurrentH;
            _openQueue.Enqueue(newState, priority);

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

        private void SwapTiles(byte[] board, int tile1, int tile2)
        {
            byte temp = board[tile2];
            board[tile2] = board[tile1];
            board[tile1] = temp;
        }
        private int GetHeuristics(byte[] board, int gridSize)
        {
            if (_heuristicCalculator == null)
                throw new InvalidOperationException("_heursticCalculator has not been initialized");
            return GetHeuristics(board, gridSize, _heuristicCalculator);
        }
        private int GetHeuristics(byte[] board, int gridSize, IHeuristicCalculator customCalculator)
        {
            return customCalculator.GetHeuristic(board, gridSize);// HeuristicCalculator.ManhattanDistance(board, _gridSize);
        }
        //open = priority queue ordered by(f, tie - breakers)
        //closed = hash map: state → best g seen
        //g[start] = 0
        //h[start] = heuristic(start)
        //f[start] = g[start] + w * h[start]
        //push start into open
        //while open is not empty:
        //    current = pop node with smallest f
        //    if current is goal:
        //        return reconstruct_path(current)
        //    if current in closed and closed[current] ≤ g
        //            continue
        //    closed[current] = g[current]
        //    for each neighbor in expand(current):
        //        tentative_g = g[current] + 1
        //        if neighbor in closed and closed[neighbor] ≤ tentative_g:
        //        continue
        //        g[neighbor] = tentative_g
        //        h[neighbor] = heuristic(neighbor)
        //        f[neighbor] = g[neighbor] + w * h[neighbor]
        //        push neighbor into open with priority:
        //            (f[neighbor],
        //             -g[neighbor],        // prefer deeper nodes
        //             h[neighbor])         // secondary tie-break
        //return failure        


        public int GetHeuristic(List<BoardTile> board, IHeuristicElementFactory heuristicElementFactory)
        {
            byte[] byteBoard = board.OrderBy(p => p.Row).ThenBy(p => p.Column).Select(p => p.Value).ToArray();
            int gridSize = (int)(Math.Sqrt(byteBoard.Length));
            IHeuristicCalculator calculator = heuristicElementFactory.CreateHeuristicCalculator(null,
                new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true },
                gridSize);
            return GetHeuristics(byteBoard, gridSize, calculator);
        }

        private long GetHashCode(StateInfo state)
        {
            return StateHashes.FastHash(state.Board);
        }
    }
}

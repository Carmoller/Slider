using Microsoft.Extensions.ObjectPool;
using Slider.Common;
using Slider.Heuristics;
using Slider.Common.Interfaces;
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
using Slider.Interfaces;

namespace Slider.Solver
{
    public sealed class WeightedAStarSolver : IWeightedAStarSolver
    {
        const int F_Scale = 1000;
        const int G_Scale = 10;
        private const int H_CutoffForBfs = 10;

        private double w = 3;
        private double _initialW = 3;

        public double InitialW { get { return _initialW; } set { _initialW = value; w = value; } }
        private readonly PriorityQueue<int, double> _openQueue = new();
        private readonly SolveStateDictionary<StateInfo> _closed = [];
        private int _gridSize;
        private IHeuristicCalculator? _heuristicCalculator;
        private long _discardedStates = 0;
        private readonly IOptions _options;
        private readonly IStateInfoFactory _stateInfoFactory;

        public WeightedAStarSolver(IOptions options, IStateInfoFactory stateInfoFactory)
        {
            _options = options;
            _stateInfoFactory = stateInfoFactory;
        }

         private void Finalize(bool isTimedOut, 
            int bestHValueIndex, 
            SolveResult result,
            ChunkedStructPool<StateInfo> stateInfoPool,
            ref StateInfo currentState)
        {
            ref StateInfo bestState = ref currentState;
            if (isTimedOut)
            {
                result.Result = SolveResultType.Timeout;
                bestState = ref stateInfoPool.GetRef(bestHValueIndex);
            }
            else
            {
                result.Result = SolveResultType.Solved;
            }
            result.Moves = ReconstructPath(bestState, stateInfoPool);
            Cleanup();
        }

        public SolveResult Solve(List<BoardTile> board, byte[] targetBoard, ISolverOptions solverOptions, IHeuristicElementFactory heuristicElementFactory)
        {
            return Solve(SolverHelper.CreateStartBoard(board), targetBoard, solverOptions, heuristicElementFactory);
        }

        public SolveResult Solve(byte[] board, byte[] targetBoard, ISolverOptions solverOptions, IHeuristicElementFactory heuristicElementFactory)
        { 
            _gridSize = (int)Math.Sqrt(board.Length);
            int bestHValueIndex = -1;
            ChunkedStructPool<StateInfo> stateInfoPool = new(1000000);
            ChunkedArrayPoolUnsafe arrayPool = new ChunkedArrayPoolUnsafe(1000000, _gridSize * _gridSize);
            SolveResult result = new();
            Stopwatch sw = Stopwatch.StartNew();
            _heuristicCalculator = heuristicElementFactory.CreateHeuristicCalculator(SolverHelper.CreateGoalBoard(_gridSize), _gridSize, _options, solverOptions);

            StateInfo startState = SolverHelper.CreateStateInfoFromBoard(
                board, 
                arrayPool, 
                stateInfoPool, 
                _heuristicCalculator,
                _gridSize, 
                (StateInfo state) => { return w * state.CurrentF; },
                GetHeuristics, 
                GetHashCode);
#if DIAGNOSE
            bool alreadyFound = false;
            Span<byte> checkSpan = startState.BoardToken.AsSpan();
            for (int i = 0; i < checkSpan.Length; i++)
            {
                if (checkSpan[i] == 0)
                {
                    if (alreadyFound)
                    {
                        throw new InvalidOperationException("More than one blank!");
                    }
                    alreadyFound = true;
                }
            }
#endif

            _openQueue.Enqueue(startState.NodeIndex, startState.CurrentF);
            int min_h = int.MaxValue;
            int h_previous = int.MaxValue;
            bestHValueIndex = startState.NodeIndex;
            while (_openQueue.TryDequeue(out int nodeIndex, out double f_current))
            {
                StateInfo currentState = stateInfoPool.GetRef(nodeIndex);
                bool found = _closed.TryGetState(currentState.Hash, currentState, out StateInfo closedState);
                if (found)
                {
                    if (closedState.BestG <= currentState.CurrentG)
                    {
                        stateInfoPool.Release(currentState.NodeIndex, (ref p) => { arrayPool.Release(p.BoardArrayIndex); });
                        _discardedStates++;
                        continue;
                    }
                    closedState.BestG = currentState.CurrentG;
                }

#if DIAGNOSE
                Span<byte> checkSpan2 = currentState.BoardToken.AsSpan();
                bool alreadyFound2 = false;

                if (checkSpan2[currentState.BlankPos] != 0)
                {
                    throw new InvalidOperationException("Invalid BlankPos");
                }
                for (int i = 0; i < checkSpan2.Length; i++)
                {
                    if (checkSpan2[i] == 0)
                    {
                        if (alreadyFound2)
                        {
                            throw new InvalidOperationException("More than one blank!");
                        }
                        alreadyFound2 = true;
                    }
                }
#endif

                int h_Current = currentState.CurrentH;
                if (h_Current < min_h)
                {
                    bestHValueIndex = currentState.NodeIndex;
                    Debug.WriteLine($"Weighted A*: State #{result.TotalStatesConsidered}: h:{h_Current}");
                    min_h = h_Current;
                }
                if ((h_Current == 0) || (sw.Elapsed > _options.SolveTimeout && _options.SolveTimeout != TimeSpan.Zero))
                {
                    sw.Stop();
                    result.TimeSpent = sw.Elapsed;
                    Finalize(sw.Elapsed > _options.SolveTimeout, bestHValueIndex, result, stateInfoPool, ref currentState);
                    return result;
                }

                if (solverOptions.UseSprintFinish && ((h_Current < H_CutoffForBfs) && (h_Current >= h_previous)))
                { 
                    Debug.WriteLine($"Weighted A*: h is rising current: {h_Current}: previous:{h_previous}");
                    // We are below the cutoff threshold, and now the h is rising - time to pull the emergency cord and see if it works
                    BfsSolver solver = new(_options);
                    List<Move>? moves = solver.SprintSolve(result, currentState, stateInfoPool, arrayPool, _heuristicCalculator, _stateInfoFactory, _gridSize);
                    if (moves != null)
                    {
                        // Finished
                        sw.Stop();
                        result.TimeSpent = sw.Elapsed;
                        result.Moves = moves;
                        result.Result = SolveResultType.Solved;
                        Cleanup();
                        return result;
                    }
                }
                if (!solverOptions.UseSprintFinish)
                {
                    // Adjust w to avoid getting stuck at a low h-value, and refusing to climb back up the tree
                   // w = h_Current < 30 ? 1 : InitialW;
                }
                h_previous = h_Current;
                result.TotalStatesConsidered++;
                if (!found)
                    _closed.AddState(currentState.Hash, currentState);
                _stateInfoFactory.GetAvailableMoves(currentState, _gridSize, stateInfoPool, arrayPool, (ref p) => { HandleNewState(ref currentState, ref p); });
            }
            result.Result = SolveResultType.Unsolvable;
            return result;
        }

        private void Cleanup()
        {
            _openQueue.Clear();
            _closed.Clear();
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

        private List<Move> ReconstructPath(StateInfo goalState, ChunkedStructPool<StateInfo> stateInfoPool)
        {
            List<Move> moves = [];
            int nodeIndex = goalState.NodeIndex;
            while (nodeIndex != -1)
            {
                ref StateInfo current = ref stateInfoPool.GetRef(nodeIndex);
                if (current.ParentIndex == -1)
                {
                    moves.Reverse();
                    Cleanup();
                    return moves;
                }

                ref StateInfo parent = ref stateInfoPool.GetRef(current.ParentIndex);
                moves.Add(GetMove(parent, current));
                nodeIndex = parent.NodeIndex;
            }
            throw new InvalidOperationException("Shouldn't get here");
        }

        private void HandleNewState(ref StateInfo currentState, ref StateInfo newState)
        {
            int tentative_g = currentState.CurrentG + 1;
            newState.Hash = GetHashCode(newState);
            newState.CurrentG = tentative_g;
            newState.BestG = int.MaxValue;
            newState.CurrentH = GetHeuristics(_heuristicCalculator, newState.BoardToken.AsSpan(), _gridSize);
            newState.CurrentF = newState.CurrentG + (w * newState.CurrentH);
            if (newState.BestG > currentState.CurrentG)
            {
                newState.BestG = currentState.CurrentG;
            }
            double priority = newState.CurrentF * F_Scale - (-newState.CurrentG * G_Scale) + newState.CurrentH;
            _openQueue.Enqueue(newState.NodeIndex, priority);
#if DIAGNOSE
            Span<byte> checkSpan = currentState.BoardToken.AsSpan();
            bool alreadyFound = false;
            if (checkSpan[currentState.BlankPos] != 0)
            {
                throw new InvalidOperationException("Invalid BlankPos");
            }
            for (int i = 0; i < checkSpan.Length; i++)
            {
                if (checkSpan[i] == 0)
                {
                    if (alreadyFound)
                    {
                        throw new InvalidOperationException("More than one blank!");
                    }
                    alreadyFound = true;
                }
            }
#endif
        }

        private static int GetHeuristics(IHeuristicCalculator customCalculator, Span<byte> board, int gridSize)
        {
            return customCalculator.GetHeuristic(board, gridSize);
        }
        public int GetHeuristic(List<BoardTile> board, IHeuristicElementFactory heuristicElementFactory)
        {
            byte[] byteBoard = board.OrderBy(p => p.Row).ThenBy(p => p.Column).Select(p => p.Value).ToArray();
            int gridSize = (int)(Math.Sqrt(byteBoard.Length));
            IHeuristicCalculator calculator = heuristicElementFactory.CreateHeuristicCalculator(SolverHelper.CreateGoalBoard(gridSize), gridSize, _options,
                new SolverOptions {UseManhattanDistance = true, UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true });
            return GetHeuristics(calculator, byteBoard, gridSize);
        }

        private static long GetHashCode(StateInfo state)
        {
            return StateHashes.FastHash(state.BoardToken.AsSpan());
        }
    }
}

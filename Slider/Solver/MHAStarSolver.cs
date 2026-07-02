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
    public class MHAStarSolver : ISolver
    {
        const int F_Scale = 1000;
        const int G_Scale = 10;
        private const int H_CutoffForBfs = 10;

        private double w = 3;
        private double _initialW = 3;

        public double InitialW { get { return _initialW; } set { _initialW = value; w = value; } }
        private PriorityQueue<int, double> _anchorQueue = new();
        private PriorityQueue<int, double> _scoutQueue = new();
        private SolveStateDictionary<StateInfo> _closed = new();
        private int _gridSize;
        private IHeuristicCalculator? _heuristicCalculator;
        private long _discardedStates = 0;
        private ChunkedStructPool<StateInfo>? _stateInfoPool;
        private ChunkedArrayPoolUnsafe? _arrayPool;
        private IOptions _options;
        private IStateInfoFactory _stateInfoFactory;

        public MHAStarSolver(IOptions options, IStateInfoFactory stateInfoFactory)
        {
            _options = options;
            _stateInfoFactory = stateInfoFactory;
        }

        private StateInfo CreateStartState(List<BoardTile> board)
        {
            byte[] startBoard = new byte[board.Count];
            byte startBlank = byte.MaxValue;
            foreach (BoardTile tile in board)
            {
                if (tile.Value == 0)
                    startBlank = (byte)(tile.Row * _gridSize + tile.Column);
                startBoard[tile.Row * _gridSize + tile.Column] = tile.Value;
            }

            startBoard.CopyTo(startBoard);

            StateInfo startState = new StateInfo
            {
                ParentIndex = ChunkedStructPool<StateInfo>.NoIndex,
                BlankPos = startBlank,
                BestG = 0,
                CurrentG = 0,
                PreviousMove = MoveDirection.None,
                BoardToken = _arrayPool.GetToken(),
                CurrentH = GetHeuristics(startBoard, _gridSize)
            };

            startState.CurrentF = (w * startState.CurrentH);
            startState.Hash = GetHashCode(startState);

            startState.NodeIndex = _stateInfoPool.Get(startState, (ref StateInfo state, StateInfo source) =>
            {
                state = source;
            });
            startBoard.CopyTo(startState.BoardToken.AsSpan());
            return startState;
        }

        private void Finalize(bool isTimedOut, int bestHValueIndex, SolveResult result, ref StateInfo currentState)
        {
            ref StateInfo bestState = ref currentState;
            if (isTimedOut)
            {
                result.Result = SolveResultType.Timeout;
                bestState = ref _stateInfoPool!.GetRef(bestHValueIndex);
            }
            else
            {
                result.Result = SolveResultType.Solved;
            }
            result.Moves = ReconstructPath(bestState);
            Cleanup();
        }

        public SolveResult Solve(List<BoardTile> board, ISolverOptions solverOptions, IHeuristicElementFactory heuristicElementFactory)
        {
            _gridSize = (int)Math.Sqrt(board.Count);
            int bestHValueIndex = -1;
            _stateInfoPool = new(1000000);
            _arrayPool = new ChunkedArrayPoolUnsafe(1000000, _gridSize * _gridSize);
            SolveResult result = new();
            Stopwatch sw = Stopwatch.StartNew();
            _heuristicCalculator = heuristicElementFactory.CreateHeuristicCalculator(_options, solverOptions, _gridSize);

            StateInfo startState = CreateStartState(board);
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

            _anchorQueue.Enqueue(startState.NodeIndex, startState.CurrentF);
            _scoutQueue.Enqueue(startState.NodeIndex, startState.CurrentF);
            int min_h = int.MaxValue;
            int h_previous = int.MaxValue;
            bestHValueIndex = startState.NodeIndex;
            int current = 0;

            while (true)
            {
                int nodeIndex = StateInfo.Empty.NodeIndex;
                double f_current;
                if (_anchorQueue.Count == 0 && _scoutQueue.Count == 0)
                    break;
                bool useAnchor = current++ % 4 == 0;
                if (useAnchor)
                {
                    while (true)
                    {
                        if (!_anchorQueue.TryDequeue(out nodeIndex, out f_current))
                        {
                            useAnchor = false; // When breaking, we should attempt to use the scout queue
                            break;
                        }
                        ref StateInfo testState = ref _stateInfoPool.GetRef(nodeIndex);
                        if (!_closed.TryGetState(testState.Hash, testState, out StateInfo _))
                            break;
                    }
                }
                if (!useAnchor)
                {
                    if (!_scoutQueue.TryDequeue(out nodeIndex, out f_current))
                        break;
                }
                StateInfo currentState = _stateInfoPool.GetRef(nodeIndex);

                bool found = _closed.TryGetState(currentState.Hash, currentState, out StateInfo closedState);
                if (found)
                {
                    if (closedState.BestG <= currentState.CurrentG)
                    {
                        _closed.AddState(currentState.Hash, currentState);
                        _discardedStates++;
                        continue;
                    }
                    else
                    {
                        _closed.ReplaceState(currentState.Hash, closedState, currentState);
                        _stateInfoPool.Release(closedState.NodeIndex, (ref StateInfo p) => { _arrayPool!.Release(p.BoardArrayIndex); });
                    }
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
                    Debug.WriteLine($"MHA* ({(useAnchor ? "Anchor" : "Scout")}): State #{result.TotalStatesConsidered}: h:{h_Current}");
                    min_h = h_Current;
                }
                if ((h_Current == 0) || (sw.Elapsed > _options.SolveTimeout))
                {
                    sw.Stop();
                    result.TimeSpent = sw.Elapsed;
                    Finalize(sw.Elapsed > _options.SolveTimeout, bestHValueIndex, result, ref currentState);
                    return result;
                }

                if (solverOptions.UseSprintFinish && ((h_Current < H_CutoffForBfs) && (h_Current >= h_previous)))
                { 
                    Debug.WriteLine($"Weighted A*: h is rising current: {h_Current}: previous:{h_previous}");
                    // We are below the cutoff threshold, and now the h is rising - time to pull the emergency cord and see if it works
                    GreedyBfsSolver solver = new(_options, _stateInfoFactory);
                    List<Move>? moves = solver.SprintSolve(result, currentState, _stateInfoPool, _arrayPool, _heuristicCalculator, _gridSize);
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
                // Adjust w to avoid getting stuck at a low h-value, and refusing to climb back up the tree
                w = /*h_Current < 30 ? 1 :*/ InitialW;

                h_previous = h_Current;
                result.TotalStatesConsidered++;
                if (!found)
                    _closed.AddState(currentState.Hash, currentState);
                _stateInfoFactory.GetAvailableMoves(currentState, _gridSize, _stateInfoPool, _arrayPool, (ref p) => { HandleNewState(ref currentState, ref p); });
            }
            result.Result = SolveResultType.Unsolvable;
            return result;
        }

        private double GetFValue(ref StateInfo state, bool isAnchor)
        {
            int heuristic = GetHeuristics(state.BoardToken.AsSpan(), _gridSize);
            state.CurrentH = heuristic;
            state.CurrentF = isAnchor ? state.CurrentG + heuristic : state.CurrentG + w * heuristic;
            return state.CurrentF;

        }
        private void Cleanup()
        {
            _anchorQueue.Clear();
            _scoutQueue.Clear();

            _closed.Clear();
            _stateInfoPool = null;
            _arrayPool = null;
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
            throw new InvalidOperationException("Shouldn't get here");
        }

        private void HandleNewState(ref StateInfo currentState, ref StateInfo newState)
        {
            int tentative_g = currentState.CurrentG + 1;
            newState.Hash = GetHashCode(newState);
            newState.CurrentG = tentative_g;
            newState.BestG = int.MaxValue;
            newState.CurrentH = GetHeuristics(newState.BoardToken.AsSpan(), _gridSize);
            newState.CurrentF = newState.CurrentG + (w * newState.CurrentH);
            if (newState.BestG > currentState.CurrentG)
            {
                newState.BestG = currentState.CurrentG;
            }
            //            double priority = newState.CurrentF * F_Scale - (-newState.CurrentG * G_Scale) + newState.CurrentH;
            int scoutStateIndex = _stateInfoPool.Get(newState, (ref StateInfo state, StateInfo source) =>
            {
                /*
                state.CurrentG = source.CurrentG;
                state.PreviousMove = source.PreviousMove;
                state.BlankPos = source.BlankPos;
                state.BoardToken = _arrayPool.GetToken();
                state.Hash = StateHashes.FastHash(state.BoardToken.AsSpan());
                source.BoardToken.AsSpan().CopyTo(state.BoardToken.AsSpan());
                state.CurrentF = GetFValue(ref state, true);*/
                state = source;
            });
            ref StateInfo scoutState = ref _stateInfoPool.GetRef(scoutStateIndex);
            scoutState.NodeIndex = scoutStateIndex;
            if (scoutStateIndex == 649 || scoutState.NodeIndex == 653)
            {
                Debug.WriteLine($"ScoutState: {scoutState.NodeIndex}");
            }
            if (newState.NodeIndex == scoutState.NodeIndex)
            {
                throw new InvalidOperationException("NewState = ScoutState");
            }
            _anchorQueue.Enqueue(newState.NodeIndex, GetFValue(ref newState, true));
            _scoutQueue.Enqueue(scoutStateIndex, GetFValue(ref scoutState, false));
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

        private int GetHeuristics(Span<byte> board, int gridSize)
        {
            if (_heuristicCalculator == null)
                throw new InvalidOperationException("_heursticCalculator has not been initialized");
            return GetHeuristics(board, gridSize, _heuristicCalculator);
        }
        private int GetHeuristics(Span<byte> board, int gridSize, IHeuristicCalculator customCalculator)
        {
            return customCalculator.GetHeuristic(board, gridSize);
        }
        public int GetHeuristic(List<BoardTile> board, IHeuristicElementFactory heuristicElementFactory)
        {
            byte[] byteBoard = board.OrderBy(p => p.Row).ThenBy(p => p.Column).Select(p => p.Value).ToArray();
            int gridSize = (int)(Math.Sqrt(byteBoard.Length));
            IHeuristicCalculator calculator = heuristicElementFactory.CreateHeuristicCalculator(_options,
                new SolverOptions {UseManhattanDistance = true, UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true },
                gridSize);
            return GetHeuristics(byteBoard, gridSize, calculator);
        }

        private long GetHashCode(StateInfo state)
        {
            return StateHashes.FastHash(state.BoardToken.AsSpan());
        }
    }
}

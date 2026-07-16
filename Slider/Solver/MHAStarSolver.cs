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
    public sealed class MHAStarSolver : ISolver
    {
        private struct MHAStarSolverContext
        {
            public required IChunkedStructPool<StateInfo> ObjectPool { get; set; }
            public required int CurrentStepIndex { get; set; }
        }

        const int F_Scale = 1000;
        const int G_Scale = 10;
        private const int H_CutoffForBfs = 10;

        private double w = 3;
        private double _initialW = 3;

        public double InitialW { get { return _initialW; } set { _initialW = value; w = value; } }
        private readonly PriorityQueue<int, double> _anchorQueue = new();
        private readonly PriorityQueue<int, double> _scoutQueue = new();
        private readonly SolveStateDictionary<StateInfo> _closed = [];
        private int _gridSize;
        private IHeuristicCalculator? _heuristicCalculator;
        private long _discardedStates = 0;
        private readonly IOptions _options;
        private readonly IStateInfoFactory _stateInfoFactory;
        private RefAction<StateInfo, MHAStarSolverContext> _cachedProcessNewStateHandler;

        public MHAStarSolver(IOptions options, IStateInfoFactory stateInfoFactory)
        {
            _options = options;
            _stateInfoFactory = stateInfoFactory;
            _cachedProcessNewStateHandler = ProcessNewState;
        }

        private StateInfo CreateStartState(ChunkedArrayPoolUnsafe arrayPool, ChunkedStructPool<StateInfo> stateInfoPool, Span<byte> board)
        {
            int startBlank = int.MaxValue;
            for (int i = 0; i < board.Length; i++)
            {
                if (board[i] == 0)
                {
                    startBlank = i;
                    break;
                }
            }

            StateInfo startState = new()
            {
                ParentIndex = ChunkedStructPool<StateInfo>.NoIndex,
                BlankPos = startBlank,
                BestG = 0,
                CurrentG = 0,
                PreviousMove = MoveDirection.None,
                BoardToken = arrayPool.GetToken(),
                CurrentH = GetHeuristics(board, _gridSize)
            };

            startState.CurrentF = (w * startState.CurrentH);
            startState.Hash = GetHashCode(startState);

            startState.NodeIndex = stateInfoPool.Get(startState, (ref state, source) =>
            {
                state = source;
            });
            board.CopyTo(startState.BoardToken.AsSpan());
            return startState;
        }

        private void Finalize(ChunkedStructPool<StateInfo> stateInfoPool, bool isTimedOut, int bestHValueIndex, SolveResult result, ref StateInfo currentState)
        {
            ref StateInfo bestState = ref currentState;
            if (isTimedOut)
            {
                result.Result = SolveResultType.Timeout;
                bestState = ref stateInfoPool!.GetRef(bestHValueIndex);
            }
            else
            {
                result.Result = SolveResultType.Solved;
            }
            result.Moves = ReconstructPath(stateInfoPool, bestState);
            Cleanup();
        }

        private Span<byte> GetGoalBoard(int gridSize)
        {
            byte[] goalBoard = new byte[gridSize * gridSize];
            for (int i = 1; i < goalBoard.Length; i++)
            {
                goalBoard[i - 1] = (byte)i;
            }
            return goalBoard;
        }
        public SolveResult Solve(Span<byte> board, Span<byte> targetBoard, ISolverOptions solverOptions, IHeuristicElementFactory heuristicElementFactory)
        {
            _gridSize = (int)Math.Sqrt(board.Length);
            if (targetBoard.Length == 0)
            {
                targetBoard = SolverHelper.CreateGoalBoard(_gridSize);
            }
            int bestHValueIndex = -1;
            ChunkedStructPool<StateInfo> stateInfoPool = new(1000000);
            ChunkedArrayPoolUnsafe arrayPool = new(1000000, _gridSize * _gridSize);
            SolveResult result = new();
            Stopwatch sw = Stopwatch.StartNew();
            _heuristicCalculator = heuristicElementFactory.CreateHeuristicCalculator(GetGoalBoard(_gridSize), _gridSize, _options, solverOptions);

            StateInfo startState = CreateStartState(arrayPool, stateInfoPool, board);
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
                        ref StateInfo testState = ref stateInfoPool.GetRef(nodeIndex);
                        if (!_closed.TryGetState(testState.Hash, testState, out StateInfo _))
                            break;
                    }
                }
                if (!useAnchor)
                {
                    if (!_scoutQueue.TryDequeue(out nodeIndex, out f_current))
                        break;
                }
                StateInfo currentState = stateInfoPool.GetRef(nodeIndex);

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
                        stateInfoPool.Release(closedState.NodeIndex, (ref p) => { arrayPool!.Release(p.BoardArrayIndex); });
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
                    Finalize(stateInfoPool, sw.Elapsed > _options.SolveTimeout, bestHValueIndex, result, ref currentState);
                    return result;
                }

                if (solverOptions.UseSprintFinish && ((h_Current < H_CutoffForBfs) && (h_Current >= h_previous)))
                { 
                    Debug.WriteLine($"Weighted A*: h is rising current: {h_Current}: previous:{h_previous}");
                    // We are below the cutoff threshold, and now the h is rising - time to pull the emergency cord and see if it works
                    BfsSolver solver = new(_options);
                    SolveResult sprintResult = solver.SprintSolve(currentState, stateInfoPool, arrayPool, _heuristicCalculator, _stateInfoFactory, sw, _gridSize);
                    result.TotalStatesConsidered += sprintResult.TotalStatesConsidered;
                    if (sprintResult.Result == SolveResultType.Solved)
                    {
                        // Finished
                        sw.Stop();
                        result.TimeSpent = sw.Elapsed;
                        result.Moves = sprintResult.Moves;
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

                MHAStarSolverContext context = new MHAStarSolverContext
                {
                    ObjectPool = stateInfoPool,
                    CurrentStepIndex = currentState.NodeIndex
                };

                _stateInfoFactory.GetAvailableMoves(ref currentState, _gridSize, stateInfoPool, arrayPool, ref context, _cachedProcessNewStateHandler!);

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

        private List<Move> ReconstructPath(ChunkedStructPool<StateInfo> stateInfoPool, StateInfo goalState)
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
        private void ProcessNewState(ref StateInfo newState, ref MHAStarSolverContext context)
        {
            ref StateInfo csRef = ref context.ObjectPool.GetRef(context.CurrentStepIndex);
            HandleNewState(context.ObjectPool, ref csRef, ref newState);
        }


        private void HandleNewState(IChunkedStructPool<StateInfo> stateInfoPool, ref StateInfo currentState, ref StateInfo newState)
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
            int scoutStateIndex = stateInfoPool.Get(newState, (ref state, source) =>
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
            ref StateInfo scoutState = ref stateInfoPool.GetRef(scoutStateIndex);
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
        private static int GetHeuristics(Span<byte> board, int gridSize, IHeuristicCalculator customCalculator)
        {
            return customCalculator.GetHeuristic(board, gridSize);
        }
        public int GetHeuristic(List<BoardTile> board, IHeuristicElementFactory heuristicElementFactory)
        {
            byte[] byteBoard = board.OrderBy(p => p.Row).ThenBy(p => p.Column).Select(p => p.Value).ToArray();
            int gridSize = (int)(Math.Sqrt(byteBoard.Length));
            IHeuristicCalculator calculator = heuristicElementFactory.CreateHeuristicCalculator(GetGoalBoard(gridSize), gridSize, _options,
                new SolverOptions {UseManhattanDistance = true, UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true });
            return GetHeuristics(byteBoard, gridSize, calculator);
        }

        private static long GetHashCode(StateInfo state)
        {
            return StateHashes.FastHash(state.BoardToken.AsSpan());
        }
    }
}

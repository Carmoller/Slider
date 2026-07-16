using Microsoft.Extensions.ObjectPool;
using Slider.Common;
using Slider.Common.Interfaces;
using Slider.Heuristics;
using Slider.Interfaces;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Media.Animation;

namespace Slider.Solver
{
    public sealed class BfsSolver : ISolver
    {
        private struct BfsSolverContext
        {
            public required IChunkedStructPool<StateInfo> ObjectPool { get; set; }
            public required IChunkedArrayPoolUnsafe ArrayPool { get; set; }
            public required IHeuristicCalculator Calculator { get; set; }
            public required int CurrentStepIndex { get; set; }
            public required MultiMap<StateInfo> Closed { get; set; }
            public required PriorityQueue<StateInfo, double> OpenQueue { get; set; }
        }

        private int _gridSize;
        private const double H_Scale = 1.2;
        private int _min_h;
        private int _startNodeIndex;
        private readonly IOptions _options;
        private RefAction<StateInfo, BfsSolverContext>? _cachedProcessNewStateHandler;

        public BfsMode BfsMode { get; set; } = BfsMode.Greedy;

        public BfsSolver(IOptions options)
        {
            _options = options;
            _cachedProcessNewStateHandler = ProcessNewState;
        }

        public SolveResult Solve(Span<byte> board, Span<byte> targetBoard, ISolverOptions solverOptions, IHeuristicElementFactory heuristicElementFactory)
        {
            _gridSize = (int)Math.Sqrt(board.Length);
            if (targetBoard.Length == 0)
                targetBoard = SolverHelper.CreateGoalBoard(_gridSize);
            IStateInfoFactory stateInfoFactory = new StateInfoFactory();
            ChunkedStructPool<StateInfo> stateInfoPool = new(1000000);
            using (ChunkedArrayPoolUnsafe arrayPool = new ChunkedArrayPoolUnsafe(1000000, _gridSize * _gridSize))
            {
                Stopwatch sw = Stopwatch.StartNew();
                IHeuristicCalculator heuristicCalculator = heuristicElementFactory.CreateHeuristicCalculator(targetBoard, _gridSize, _options, solverOptions);

                StateInfo startState = SolverHelper.CreateStateInfoFromBoard(
                    board,
                    arrayPool,
                    stateInfoPool,
                    heuristicCalculator,
                    _gridSize,
                    (StateInfo stateInfo) => { return stateInfo.CurrentG + stateInfo.CurrentH; },
                    GetHeuristics,
                    SolverHelper.GetHashCode);

                SolveResult result = SprintSolve(startState, stateInfoPool, arrayPool, heuristicCalculator, stateInfoFactory, sw, _gridSize, int.MaxValue);
                return result;
            }
        }

        public SolveResult SprintSolve(
            StateInfo startState,
            ChunkedStructPool<StateInfo> stateInfoPool,
            ChunkedArrayPoolUnsafe arrayPool,
            IHeuristicCalculator heuristicsCalculator,
            IStateInfoFactory stateInfoFactory,
            Stopwatch sw,
            int gridSize,
            int maxNodes = 5000)
        {
            TimeSpan timeout = _options.SolveTimeout;
            SolveResult result = new();
            MultiMap<StateInfo> closed = new(300000, 300000);
            PriorityQueue<StateInfo, double> openQueue = new();
            try
            {
                _gridSize = gridSize;
                _startNodeIndex = startState.NodeIndex;
                openQueue.Enqueue(startState, startState.CurrentF);
                _min_h = startState.CurrentH;
                while (openQueue.TryDequeue(out StateInfo currentState, out double _) && result.TotalStatesConsidered < maxNodes)
                {
                    StateInfo closedState = new();
                    bool found = closed.TryGetState(currentState.Hash, currentState, ref closedState);
                    if (found)
                    {
                        if (closedState.BestG <= currentState.CurrentG)
                        {
                            closed.AddState(currentState.Hash, currentState); // No reason to continue down this road, just mark it as closed
                            continue;
                        }
                    }

                    if (currentState.CurrentH < _min_h)
                    {
                        Debug.WriteLine($"{(BfsMode == BfsMode.Standard ? "Standard" : "Greedy")} BFS: State #{result.TotalStatesConsidered}: h:{currentState.CurrentH}");
                        _min_h = currentState.CurrentH;
                    }
                    result.TotalStatesConsidered++;
                    if (result.TotalStatesConsidered >= maxNodes)
                    {
                        Console.WriteLine($"Currently at {result.TotalStatesConsidered} examine nodes. Max is {maxNodes}");
                        result.Result = SolveResultType.StateLimitExceeded;
                        return result;
                    }

                    Span<byte> board = currentState.BoardToken.AsSpan();
                    if ((currentState.CurrentH == 0) ||  (timeout != TimeSpan.Zero && sw.Elapsed > timeout))
                    {
                        if (timeout != TimeSpan.Zero && sw.Elapsed > timeout)
                            result.Result = SolveResultType.Timeout;
                        else
                        {
                            result.Moves = SolverHelper.ReconstructPath(currentState, stateInfoPool, gridSize);
                            result.Result = SolveResultType.Solved;
                        }
                        return result;
                    }

                    if (!found)
                    {
                        closed.AddState(currentState.Hash, currentState);
                    }

                    BfsSolverContext context = new BfsSolverContext
                    {
                        ArrayPool = arrayPool,
                        ObjectPool = stateInfoPool,
                        Calculator = heuristicsCalculator,
                        CurrentStepIndex = currentState.NodeIndex,
                        OpenQueue = openQueue,
                        Closed = closed
                    };

                    stateInfoFactory.GetAvailableMoves(ref currentState, _gridSize, stateInfoPool, arrayPool, ref context, _cachedProcessNewStateHandler!);
                }
                result.Result = SolveResultType.Unsolvable;
                return result;
            }
            finally
            {
                closed.Clear();
                openQueue.Clear();
            }
        }

        private void ProcessNewState(ref StateInfo newState, ref BfsSolverContext context)
        {
            ref StateInfo csRef = ref context.ObjectPool.GetRef(context.CurrentStepIndex);
            int tentative_g = csRef.CurrentG + 1;
            newState.Hash = SolverHelper.GetHashCode(newState);
            StateInfo closedNeighbor = new();
            if (context.Closed.TryGetState(newState.Hash, newState, ref closedNeighbor))
            {
                if (closedNeighbor.BoardToken.AsSpan().ToArray().Where(p => p == 0).Count() > 1)
                {
                    throw new InvalidOperationException("Multiple blank tiles on board");
                }
                if (closedNeighbor.CurrentG > tentative_g)
                {
                    context.ArrayPool.Release(newState.BoardArrayIndex);
                    context.ObjectPool.Release(newState.NodeIndex, null);
                    return;
                }
            }
            newState.CurrentG = tentative_g;
            newState.BestG = int.MaxValue;
            newState.CurrentH = GetHeuristics(context.Calculator, newState.BoardToken.AsSpan(), _gridSize);
            newState.CurrentF = newState.CurrentG + newState.CurrentH;
            if (newState.BestG > csRef.CurrentG)
            {
                newState.BestG = csRef.CurrentG;
            }
            double priority = (newState.CurrentH * H_Scale + csRef.CurrentG) - (csRef.CurrentG * 0.0001);
            context.OpenQueue.Enqueue(newState, BfsMode == BfsMode.Greedy ? priority : newState.CurrentG);
        }

        public int GetHeuristic(List<BoardTile> board, IHeuristicElementFactory heuristicElementFactory)
        {
            byte[] byteBoard = board.OrderBy(p => p.Row).ThenBy(p => p.Column).Select(p => p.Value).ToArray();
            int gridSize = (int)(Math.Sqrt(byteBoard.Length));
            IHeuristicCalculator calculator = heuristicElementFactory.CreateHeuristicCalculator(SolverHelper.CreateGoalBoard(gridSize), gridSize, _options,
                new SolverOptions { UseManhattanDistance = true, UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true });
            return GetHeuristics(calculator, byteBoard, gridSize);
        }
        private static int GetHeuristics(IHeuristicCalculator heuristicsCalculator, Span<byte> board, int gridSize)
        {
            return heuristicsCalculator.GetHeuristic(board, gridSize);
        }

    }
}

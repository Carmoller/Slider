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
        }
        public int MaxG { get; set; } = int.MaxValue;

        private readonly PriorityQueue<StateInfo, double> _openQueue = new();
        private readonly SolveStateDictionary<StateInfo> _closed = [];
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
                IHeuristicCalculator heuristicCalculator = heuristicElementFactory.CreateHeuristicCalculator(targetBoard, _gridSize, _options, solverOptions);
                SolveResult result = new();

                StateInfo startState = SolverHelper.CreateStateInfoFromBoard(
                    board,
                    arrayPool,
                    stateInfoPool,
                    heuristicCalculator,
                    _gridSize,
                    (StateInfo stateInfo) => { return stateInfo.CurrentG + stateInfo.CurrentH; },
                    GetHeuristics,
                    SolverHelper.GetHashCode);

                List<Move>? moves = SprintSolve(result, startState, stateInfoPool, arrayPool, heuristicCalculator, stateInfoFactory, _gridSize, int.MaxValue);
                if (moves == null)
                    result.Result = SolveResultType.Timeout;
                else
                {
                    result.Result = SolveResultType.Solved;
                    result.Moves = moves;
                }
                return result;
            }
        }

        public List<Move>? SprintSolve(
            SolveResult result,
            StateInfo startState, 
            ChunkedStructPool<StateInfo> stateInfoPool,
            ChunkedArrayPoolUnsafe arrayPool,
            IHeuristicCalculator heuristicsCalculator,
            IStateInfoFactory stateInfoFactory,
            int gridSize, 
            int maxNodes = 5000)
        {
            try
            {
                _gridSize = gridSize;
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
                            _closed.AddState(currentState.Hash, currentState); // No reason to continue down this road, just mark it as closed
                            continue;
                        }
                    }

                    if (currentState.CurrentH < _min_h)
                    {
                        Debug.WriteLine($"{(BfsMode == BfsMode.Standard ? "Standard" : "Greedy")} BFS: State #{nodesExplored}: h:{currentState.CurrentH}");
                        _min_h = currentState.CurrentH;
                    }
                    if (currentState.CurrentG >= MaxG)
                        return null;
                    nodesExplored++;
                    result.TotalStatesConsidered++;

                    /* Code to check locked corner move needs. DELETE AFTER USE!!*/
                    Span<byte> board = currentState.BoardToken.AsSpan();

                    if (currentState.CurrentH == 0)
                    {
                        return SolverHelper.ReconstructPath(currentState, stateInfoPool, gridSize);
                    }

                    if (!found)
                        _closed.AddState(currentState.Hash, currentState);

                    BfsSolverContext context = new BfsSolverContext
                    {
                        ArrayPool = arrayPool,
                        ObjectPool = stateInfoPool,
                        Calculator = heuristicsCalculator,
                        CurrentStepIndex = currentState.NodeIndex
                    };

                    stateInfoFactory.GetAvailableMoves(ref currentState, _gridSize, stateInfoPool, arrayPool, ref context, _cachedProcessNewStateHandler!);
                }
                if (nodesExplored >= maxNodes)
                {
                    Console.WriteLine($"Currently at {nodesExplored} examine nodes. Max is {maxNodes}");
                }
                return null;
            }
            finally
            {
                _closed.Clear();
            }
        }

        private void ProcessNewState(ref StateInfo newState, ref BfsSolverContext context)
        {
            ref StateInfo csRef = ref context.ObjectPool.GetRef(context.CurrentStepIndex);
            HandleNewState(context.ArrayPool, context.ObjectPool, context.Calculator, ref csRef, ref newState);
        }


        private void HandleNewState(
            IChunkedArrayPoolUnsafe arrayPool, 
            IChunkedStructPool<StateInfo> stateInfoPool, 
            IHeuristicCalculator heuristicsCalculator,
            ref StateInfo currentState, 
            ref StateInfo newState)
        {
            int tentative_g = currentState.CurrentG + 1;
            newState.Hash = SolverHelper.GetHashCode(newState);
            if (_closed.TryGetState(newState.Hash, newState, out StateInfo closedNeighbor))
            {
                if (closedNeighbor.CurrentG > tentative_g)
                {
                    arrayPool.Release(closedNeighbor.BoardArrayIndex);
                    stateInfoPool.Release(newState.NodeIndex, (ref p) => { arrayPool.Release(p.BoardArrayIndex); });
                    return;
                }
            }
            newState.CurrentG = tentative_g;
            newState.BestG = int.MaxValue;
            newState.CurrentH = GetHeuristics(heuristicsCalculator, newState.BoardToken.AsSpan(), _gridSize);
            newState.CurrentF = newState.CurrentG + newState.CurrentH;
            if (newState.BestG > currentState.CurrentG)
            {
                newState.BestG = currentState.CurrentG;
            }
            double priority = (newState.CurrentH * H_Scale + currentState.CurrentG) - (currentState.CurrentG * 0.0001);
            _openQueue.Enqueue(newState, BfsMode == BfsMode.Greedy ? priority : newState.CurrentG);
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

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
        private readonly PriorityQueue<StateInfo, double> _openQueue = new();
        private readonly SolveStateDictionary<StateInfo> _closed = [];
        private int _gridSize;
        private const double H_Scale = 1.2;
        private int _min_h;
        private int _startNodeIndex;
        private readonly IOptions _options;
        public BfsMode BfsMode { get; set; } = BfsMode.Greedy;
        public BfsPurpose BfsPurpose { get; set; } = BfsPurpose.SolveBoard;

        public BfsSolver(IOptions options)
        {
            _options = options;
        }

        public SolveResult Solve(byte[] board, byte[] targetBoard, ISolverOptions solverOptions, IHeuristicElementFactory heuristicElementFactory)
        {
            _gridSize = (int)Math.Sqrt(board.Length);
            if (targetBoard.Length == 0)
                targetBoard = SolverHelper.CreateGoalBoard(_gridSize);
            IStateInfoFactory stateInfoFactory = new StateInfoFactory();
            ChunkedStructPool<StateInfo> stateInfoPool = new(1000000);
            ChunkedArrayPoolUnsafe arrayPool = new ChunkedArrayPoolUnsafe(1000000, _gridSize * _gridSize);
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
                GetHashCode);

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
                    Console.WriteLine($"{(BfsMode == BfsMode.Standard ? "Standard" : "Greedy")} BFS: State #{nodesExplored}: h:{currentState.CurrentH}");
                    _min_h = currentState.CurrentH;
                }
                nodesExplored++;
                result.TotalStatesConsidered++;

                /* Code to check locked corner move needs. DELETE AFTER USE!!*/
                Span<byte> board = currentState.BoardToken.AsSpan();

                if ((BfsPurpose == BfsPurpose.SolveTopRow && (board[0] == 1 && board[1] == 2 && board[2] == 3 && board[3] == 4))
                    || (BfsPurpose == BfsPurpose.SolveBoard && currentState.CurrentH == 0))
                {
                    return ReconstructPath(stateInfoPool, currentState);
                }

                if (!found)
                    _closed.AddState(currentState.Hash, currentState);

                stateInfoFactory.GetAvailableMoves(ref currentState, _gridSize, stateInfoPool, arrayPool, 
                    (ref p) => { 
                    HandleNewState(arrayPool, stateInfoPool, heuristicsCalculator, ref currentState, ref p); });
            }
            if (nodesExplored >= maxNodes)
            {
                Console.WriteLine($"Currently at {nodesExplored} examine nodes. Max is {maxNodes}");
            }
            return null;
        }

        private List<Move> ReconstructPath(ChunkedStructPool<StateInfo> stateInfoPool, StateInfo goalState)
        {
            List<Move> moves = [];
            int nodeIndex = goalState.NodeIndex;
            while (nodeIndex != -1)
            {
                ref StateInfo current = ref stateInfoPool.GetRef(nodeIndex);
                if (current.NodeIndex == _startNodeIndex)
                {
                    Console.WriteLine($"Greedy BFS: Used {moves.Count} moves");
                }
                if (current.ParentIndex == -1)
                {
                    moves.Reverse();
                    return moves;
                }

                ref StateInfo parent = ref stateInfoPool.GetRef(current.ParentIndex);
                moves.Add(GetMove(parent, current));
                nodeIndex = parent.NodeIndex;
            }
            throw new InvalidOperationException("Shouldn't get here");
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
            return StateHashes.FastHash(state.BoardToken.AsSpan());
        }

        private void HandleNewState(
            ChunkedArrayPoolUnsafe arrayPool, 
            ChunkedStructPool<StateInfo> stateInfoPool, 
            IHeuristicCalculator heuristicsCalculator,
            ref StateInfo currentState, 
            ref StateInfo newState)
        {
            int tentative_g = currentState.CurrentG + 1;
            newState.Hash = GetHashCode(newState);
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
            throw new NotImplementedException();
        }
        private static int GetHeuristics(IHeuristicCalculator heuristicsCalculator, Span<byte> board, int gridSize)
        {
            return heuristicsCalculator.GetHeuristic(board, gridSize);
        }

    }
}

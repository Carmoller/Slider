using Microsoft.Windows.Themes;
using Slider.Common;
using Slider.Common.Interfaces;
using Slider.Heuristics;
using Slider.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Controls;

namespace Slider.Solver
{
    public class BidirectionalAStarSolver : ISolver
    {
        private int _gridSize = 0;
        public IHeuristicCalculator? Calculator { get { return _heuristicsCalculator; } }
        private int _statesCalculatedCount { get; set; }
        private ChunkedArrayPoolUnsafe? _arrayPool;
        private ChunkedStructPool<StateInfo> _objectPool;
        private IHeuristicCalculator? _heuristicsCalculator;
        private IStateInfoFactory _StateInfoFactory;
        private IOptions _options;
        public BidirectionalAStarSolver(IOptions options, IStateInfoFactory StateInfoFactory)
        {
            _options = options;
            _StateInfoFactory = StateInfoFactory;
        }

        private void SetupBoardAndPositions(
            List<BoardTile> board,
            byte[] boardArray,
            byte[] goalBoard,
            byte[] startPositions,
            out int emptyPosition)
        {
            emptyPosition = 0;
            foreach (BoardTile tile in board)
            {
                int index = tile.Row * _gridSize + tile.Column;
                if (tile.Value == 0)
                {
                    emptyPosition = index;
                    continue;
                }
                boardArray[index] = tile.Value;
                startPositions[tile.Value] = (byte)index;
                goalBoard[tile.Value - 1] = (byte)(tile.Value);
            }
        }
        private void SetupBoardAndPositions(
            List<BoardTile> board,
            Span<byte> boardArray,
            Span<byte> goalBoard,
            byte[] startPositions,
            out int emptyPosition)
        {
            emptyPosition = 0;
            foreach (BoardTile tile in board)
            {
                int index = tile.Row * _gridSize + tile.Column;
                if (tile.Value == 0)
                {
                    emptyPosition = index;
                    continue;
                }
                boardArray[index] = tile.Value;
                startPositions[tile.Value] = (byte)index;
                goalBoard[tile.Value - 1] = (byte)(tile.Value);
            }
        }

        [MemberNotNull(nameof(_arrayPool))]
        public SolveResult Solve(List<BoardTile> board, ISolverOptions solverOptions, IHeuristicElementFactory heuristicElementFactory)
        {
            Stopwatch sw = Stopwatch.StartNew();
            _heuristicsCalculator = heuristicElementFactory.CreateHeuristicCalculator(_options, solverOptions, (int)Math.Sqrt(board.Count));

            _gridSize = (byte)Math.Sqrt(board.Count);
            _arrayPool = new(1000000, board.Count);
            _objectPool = new(1000000);

            PointerToken initialToken = _arrayPool.GetToken();
            PointerToken goalToken = _arrayPool.GetToken();

            byte[] startPositions = new byte[board.Count];
            SetupBoardAndPositions(board, initialToken.AsSpan(), goalToken.AsSpan(), startPositions, out int emptyPosition);

            // Check if already solved
            if (initialToken.AsSpan().SequenceCompareTo(goalToken.AsSpan()) == 0)
                return new() { Result = SolveResultType.AlreadySolved };

            // Bidirectional A*
            PriorityQueue<int, double> forwardOpen = new();
            SolveStateDictionary<StateInfo> forwardClosed = new();
            PriorityQueue<int, double> backwardOpen = new();
            SolveStateDictionary<StateInfo> backwardClosed = new();

            int initialH = GetHeuristic(initialToken.AsSpan(), _gridSize);
            int goalH = ReverseManhattanCalculator.Calculate(goalToken.AsSpan(), startPositions, _gridSize);

            StateInfo startState = new StateInfo { CurrentG = 0, CurrentH = initialH, BlankPos = emptyPosition };
            int startStateIndex = _objectPool.Get(startState, (ref StateInfo state, StateInfo source) =>
            {
                state.ParentIndex = -1;
                state.CurrentG = 0;
                state.CurrentH = initialH;
                state.BlankPos = emptyPosition;
                state.BoardToken = _arrayPool.GetToken();
                initialToken.AsSpan().CopyTo(state.BoardToken.AsSpan());
            });
            ref StateInfo startStateActual = ref _objectPool.GetRef(startStateIndex);
            startStateActual.NodeIndex = startStateIndex;

            StateInfo goalState = new StateInfo { CurrentG = 0, CurrentH = goalH, BlankPos = _gridSize * _gridSize - 1 };
            int goalStateIndex = _objectPool.Get(startState, (ref StateInfo state, StateInfo source) =>
            {
                state.ParentIndex = -1;
                state.CurrentG = 0;
                state.CurrentH = goalH;
                state.BlankPos = _gridSize * _gridSize - 1;
                state.BoardToken = _arrayPool.GetToken();
                goalToken.AsSpan().CopyTo(state.BoardToken.AsSpan());
            });
            ref StateInfo goalStateActual = ref _objectPool.GetRef(goalStateIndex);
            goalStateActual.NodeIndex = goalStateIndex;

            forwardOpen.Enqueue(startStateIndex, startState.CurrentF);
            backwardOpen.Enqueue(goalStateIndex, goalState.CurrentF);

            _statesCalculatedCount = 1;

            List<Move> moves = IteratePaths(forwardOpen, backwardOpen, forwardClosed, backwardClosed, startPositions);

            sw.Stop();
            return new(moves)
            {
                Result = SolveResultType.Solved,
                TimeSpent = TimeSpan.FromTicks(sw.ElapsedTicks),
                TotalStatesConsidered = _statesCalculatedCount,
                ForwardDictonarySize = forwardClosed.Count,
                BackwardDictonarySize = backwardClosed.Count,
                ForwardCollisionCount = forwardClosed.CollisionCount,
                BackwardCollisionCount = backwardClosed.CollisionCount,
                ForwardHitCount = forwardClosed.HitCount,
                BackwardHitCount = backwardClosed.HitCount,
                ForwardMaxListLength = forwardClosed.MaxLength,
                BackwardMaxListLength = backwardClosed.MaxLength
            };
        }

        private List<Move> IteratePaths(
            PriorityQueue<int, double> forwardOpen,
            PriorityQueue<int, double> backwardOpen,
            SolveStateDictionary<StateInfo> forwardClosed,
            SolveStateDictionary<StateInfo> backwardClosed,
            byte[] startPositions)
        {
            StateInfo forwardState = StateInfo.Empty;
            StateInfo backwardState = StateInfo.Empty;
            while (forwardOpen.Count > 0 && backwardOpen.Count > 0)
            {
                // Forward step
                bool forwardResult = StepSearch(forwardOpen, forwardClosed, backwardClosed, ref forwardState, ref backwardState, startPositions, true);
                if (forwardResult)
                {
                    return ReconstructPath(ref forwardState, ref backwardState);
                }

                // Backward step
                bool backwardResult = StepSearch(backwardOpen, backwardClosed, forwardClosed, ref forwardState, ref backwardState, startPositions, false);
                if (backwardResult)
                {
                    return ReconstructPath(ref forwardState, ref backwardState);
                }
            }
            return new();
        }

        private bool StepSearch(
            PriorityQueue<int, double> open,
            SolveStateDictionary<StateInfo> closed,
            SolveStateDictionary<StateInfo> oppositeClosed,
            ref StateInfo forwardState,
            ref StateInfo backwardState,
            byte[] startPositions,
            bool isForward)
        {
            if (open.Count == 0)
                return false;

            int currentIndex = open.Dequeue();
            StateInfo current = _objectPool.GetRef(currentIndex);
#if DIAGNOSE
            Debug.WriteLine($"{(isForward ? "Forward" : "Backward")}: {current.ToString()}");
#endif
            long stateHash = StateHashes.FastHash(current.BoardToken.AsSpan());

            // Check if already in closed set
            if (closed.Exists(stateHash, current))
            {
#if DIAGNOSE
                Debug.WriteLine("\tAlready visited");
#endif
                _objectPool.Release(currentIndex, (ref StateInfo p) => { _arrayPool!.Release(p.BoardArrayIndex); });
                return false;
            }

            // Check if this state was reached from opposite direction before adding to closed
            if (oppositeClosed.TryGetState(stateHash, current, out StateInfo oppositeState))
            {
#if DIAGNOSE
                Debug.WriteLine($"{(isForward ? "Forward" : "Backward")}. Found in oppositeClosed: {oppositeState.ToString()}");
#endif
                // Found meeting point!
                if (isForward)
                {
                    forwardState = current;
                    backwardState = oppositeState;
                    return true;
                }
                else
                {
                    forwardState = oppositeState;
                    backwardState = current;
                    return true;
                }
            }

            // Add current state to closed set
            closed.AddState(stateHash, current);

            _StateInfoFactory.GetAvailableMoves(current, _gridSize, _objectPool, _arrayPool!,
                (ref p) => { HandleNewState(ref current, ref p, open, isForward, startPositions); });
            return false;
        }

        private void HandleNewState(ref StateInfo currentState, ref StateInfo newState, PriorityQueue<int, double> open, bool isForward, byte[] startPositions)
        {
            int tentative_g = currentState.CurrentG + 1;
            newState.Hash = StateHashes.FastHash(newState.BoardToken.AsSpan());
            newState.CurrentG = tentative_g;
            newState.BestG = int.MaxValue;
            if (isForward)
            {
                newState.CurrentH = GetHeuristic(newState.BoardToken.AsSpan(), _gridSize);
            }
            else
            {
                newState.CurrentH = ReverseManhattanCalculator.Calculate(newState.BoardToken.AsSpan(), startPositions, _gridSize);
            }
            newState.CurrentF = newState.CurrentG + newState.CurrentH;
            if (newState.BestG > currentState.CurrentG)
            {
                newState.BestG = currentState.CurrentG;
            }
            double priority = newState.CurrentF + newState.CurrentG;
            open.Enqueue(newState.NodeIndex, priority);
#if DEBUG
            if (newState.BoardToken.AsSpan()[newState.BlankPos] != 0)
            {
                throw new InvalidOperationException("Invalid BlankPos");
            }
            if (newState.BoardToken.AsSpan().ToArray().Where(p => p == 0).Count() > 1)
            {
                throw new InvalidOperationException("More than one blank!");
            }
#endif
        }

        private List<Move> ReconstructPath(ref StateInfo forwardState, ref StateInfo backwardState)
        {
            List<Move> result = new();

            List<Move> forwardMoves = new();
            int forwardStateIndex = forwardState.NodeIndex;
            while (forwardStateIndex != -1)
            {
                ref StateInfo forwardStateIteration = ref _objectPool.GetRef(forwardStateIndex);

                forwardStateIndex = forwardStateIteration.ParentIndex;
                if (forwardStateIndex != -1)
                {
                    ref StateInfo parentIteration = ref _objectPool.GetRef(forwardStateIteration.ParentIndex);
                    forwardMoves.Add(new Move
                    {
                        FromRow = forwardStateIteration.BlankPos / _gridSize,
                        FromColumn = forwardStateIteration.BlankPos % _gridSize,
                        ToRow = parentIteration.BlankPos / _gridSize,
                        ToColumn = parentIteration.BlankPos % _gridSize
                    });
#if DIAGNOSE
                    Debug.WriteLine($"Forward: Moving from {forwardStateIteration.ToString()} to {parentIteration.ToString()}");
#endif
                }
            }
            forwardMoves.Reverse();
            result.AddRange(forwardMoves);

            List<Move> backwardMoves = new();
            int backwardStateIndex = backwardState.NodeIndex;
            while (backwardStateIndex != -1)
            {
                ref StateInfo backwardStateIteration = ref _objectPool.GetRef(backwardStateIndex);
                backwardStateIndex = backwardStateIteration.ParentIndex;
                if (backwardStateIndex != -1)
                {
                    ref StateInfo parentIteration = ref _objectPool.GetRef(backwardStateIteration.ParentIndex);
                    backwardMoves.Add(new Move
                    {
                        FromRow = parentIteration.BlankPos / _gridSize,
                        FromColumn = parentIteration.BlankPos % _gridSize,
                        ToRow = backwardStateIteration.BlankPos / _gridSize,
                        ToColumn = backwardStateIteration.BlankPos % _gridSize
                    });
#if DIAGNOSE
                    Debug.WriteLine(message: $"Backward: Moving from {backwardStateIteration.ToString()} to {parentIteration.ToString()}");
#endif
                }
            }
            result.AddRange(backwardMoves);
            return result;

        }
        public int GetHeuristic(List<BoardTile> board, IHeuristicElementFactory heuristicElementFactory)
        {
            int gridSize = (int)Math.Sqrt(board.Count);
            byte[] boardArray = new byte[board.Count];
            byte[] goalBoard = new byte[board.Count];
            byte[] startPositions = new byte[board.Count];
            SolverOptions solverOptions = new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true };
            _gridSize = (int)Math.Sqrt(board.Count);
            SetupBoardAndPositions(board, boardArray, goalBoard, startPositions, out int emptyPosition);

            _heuristicsCalculator = heuristicElementFactory.CreateHeuristicCalculator(_options, solverOptions, gridSize);
            return GetHeuristic(boardArray, gridSize);
        }

        private int GetHeuristic(byte[] board, int gridSize)
        {
            _statesCalculatedCount++;
            return _heuristicsCalculator!.GetHeuristic(board, gridSize);
        }
        private int GetHeuristic(Span<byte> board, int gridSize)
        {
            _statesCalculatedCount++;
            return _heuristicsCalculator!.GetHeuristic(board, gridSize);
        }
    }
}


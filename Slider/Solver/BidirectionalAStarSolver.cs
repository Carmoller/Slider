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
    public sealed class BidirectionalAStarSolver : ISolver
    {
        private struct BidirectionalContext
        {
            public int CurrentStepIndex;
            public required PriorityQueue<int, double> CurrentOpen;
            public bool CurrentIsForward;
            public required int[] CurrentStartPositions;
            public IChunkedStructPool<StateInfo> ObjectPool;
        }

        private int _gridSize = 0;
        public IHeuristicCalculator? Calculator { get { return _heuristicsCalculator; } }
        private int StatesCalculatedCount { get; set; }
        private IHeuristicCalculator? _heuristicsCalculator;
        private IHeuristicCalculator? _reverseCalculator;
        private readonly IStateInfoFactory _stateInfoFactory;
        private readonly IOptions _options;
        private int _minhForward = int.MaxValue;
        private int _minhBackward = int.MaxValue;
        private RefAction<StateInfo, BidirectionalContext>? _cachedProcessNewStateHandler;

        public BidirectionalAStarSolver(IOptions options, IStateInfoFactory StateInfoFactory)
        {
            _options = options;
            _stateInfoFactory = StateInfoFactory;
        }

        private void SetupBoardAndPositions(
            Span<byte> board,
            Span<byte> boardArray,
            Span<byte> goalBoard,
            int[] startPositions,
            out int emptyPosition)
        {
            emptyPosition = 0;
            for (int i=0; i<board.Length; i++)
            {
                if (board[i] == 0)
                {
                    emptyPosition = i;
                    continue;
                }
                boardArray[i] = board[i];
                startPositions[board[i]] = (byte)i;
                goalBoard[board[i]-1] = (byte)(board[i]);
            }
        }

        public SolveResult Solve(Span<byte> board, Span<byte> targetBoard, ISolverOptions solverOptions, IHeuristicElementFactory heuristicElementFactory)
        {
            int gridSize = (int)Math.Sqrt(board.Length);
            if (targetBoard.Length == 0)
            {
                targetBoard = SolverHelper.CreateGoalBoard(gridSize);
            }
            Stopwatch sw = Stopwatch.StartNew();

            _gridSize = (byte)Math.Sqrt(board.Length);
            using (ChunkedArrayPoolUnsafe arrayPool = new(1000000, board.Length))
            {
                ChunkedStructPool<StateInfo> objectPool = new(1000000);

                // Initialize the cached delegate once per solve
                _cachedProcessNewStateHandler = ProcessNewState;

                PointerToken initialToken = arrayPool.GetToken();
                PointerToken goalToken = arrayPool.GetToken();

                int[] startPositions = new int[board.Length];
                SetupBoardAndPositions(board, initialToken.AsSpan(), goalToken.AsSpan(), startPositions, out int emptyPosition);

                _heuristicsCalculator = heuristicElementFactory.CreateHeuristicCalculator(goalToken.AsSpan(), gridSize, _options, solverOptions);

                _reverseCalculator = heuristicElementFactory.CreateHeuristicCalculator(initialToken.AsSpan(), gridSize, _options,
                    new SolverOptions { UseManhattanDistance = true, UseLinearConflict = false, UseEdgePattern = false, UseCornerPattern = true, UsePdbs = false });

                // Check if already solved
                if (initialToken.AsSpan().SequenceCompareTo(goalToken.AsSpan()) == 0)
                    return new() { Result = SolveResultType.AlreadySolved };

                // Bidirectional A*
                PriorityQueue<int, double> forwardOpen = new();
                SolveStateDictionary<StateInfo> forwardClosed = [];
                PriorityQueue<int, double> backwardOpen = new();
                SolveStateDictionary<StateInfo> backwardClosed = [];

                int initialH = GetHeuristic(initialToken.AsSpan(), _gridSize);
                int goalH = _reverseCalculator.GetHeuristic(goalToken.AsSpan(), _gridSize);

                StateInfo startState = new() { CurrentG = 0, CurrentH = initialH, BlankPos = emptyPosition };
                int startStateIndex = objectPool.Get(startState, (ref state, source) =>
                {
                    state.ParentIndex = -1;
                    state.CurrentG = 0;
                    state.CurrentH = initialH;
                    state.BlankPos = emptyPosition;
                    state.BoardToken = arrayPool.GetToken();
                    initialToken.AsSpan().CopyTo(state.BoardToken.AsSpan());
                });
                ref StateInfo startStateActual = ref objectPool.GetRef(startStateIndex);
                startStateActual.NodeIndex = startStateIndex;

                StateInfo goalState = new() { CurrentG = 0, CurrentH = goalH, BlankPos = _gridSize * _gridSize - 1 };
                int goalStateIndex = objectPool.Get(startState, (ref state, source) =>
                {
                    state.ParentIndex = -1;
                    state.CurrentG = 0;
                    state.CurrentH = goalH;
                    state.BlankPos = _gridSize * _gridSize - 1;
                    state.BoardToken = arrayPool.GetToken();
                    goalToken.AsSpan().CopyTo(state.BoardToken.AsSpan());
                });
                ref StateInfo goalStateActual = ref objectPool.GetRef(goalStateIndex);
                goalStateActual.NodeIndex = goalStateIndex;

                forwardOpen.Enqueue(startStateIndex, startState.CurrentF);
                backwardOpen.Enqueue(goalStateIndex, goalState.CurrentF);

                StatesCalculatedCount = 1;

                List<Move> moves = IteratePaths(objectPool, arrayPool, forwardOpen, backwardOpen, forwardClosed, backwardClosed, startPositions);

                sw.Stop();
                return new(moves)
                {
                    Result = SolveResultType.Solved,
                    TimeSpent = TimeSpan.FromTicks(sw.ElapsedTicks),
                    TotalStatesConsidered = StatesCalculatedCount,
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
        }

        private List<Move> IteratePaths(
            IChunkedStructPool<StateInfo> objectPool,
            IChunkedArrayPoolUnsafe arrayPool,
            PriorityQueue<int, double> forwardOpen,
            PriorityQueue<int, double> backwardOpen,
            SolveStateDictionary<StateInfo> forwardClosed,
            SolveStateDictionary<StateInfo> backwardClosed,
            int[] startPositions)
        {
            StateInfo forwardState = StateInfo.Empty;
            StateInfo backwardState = StateInfo.Empty;
            while (forwardOpen.Count > 0 && backwardOpen.Count > 0)
            {
                // Forward step
                bool forwardResult = StepSearch(objectPool, arrayPool, forwardOpen, forwardClosed, backwardClosed, ref forwardState, ref backwardState, startPositions, true);
                if (forwardResult)
                {
                    return ReconstructPath(objectPool, ref forwardState, ref backwardState);
                }

                // Backward step
                bool backwardResult = StepSearch(objectPool, arrayPool, backwardOpen, backwardClosed, forwardClosed, ref forwardState, ref backwardState, startPositions, false);
                if (backwardResult)
                {
                    return ReconstructPath(objectPool, ref forwardState, ref backwardState);
                }
            }
            return new();
        }

        private bool StepSearch(
            IChunkedStructPool<StateInfo> objectPool,
            IChunkedArrayPoolUnsafe arrayPool,
            PriorityQueue<int, double> open,
            SolveStateDictionary<StateInfo> closed,
            SolveStateDictionary<StateInfo> oppositeClosed,
            ref StateInfo forwardState,
            ref StateInfo backwardState,
            int[] startPositions,
            bool isForward)
        {
            if (open.Count == 0)
                return false;

            int currentIndex = open.Dequeue();
            ref StateInfo currentState = ref objectPool.GetRef(currentIndex);
#if DIAGNOSE
            Debug.WriteLine($"{(isForward ? "Forward" : "Backward")}: {current.ToString()}");
#endif
            long stateHash = StateHashes.FastHash(currentState.BoardToken.AsSpan());

            //f Check if already in closed set
            if (closed.Exists(stateHash, currentState))
            {
#if DIAGNOSE
                Debug.WriteLine("\tAlready visited");
#endif
                objectPool.Release(currentIndex, (ref p) => { arrayPool.Release(p.BoardArrayIndex); });
                return false;
            }

            // Check if this state was reached from opposite direction before adding to closed
            if (oppositeClosed.TryGetState(stateHash, currentState, out StateInfo oppositeState))
            {
#if DIAGNOSE
                Debug.WriteLine($"{(isForward ? "Forward" : "Backward")}. Found in oppositeClosed: {oppositeState.ToString()}");
#endif
                // Found meeting point!
                if (isForward)
                {
                    forwardState = currentState;
                    backwardState = oppositeState;
                    return true;
                }
                else
                {
                    forwardState = oppositeState;
                    backwardState = currentState;
                    return true;
                }
            }

            // Add current state to closed set
            closed.AddState(stateHash, currentState);

            // Set up context for the cached handler
            BidirectionalContext context = new BidirectionalContext
            {
                CurrentStepIndex = currentIndex,
                CurrentOpen = open,
                CurrentStartPositions = startPositions,
                CurrentIsForward = isForward,
                ObjectPool = objectPool,
            };
            _stateInfoFactory.GetAvailableMoves(ref currentState, _gridSize, objectPool, arrayPool, ref context, _cachedProcessNewStateHandler!);
            return false;
        }

        private void ProcessNewState(ref StateInfo newState, ref BidirectionalContext context)
        {
            ref StateInfo csRef = ref context.ObjectPool.GetRef(context.CurrentStepIndex);
            HandleNewState(ref csRef, ref newState, context.CurrentOpen, context.CurrentIsForward, context.CurrentStartPositions);
        }

        private void HandleNewState(ref StateInfo currentState, ref StateInfo newState, PriorityQueue<int, double> open, bool isForward, int[] startPositions)
        {
            int tentative_g = currentState.CurrentG + 1;
            newState.Hash = StateHashes.FastHash(newState.BoardToken.AsSpan());
            newState.CurrentG = tentative_g;
            newState.BestG = int.MaxValue;
            if (isForward)
            {
                newState.CurrentH = GetHeuristic(newState.BoardToken.AsSpan(), _gridSize);
                if (newState.CurrentH < _minhForward)
                {
                    _minhForward = newState.CurrentH;
                    Console.WriteLine($"Forward: New min h: {_minhForward}, StateCount: {StatesCalculatedCount}");
                }
            }
            else
            {
                newState.CurrentH = _reverseCalculator!.GetHeuristic(newState.BoardToken.AsSpan(), _gridSize);
                if (newState.CurrentH < _minhBackward)
                {
                    _minhBackward = newState.CurrentH;
                    Console.WriteLine($"Backward: New  min h: {_minhBackward}, StateCount: {StatesCalculatedCount}");
                }
            }
            newState.CurrentF = newState.CurrentG + newState.CurrentH;
            if (newState.BestG > currentState.CurrentG)
            {
                newState.BestG = currentState.CurrentG;
            }
//            double priority = newState.CurrentF + newState.CurrentG;
            double priority = (newState.CurrentH * 1.2 + currentState.CurrentG) - (currentState.CurrentG * 0.0001);

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

        private List<Move> ReconstructPath(IChunkedStructPool<StateInfo> objectPool, ref StateInfo forwardState, ref StateInfo backwardState)
        {
            List<Move> result = [];

            List<Move> forwardMoves = [];
            int forwardStateIndex = forwardState.NodeIndex;
            while (forwardStateIndex != -1)
            {
                ref StateInfo forwardStateIteration = ref objectPool.GetRef(forwardStateIndex);

                forwardStateIndex = forwardStateIteration.ParentIndex;
                if (forwardStateIndex != -1)
                {
                    ref StateInfo parentIteration = ref objectPool.GetRef(forwardStateIteration.ParentIndex);
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
            Console.WriteLine($"ForwardMoves has {forwardMoves.Count} entries");
            forwardMoves.Reverse();
            result.AddRange(forwardMoves);

            List<Move> backwardMoves = [];
            int backwardStateIndex = backwardState.NodeIndex;
            while (backwardStateIndex != -1)
            {
                ref StateInfo backwardStateIteration = ref objectPool.GetRef(backwardStateIndex);
                backwardStateIndex = backwardStateIteration.ParentIndex;
                if (backwardStateIndex != -1)
                {
                    ref StateInfo parentIteration = ref objectPool.GetRef(backwardStateIteration.ParentIndex);
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
            Console.WriteLine($"BackwardMoves has {backwardMoves.Count} entries");

            result.AddRange(backwardMoves);
            return result;

        }
        public int GetHeuristic(List<BoardTile> board, IHeuristicElementFactory heuristicElementFactory)
        {
            int gridSize = (int)Math.Sqrt(board.Count);
            byte[] boardArray = new byte[board.Count];
            byte[] goalBoard = new byte[board.Count];
            int[] startPositions = new int[board.Count];
            SolverOptions solverOptions = new() { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true };
            _gridSize = (int)Math.Sqrt(board.Count);
            SetupBoardAndPositions(board.ToByteArray(), boardArray, goalBoard, startPositions, out int emptyPosition);

            _heuristicsCalculator = heuristicElementFactory.CreateHeuristicCalculator(goalBoard, gridSize, _options, solverOptions);
            return GetHeuristic(boardArray, gridSize);
        }
        private int GetHeuristic(Span<byte> board, int gridSize)
        {
            StatesCalculatedCount++;
            return _heuristicsCalculator!.GetHeuristic(board, gridSize);
        }
    }
}


using Microsoft.Windows.Themes;
using Slider.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Slider.Solver
{
    public class SolverIDAStarPlus : ISolver
    {
        private SolverOptions _solverOptions = new();
        private byte _gridSize = 0;
        public IHeuristicCalculator? Calculator { get { return _heuristicsCalculator; } }
        private int _statesCalculatedCount { get; set; }
        private ArrayPool2D? _arrayPool;
        private IHeuristicCalculator? _heuristicsCalculator;

        private void SetupBoardAndPositions(
            List<BoardTile> board,
            byte[,] boardArray,
            byte[,] goalBoard,
            (byte, byte)[] goalPositions,
            out byte emptyRow,
            out byte emptyColumn)
        {
            emptyRow = emptyColumn = 0;
            for (byte row = 0; row < _gridSize; row++)
            {
                for (byte col = 0; col < _gridSize; col++)
                {
                    // Set up initial board
                    byte index = (byte)(row * _gridSize + col);
                    boardArray[board[index].Row, board[index].Column] = (byte)board[index].Value;
                    if (board[index].Value == 0)
                    {
                        emptyRow = (byte)(board[index].Row);
                        emptyColumn = (byte)(board[index].Column);
                    }
                    // Set up goal board, and goal positions
                    if (row == _gridSize - 1 && col == _gridSize - 1)
                    {
                        goalBoard[row, col] = 0;
                        goalPositions[0] = (row, col);
                    }
                    else
                    {
                        goalBoard[row, col] = (byte)(index + 1);
                        goalPositions[index + 1] = (row, col);
                    }
                }
            }
        }

        public SolveResult Solve(List<BoardTile> board, SolverOptions options, IHeuristicElementFactory heuristicElementFactory)
        {
            Stopwatch sw = Stopwatch.StartNew();
            _solverOptions = options;
            _heuristicsCalculator = heuristicElementFactory.CreateHeuristicCalculator(_solverOptions, (int)Math.Sqrt(board.Count));

            _gridSize = (byte)Math.Sqrt(board.Count);
            (byte row, byte col)[] goalPositions = new (byte, byte)[_gridSize * _gridSize];
            byte[,] initialBoard = new byte[_gridSize, _gridSize];
            byte[,] goalBoard = new byte[_gridSize, _gridSize];
            byte initialEmptyRow = 0, initialEmptyCol = 0;
            _arrayPool = new();

            SetupBoardAndPositions(board, initialBoard, goalBoard, goalPositions, out initialEmptyRow, out initialEmptyCol);

            // Check if already solved
            if (BoardsEqual(initialBoard, goalBoard))
                return new() { Result = SolveResultType.AlreadySolved };

            // Bidirectional A*
            PriorityQueue<SolveState, long> forwardOpen = new();
            SolveStateDictionary forwardClosed = new();
            PriorityQueue<SolveState, long> backwardOpen = new();
            SolveStateDictionary backwardClosed = new();

            int initialH = GetHeuristic(initialBoard, goalPositions, _gridSize);
            int goalH = 0;

            SolveState startState = new(initialBoard, 0, initialH, initialEmptyRow, initialEmptyCol);
            SolveState goalState = new(goalBoard, 0, goalH, (byte)(_gridSize - 1), (byte)(_gridSize - 1)); // We know the empty should be at the bottom right corner

            forwardOpen.Enqueue(startState, startState.FCost);
            backwardOpen.Enqueue(goalState, goalState.FCost);

            _statesCalculatedCount = 1;

            List<Move> moves = IteratePaths(forwardOpen, backwardOpen, forwardClosed, backwardClosed, goalPositions);

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
            PriorityQueue<SolveState, long> forwardOpen,
            PriorityQueue<SolveState, long> backwardOpen,
            SolveStateDictionary forwardClosed,
            SolveStateDictionary backwardClosed,
            (byte row, byte col)[] goalPositions)
        {
            SolveState? forwardState = null;
            SolveState? backwardState = null;
            while (forwardOpen.Count > 0 && backwardOpen.Count > 0)
            {
                // Forward step
                bool forwardResult = StepSearch(forwardOpen, forwardClosed, backwardClosed, ref forwardState, ref backwardState, true, goalPositions);
                if (forwardResult)
                {
                    SolveState state = forwardState!;
                    while (true)
                    {
                        if (state.Parent == null || state.ParentMoveFromRow == -1 || state.ParentMoveFromCol == -1 || state.ParentMoveToRow == -1 || state.ParentMoveFromCol == -1)
                            break;
                        //Debug.WriteLine($"Forward step: Move tile from ({state.ParentMoveFromRow},{state.ParentMoveFromCol}) to ({state.ParentMoveToRow},{state.ParentMoveToCol})");
                        state = state.Parent!;
                    }
                    return ReconstructPath(forwardState!, backwardState!);
                }

                // Backward step
                bool backwardResult = StepSearch(backwardOpen, backwardClosed, forwardClosed, ref forwardState, ref backwardState, false, goalPositions);
                if (backwardResult)
                {
                    SolveState state = backwardState!;
                    while (true)
                    {
                        if (state.Parent == null || state.ParentMoveFromRow == -1 || state.ParentMoveFromCol == -1 || state.ParentMoveToRow == -1 || state.ParentMoveFromCol == -1)
                            break;
                        state = state.Parent!;
                    }
                    return ReconstructPath(forwardState!, backwardState!);
                }
            }
            return new();
        }

        private bool MoveTile(MoveDirection direction,
                              SolveState current,
                              PriorityQueue<SolveState, long> open,
                              Dictionary<long, List<SolveState>> closed,
                              (byte, byte)[] goalPositions)
        {
            // Rent scratch board from pool
            byte[,] scratchBoard = _arrayPool!.RentScratchBoard(_gridSize);
            byte newRow = current.EmptyRow, newCol = current.EmptyCol;
            try
            {
                switch (direction)
                {
                    case MoveDirection.Left:
                        if (current.Parent != null && current.MoveDirectionFromParent == MoveDirection.Right)
                            return false; // Don't immediately reverse the previous move
                        newCol--;
                        break;
                    case MoveDirection.Right:
                        if (current.Parent != null && current.MoveDirectionFromParent == MoveDirection.Left)
                            return false; // Don't immediately reverse the previous move
                        newCol++;
                        break;
                    case MoveDirection.Up:
                        if (current.Parent != null && current.MoveDirectionFromParent == MoveDirection.Down)
                            return false; // Don't immediately reverse the previous move
                        newRow--;
                        break;
                    case MoveDirection.Down:
                        if (current.Parent != null && current.MoveDirectionFromParent == MoveDirection.Up)
                            return false; // Don't immediately reverse the previous move
                        newRow++;
                        break;

                }
                // Generate neighbors
                if (newRow < 0 || newRow >= _gridSize || newCol < 0 || newCol >= _gridSize)
                    return false;

                // Copy current board to scratch for validation
                System.Array.Copy(current.Board, scratchBoard, current.Board.Length);

                // Perform swap on scratch board
                (scratchBoard[current.EmptyRow, current.EmptyCol], scratchBoard[newRow, newCol]) =
                    (scratchBoard[newRow, newCol], scratchBoard[current.EmptyRow, current.EmptyCol]);

                long newHash = FastHash(scratchBoard);

                // Check if new state is already in closed set
                bool inClosed = false;
                if (closed.TryGetValue(newHash, out List<SolveState>? newClosedStates))
                {
                    foreach (SolveState closedState in newClosedStates)
                    {
                        if (closedState.BoardEquals(scratchBoard))
                        {
                            inClosed = true;
                            break;
                        }
                    }
                }
                if (inClosed)
                    return false;

                int newH = GetHeuristic(scratchBoard, goalPositions, _gridSize);

                // Clone the scratch board for the SolveState
                byte[,] newBoard = (byte[,])scratchBoard.Clone();

                SolveState neighbor = new(newBoard, current.GCost + 1, newH, newRow, newCol, current)
                {
                    ParentMoveFromRow = newRow,
                    ParentMoveFromCol = newCol,
                    ParentMoveToRow = current.EmptyRow,
                    ParentMoveToCol = current.EmptyCol,
                    MoveDirectionFromParent = direction
                };
                open.Enqueue(neighbor, neighbor.FCost);
                return false;
            }
            finally
            {
                // Return scratch board to pool
                _arrayPool.ReturnScratchBoard(scratchBoard);
            }
        }

        private bool StepSearch(
            PriorityQueue<SolveState, long> open,
            SolveStateDictionary closed,
            SolveStateDictionary oppositeClosed,
            ref SolveState? forwardState,
            ref SolveState? backwardState,
            bool isForward,
            (byte, byte)[] goalPositions)
        {
            forwardState = null;
            backwardState = null;
            if (open.Count == 0)
                return false;

            SolveState current = open.Dequeue();
            long stateHash = FastHash(current.Board);

            // Check if already in closed set
            if (closed.Exists(stateHash, current))
            {
                return false;
            }

            // Check if this state was reached from opposite direction before adding to closed
            if (oppositeClosed.TryGetState(stateHash, current, out SolveState? oppositeState))
            {
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

            if (MoveTile(MoveDirection.Up, current, open, closed, goalPositions))
            {
                return true;
            }
            if (MoveTile(MoveDirection.Down, current, open, closed, goalPositions))
            {
                return true;
            }
            if (MoveTile(MoveDirection.Left, current, open, closed, goalPositions))
            {
                return true;
            }
            if (MoveTile(MoveDirection.Right, current, open, closed, goalPositions))
            {
                return true;
            }
            return false;
        }

        private List<Move> ReconstructPath(SolveState forwardState, SolveState backwardState)
        {
            List<Move> result = new();

            // Reconstruct forward path: collect states from initial to meeting point
            List<SolveState> forwardStates = new();
            SolveState? current = forwardState;
            while (current != null)
            {
                forwardStates.Add(current);
                current = current.Parent;
            }
            forwardStates.Reverse(); // Now: initial -> meeting_point

            //Debug.WriteLine("=============== Move generation: Forward ===============");
            // Extract moves from forward states by comparing consecutive boards
            for (int i = 0; i < forwardStates.Count - 1; i++)
            {
                Move move = ExtractMoveBetweenStates(forwardStates[i], forwardStates[i + 1]);
                result.Add(move);
            }

            // Reconstruct backward path: collect states from goal to meeting point  
            List<SolveState> backwardStates = new();
            current = backwardState;
            while (current != null)
            {
                backwardStates.Add(current);
                current = current.Parent;
            }
            // Extract moves from backward states by comparing consecutive boards
            for (int i = 0; i < backwardStates.Count - 1; i++)
            {
                Move move = ExtractMoveBetweenStates(backwardStates[i], backwardStates[i + 1]);
                result.Add(move);
            }

            return result;
        }

        private Move ExtractMoveBetweenStates(SolveState fromState, SolveState toState)
        {
            // The position of the tile that has moved is the empty position in toState,
            // and the position of the empty tile in fromState is where the tile moved to.
            return new Move { FromRow = toState.EmptyRow, FromColumn = toState.EmptyCol, ToRow = fromState.EmptyRow, ToColumn = fromState.EmptyCol };
        }

        public int GetHeuristic(List<BoardTile> board, IHeuristicElementFactory heuristicElementFactory)
        {
            byte gridSize = (byte)Math.Sqrt(board.Count);
            byte[,] boardArray = new byte[gridSize, gridSize];
            (byte row, byte col)[] goalPositions = new (byte, byte)[gridSize * gridSize];
            for (byte row = 0; row < gridSize; row++)
            {
                for (byte col = 0; col < gridSize; col++)
                {
                    // Set up initial board
                    byte index = (byte)(row * gridSize + col);
                    boardArray[board[index].Row, board[index].Column] = (byte)board[index].Value;
                    // Set up goal board, and goal positions
                    if (row == gridSize - 1 && col == gridSize - 1)
                    {
                        goalPositions[0] = (row, col);
                    }
                    else
                    {
                        goalPositions[index + 1] = (row, col);
                    }
                }
            }
            _heuristicsCalculator = heuristicElementFactory.CreateHeuristicCalculator(_solverOptions, (int)Math.Sqrt(board.Count));
            return GetHeuristic(boardArray, goalPositions, gridSize);
        }

        private int GetHeuristic(byte[,] board, (byte row, byte col)[] goalPositions, byte gridSize)
        {
            _statesCalculatedCount++;
            return _heuristicsCalculator!.GetHeuristic(board, goalPositions, gridSize);
        }

        private bool BoardsEqual(byte[,] board1, byte[,] board2)
        {
            for (byte row = 0; row < _gridSize; row++)
            {
                for (byte col = 0; col < _gridSize; col++)
                {
                    if (board1[row, col] != board2[row, col])
                        return false;
                }
            }
            return true;
        }

        private long FastHash(byte[,] board)
        {
            unchecked
            {
                long hash = 17L;
                for (byte row = 0; row < _gridSize; row++)
                {
                    for (byte col = 0; col < _gridSize; col++)
                    {
                        hash = hash * 31L + board[row, col];
                    }
                }
                return hash;
            }
        }
    }
}

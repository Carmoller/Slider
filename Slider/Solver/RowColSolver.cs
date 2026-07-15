using Slider.Common;
using Slider.Common.Interfaces;
using Slider.Heuristics;
using Slider.Interfaces;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Slider.Solver
{
    public class RowColSolver : ISolver
    {
        public class RowColHeuristicCalculator : IHeuristicCalculator
        {
            private readonly Func<IHeuristicCalculator, Span<byte>, int, int> _getHeuristic;

            public List<IHeuristicElement> ElementCalculators { get; }

            public int GetHeuristic(Span<byte> board, int gridSize)
            {
                return _getHeuristic(this, board, gridSize);
            }
            public RowColHeuristicCalculator(Func<IHeuristicCalculator, Span<byte>, int, int> getHeuristic)
            {
                ElementCalculators = [];
                _getHeuristic = getHeuristic;
            }
        }
        public class RowColHeuristicFactory : IHeuristicElementFactory
        {
            private readonly Func<IHeuristicCalculator, Span<byte>, int, int> _getHeuristic;
            public RowColHeuristicFactory(Func<IHeuristicCalculator, Span<byte>, int, int> getHeuristic)
            {
                _getHeuristic = getHeuristic;

            }
            public IHeuristicCalculator CreateHeuristicCalculator(Span<byte> goalBoard, int gridSize, IOptions options, ISolverOptions solverOptions)
            {
                return new RowColHeuristicCalculator(_getHeuristic);
            }
            public IHeuristicElement CreateCornerPattern(Span<int> goalPositions, int gridSize)
            {
                throw new NotImplementedException();
            }
            public IHeuristicElement CreateLinearConflict(int gridSize)
            {
                throw new NotImplementedException();
            }

            public IHeuristicElement CreateManhattanDistance(Span<int> goalPositions, int gridSize)
            {
                throw new NotImplementedException();
            }
        }
        private int _gridSize;
        private readonly IOptions _options;
        private readonly IStateInfoFactory _stateInfoFactory;
        private double w = 3;
        private readonly ISolverFactory _solverFactory;
        private int _goalPositionsActiveCount;
        private byte[] _goalPositions;
        private readonly MinimumSpanningTree _mst = new();
        public RowColSolver(IOptions options, IStateInfoFactory stateInfoFactory, ISolverFactory solverFactory)
        {
            _options = options;
            _stateInfoFactory = stateInfoFactory;
            _solverFactory = solverFactory;
        }
        private SolveResult SolveOneRowColumn(ChunkedArrayPoolUnsafe arrayPool, ChunkedStructPool<StateInfo> stateInfoPool, byte[] board, int rowColNumber)
        {
            _goalPositionsActiveCount = 0;
            SolveResult result = new();
            _goalPositions = new byte[board.Length];
            for (int i = 0; i < _goalPositions.Length; i++)
            {
                if (i == 0)
                {
                    _goalPositions[i] = byte.MaxValue;
                    continue;
                }
                _goalPositionsActiveCount++;
                (int row, int col) = (i-1).ToRowAndColumn(_gridSize);
                _goalPositions[i] = (row == rowColNumber || col == rowColNumber) ? (byte)(i-1) : byte.MaxValue;
            }
            StateInfo startState = SolverHelper.CreateStateInfoFromBoard(
                board,
                arrayPool,
                stateInfoPool,
                new RowColHeuristicCalculator(GetHeuristics),
                _gridSize,
                p => p.CurrentG + p.CurrentH,
                GetHeuristics,
                GetHashCode);
            ISolver solver = _solverFactory.Create(SolverType.WeightedAStar);
            if (solver is IWeightedAStarSolver weightedSolver)
            {
                weightedSolver.InitialW = 2.7;
            }
            ;

            return solver.Solve(board, [], new SolverOptions { UseSprintFinish = false}, 
                new RowColHeuristicFactory(GetHeuristics));
        }

        public SolveResult Solve(byte[] board, byte[] targetBoard, ISolverOptions solverOptions, IHeuristicElementFactory heuristicElementFactory)
        {
            _gridSize = (int)Math.Sqrt(board.Length);
            ChunkedStructPool<StateInfo> stateInfoPool = new(1000000);
            ChunkedArrayPoolUnsafe arrayPool = new(1000000, _gridSize * _gridSize);

            SolveResult result = new();

            // Solve row and column, one by one, leaving increasingly small squares
            //for (int i = 0; i < _gridSize-1; i++)
            {
                return SolveOneRowColumn(arrayPool, stateInfoPool, board,0);
            }
            return result;
        }
        private static long GetHashCode(StateInfo state)
        {
            return StateHashes.FastHash(state.BoardToken.AsSpan());
        }

        private int GetHeuristics(IHeuristicCalculator heuristicsCalculator, Span<byte> board, int gridSize)
        {
            int manhattanDistance = 0;
            Span<byte> misplacedTiles = stackalloc byte[_goalPositionsActiveCount];
            int misplacedTilesIndex = 1;
            for (int i = 0; i < board.Length; i++)
            {
                if (board[i] == 0)
                {
                    misplacedTiles[0] = (byte)i;
                    continue;
                }
                byte goalPosition = _goalPositions[board[i]];
                if (goalPosition == byte.MaxValue)
                    continue;
                if (i != goalPosition)
                    misplacedTiles[misplacedTilesIndex++] = (byte)i;
                (int currentRow, int currentCol) = i.ToRowAndColumn(_gridSize);
                (int targetRow, int targetCol) = ((int)(goalPosition)).ToRowAndColumn(_gridSize);
                manhattanDistance += Math.Abs(targetRow - currentRow) + Math.Abs(targetCol - currentCol);
            }
            if (manhattanDistance == 0)
            {
                return 0;
            }
            int mstValue = _mst.CalculateMST(misplacedTiles, _gridSize);
            return manhattanDistance + 2*mstValue;
        }
        private static int GetHeuristics(Span<byte> board, int gridSize, IHeuristicCalculator customCalculator)
        {
            return customCalculator.GetHeuristic(board, gridSize);
        }

        public int GetHeuristic(List<BoardTile> board, IHeuristicElementFactory heuristicElementFactory)
        {
            byte[] byteBoard = board.OrderBy(p => p.Row).ThenBy(p => p.Column).Select(p => p.Value).ToArray();
            int gridSize = (int)(Math.Sqrt(byteBoard.Length));
            IHeuristicCalculator calculator = heuristicElementFactory.CreateHeuristicCalculator(Span<byte>.Empty, gridSize, _options,
                new SolverOptions { UseManhattanDistance = true, UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true });
            return GetHeuristics(byteBoard, gridSize, calculator);
        }


    }
}

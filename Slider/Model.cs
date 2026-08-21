using Slider.Common.Interfaces;
using Slider.Heuristics;
using Slider.Interfaces;
using Slider.Solver;
using Slider.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;

namespace Slider
{
    public class Model : IModel
    {
        public event EventHandler? BoardLayoutChanged;
        public event EventHandler? BoardSolved;
        public List<BoardTile> Board { get; private set; }
        public bool CanUndo { get { return MoveHistory.Count > 0; } }
        public int Heuristic { get; private set; }
        public LinkedList<Move> MoveHistory { get; private set; } = new();
        public int NumberOfMoves { get; private set; }

        private readonly ISolverFactory _solverFactory;
        private readonly IOptions _options;
        private readonly IGenerator _generator;
        private readonly IHeuristicElementFactory _heuristicElementFactory;
        private readonly IHeuristicCalculatorFactory _heuristicCalculatorFactory;
        private int[] _goalPositions = [];
        private BoardTile? _emptyTile;
        public Model(IGenerator generator, ISolverFactory solverFactory, IOptions options, IHeuristicCalculatorFactory heuristicCalculatorFactory,
                    IHeuristicElementFactory heuristicElementFactory)
        {
            _options = options;
            _generator = generator;
            _solverFactory = solverFactory;
            _heuristicElementFactory = heuristicElementFactory;
            _heuristicCalculatorFactory = heuristicCalculatorFactory;
            _options.PropertyChanged += Options_PropertyChanged;
            Board = new();
            GenerateEmptyBoard(_options.GridSize);
        }

        private int CalculateHeuristics(byte[] board)
        {
            if (_goalPositions.Length == 0)
            {
                _goalPositions = GetGoalBoard(_options.GridSize);
            }

            IHeuristicCalculator heuristicCalculator = _heuristicCalculatorFactory.GetHeuristicCalculator(_goalPositions, _options.GridSize);
            return heuristicCalculator.GetHeuristic(board, _options.GridSize);
        }

        private void GenerateEmptyBoard(int gridSize)
        {
            for (int i = 0; i < gridSize * gridSize; i++)
            {
                (int row, int col) = Math.DivRem(i, gridSize);
                BoardTile tile = new BoardTile()
                {
                    Value = 0, // Create board with all blanks
                    Row = row,
                    Column = col,
                };
                Board.Add(tile);
            }
        }

        private void Options_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IOptions.GridSize))
            {
                Board = new();
                GenerateEmptyBoard(_options.GridSize);
                MoveHistory.Clear();
                NumberOfMoves = 0;
                BoardLayoutChanged?.Invoke(this, new EventArgs());
            }
        }

        public void New()
        {
            GenerateBoard();
        }
        public void Undo()
        {
            if (MoveHistory.Count == 0)
                return;
            Move lastMove = MoveHistory.Last!.Value;
            MoveHistory.RemoveLast();
            BoardTile tileToMoveBack = Board.Find(t => t.Row == lastMove.ToRow && t.Column == lastMove.ToColumn)!;
            int tempColumn = tileToMoveBack.Column;
            int tempRow = tileToMoveBack.Row;
            tileToMoveBack.MoveTo(lastMove.FromRow, lastMove.FromColumn);
            _emptyTile?.MoveTo(tempRow, tempColumn);
            NumberOfMoves--;
            Heuristic = CalculateHeuristics(Board.OrderBy(p=>p.Row).ThenBy(p=>p.Column).Select(p=>p.Value).ToArray());
        }

        private static int[] GetGoalBoard(int gridSize)
        {
            int[] goalPositions = new int[gridSize*gridSize];
            for (int i = 1; i < gridSize * gridSize; i++)
            {
                if (i == 0)
                {
                    goalPositions[i] = gridSize * gridSize - 1;
                }
                else
                    goalPositions[i] = i - 1;
            }
            return goalPositions;
        }

        private void GenerateBoard()
        {
            Board = new();
            _goalPositions = [];
            MoveHistory.Clear();
            _emptyTile = null;
            NumberOfMoves = 0;
            byte[] newBoard = _generator.Generate(_options.GridSize);
            for (int i = 0; i < newBoard.Length; i++)
            {
                (int row, int col) = Math.DivRem(i, _options.GridSize);
                BoardTile tile = new BoardTile()
                {
                    Value = newBoard[i],
                    Row = row,
                    Column = col,
                };
                if (tile.IsEmpty)
                {
                    _emptyTile = tile;
                }
                Board.Add(tile);
            }
            if (_emptyTile == null)
            {
                throw new InvalidOperationException("Generated board does not contain an empty tile.");
            }
            BoardLayoutChanged?.Invoke(this, EventArgs.Empty);
            int[] goalPositions = GetGoalBoard(_options.GridSize);
            Heuristic = CalculateHeuristics(newBoard.ToArray());
        }

        public AllowedMove CanMove(BoardTile tile)
        {
            if (_emptyTile == null || _emptyTile == tile)
            {
                return AllowedMove.None;
            }
            if (tile.Row == _emptyTile.Row && Math.Abs(tile.Column - _emptyTile.Column) == 1)
            {
                return tile.Column < _emptyTile.Column ? AllowedMove.Right : AllowedMove.Left;
            }
            else if (tile.Column == _emptyTile.Column && Math.Abs(tile.Row - _emptyTile.Row) == 1)
            {
                return tile.Row < _emptyTile.Row ? AllowedMove.Down : AllowedMove.Up;
            }
            return AllowedMove.None;
        }

        public AllowedMove MoveTile(BoardTile tile)
        {
            if (CanMove(tile) == AllowedMove.None)
                return AllowedMove.None;
            AllowedMove moveDirection = GetMoveDirection(tile.Row, tile.Column, _emptyTile!.Row, _emptyTile.Column);
            int tempColumn = tile.Column;
            int tempRow = tile.Row;
            tile.MoveTo(_emptyTile.Row, _emptyTile.Column);
            _emptyTile.MoveTo(tempRow, tempColumn);
            //tile.Column = _emptyTile!.Column;
            //tile.Row = _emptyTile.Row;
            //_emptyTile.Column = tempColumn;
            //_emptyTile.Row = tempRow;
            NumberOfMoves++;
            MoveHistory.AddLast(new Move { FromColumn = tempColumn, FromRow = tempRow, ToColumn = tile.Column, ToRow = tile.Row });
            Heuristic = CalculateHeuristics(Board.OrderBy(p => p.Row).ThenBy(p => p.Column).Select(p => p.Value).ToArray());
            IsSolved();
            return moveDirection;
        }

        private AllowedMove GetMoveDirection(int fromRow, int fromColumn, int toRow, int toColumn)
        {
            int rowChange = fromRow - toRow;
            int columnChange = fromColumn - toColumn;

            if (rowChange < 0)
                return AllowedMove.Down;
            if (rowChange > 0)
                return AllowedMove.Up;
            if (columnChange < 0)
                return AllowedMove.Right;
            if (columnChange > 0)
                return AllowedMove.Left;
            return AllowedMove.None;
        }

        public void EditFinished()
        {
            _emptyTile = Board.FirstOrDefault(p => p.Value == 0);
        }

        public bool IsSolved()
        {
            for (int i=0; i< Board.Count - 1; i++)
            {
                BoardTile currentTile = Board[i];
                if (currentTile.IsEmpty)
                {
                    // Empty must be placed at the bottom right
                    if (currentTile.Row != _options.GridSize - 1 || currentTile.Column != _options.GridSize - 1)
                    {
                        return false;
                    }
                    else 
                        continue;
                }
                int expectedValue = currentTile.Row * _options.GridSize + currentTile.Column + 1;
                if (currentTile.Value != expectedValue)
                {
                    return false;
                }
            }
            BoardSolved?.Invoke(this, EventArgs.Empty);
            return true;
        }

        private byte[] ByteArrayFromBoard(List<BoardTile> board)
        {
            int gridSize = (int)Math.Sqrt(board.Count);
            byte[] array = new byte[board.Count];
            foreach (BoardTile tile in board)
            {
                array[tile.Row * gridSize + tile.Column] = tile.Value;
            }
            return array;
        }
        public SolveResult Solve()
        {
            Debug.WriteLine($"{DateTime.Now}: Starting Solve()");
            ISolver solver = _solverFactory.Create(_options.GridSize, Heuristic);
            SolveResult result = solver.Solve(ByteArrayFromBoard(Board), [], _options.SolverOptions, _heuristicElementFactory);
            if ((result.Result == SolveResultType.Solved) || (result.Result == SolveResultType.Timeout))
                Debug.WriteLine($"{DateTime.Now}: Finished Solve() in {result.TimeSpent.ToString()}, Using {result.Moves!.Count} moves");
            else 
                Debug.WriteLine($"{DateTime.Now}: Finished Solve() in {result.TimeSpent.ToString()}, Result: {result.Result}");
            return result;
        }
    }
}

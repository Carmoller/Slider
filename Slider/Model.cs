using Slider.Interfaces;
using Slider.Solver;
using Slider.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

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

        private readonly IOptions _options;
        private readonly IGenerator _generator;
        private readonly ISolver _solver;
        private readonly IHeuristicElementFactory _heuristicElementFactory;
        private BoardTile? _emptyTile;
        public Model(IGenerator generator, ISolver solver, IOptions options, IHeuristicElementFactory heuristicElementFactory)
        {
            _options = options;
            _generator = generator;
            _solver = solver;
            _heuristicElementFactory = heuristicElementFactory;
            _options.PropertyChanged += Options_PropertyChanged;
            Board = new();
        }

        private void Options_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IOptions.GridSize))
            {
                Board = new();
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
            tileToMoveBack.Column = lastMove.FromColumn;
            tileToMoveBack.Row = lastMove.FromRow;
            _emptyTile!.Column = tempColumn;
            _emptyTile.Row = tempRow;
            NumberOfMoves--;
            Heuristic = _solver.GetHeuristic(Board, _heuristicElementFactory);
        }
        private void GenerateBoard()
        {
            Board = new();
            MoveHistory.Clear();
            _emptyTile = null;
            NumberOfMoves = 0;
            List<int> newBoard = _generator.Generate(_options.GridSize);
            Debug.Assert(newBoard.Count == _options.GridSize * _options.GridSize);
            int newBoardIndex = 0;
            for (int row = 0; row < _options.GridSize; row++)
            {
                for (int col = 0; col < _options.GridSize; col++)
                {
                    BoardTile tile = new BoardTile()
                    {
                        Value = newBoard[newBoardIndex++],
                        Row = row,
                        Column = col,
                    };
                    if (tile.IsEmpty)
                    {
                        _emptyTile = tile;
                    }
                    Board.Add(tile);
                }
            }
            if (_emptyTile == null)
            {
                throw new InvalidOperationException("Generated board does not contain an empty tile.");
            }
            BoardLayoutChanged?.Invoke(this, EventArgs.Empty);
            Heuristic = _solver.GetHeuristic(Board, _heuristicElementFactory);
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

        public void MoveTile(BoardTile tile)
        {
            if (CanMove(tile) == AllowedMove.None)
                return;
            int tempColumn = tile.Column;
            int tempRow = tile.Row;
            tile.Column = _emptyTile!.Column;
            tile.Row = _emptyTile.Row;
            _emptyTile.Column = tempColumn;
            _emptyTile.Row = tempRow;
            NumberOfMoves++;
            MoveHistory.AddLast(new Move { FromColumn = tempColumn, FromRow = tempRow, ToColumn = tile.Column, ToRow = tile.Row });
            Heuristic = _solver.GetHeuristic(Board, _heuristicElementFactory);
            IsSolved();
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

        public SolveResult Solve()
        {
            Debug.WriteLine($"{DateTime.Now}: Starting Solve()");
            SolveResult result = _solver.Solve(Board, _options.SolverOptions, _heuristicElementFactory);
            if (result.Result == SolveResultType.Solved)
                Debug.WriteLine($"{DateTime.Now}: Finished Solve() in {result.TimeSpent.ToString()}, Using {result.Moves!.Count} moves");
            else 
                Debug.WriteLine($"{DateTime.Now}: Finished Solve() in {result.TimeSpent.ToString()}, Result: {result.Result}");
            return result;
        }
    }
}

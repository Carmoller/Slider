using Prism.Commands;
using Slider.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Slider.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, IMainViewModel
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private double _canvasWidth;
        private DateTime _startTime;
        private string _timeElapsed = "00:00:00";
        private bool _isSolveDataAvailable;
        private TimeSpan _solveTimeElasped;
        private int _solveMoveCount;
        private long _totalStatesConsidered;
        private long _forwardDictonarySize ;
        private long _backwardDictonarySize ;
        private long _forwardCollisionCount ;
        private long _backwardCollisionCount ;
        private long _forwardHitCount ;
        private long _backwardHitCount;

        public int GridSize { get => _options.GridSize; set { _options.GridSize = value; OnPropertyChanged(); } } 
        public int AnimationDelay { get => _options.AnimationDelay; set { _options.AnimationDelay = value; OnPropertyChanged(); } }
        public int Heuristic { get => _model.Heuristic; }
        public int NumberOfMoves { get => _model.NumberOfMoves; }
        public string TimeElapsed { get => _timeElapsed; set { if (value != _timeElapsed) { _timeElapsed = value; OnPropertyChanged(); } } }
        public bool IsSolveDataAvailable { get => _isSolveDataAvailable; set { if (value != _isSolveDataAvailable) { _isSolveDataAvailable = value; OnPropertyChanged(); } } }
        public TimeSpan SolveTime { get { return _solveTimeElasped; } set {if (value != _solveTimeElasped) { _solveTimeElasped = value; OnPropertyChanged(); } } }
        public int SolveMoveCount { get { return _solveMoveCount; } set { if (value != _solveMoveCount) { _solveMoveCount = value; OnPropertyChanged(); } } }
        public long ForwardDictonarySize { get { return _forwardDictonarySize; } set { if (value != _forwardDictonarySize) { _forwardDictonarySize = value; OnPropertyChanged(); } } }
        public long BackwardDictonarySize { get { return _backwardDictonarySize; } set { if (value != _backwardDictonarySize) { _backwardDictonarySize = value; OnPropertyChanged(); } } }
        public long ForwardCollisionCount { get { return _forwardCollisionCount; } set { if (value != _forwardCollisionCount) { _forwardCollisionCount = value; OnPropertyChanged(); } } }
        public long BackwardCollisionCount { get { return _backwardCollisionCount; } set { if (value != _backwardCollisionCount) { _backwardCollisionCount = value; OnPropertyChanged(); } } }
        public long ForwardHitCount { get { return _forwardHitCount; } set { if (value != _forwardHitCount) { _forwardHitCount = value; OnPropertyChanged(); } } }
        public long BackwardHitCount { get { return _backwardHitCount; } set { if (value != _backwardHitCount) { _backwardHitCount = value; OnPropertyChanged(); } } }
        public long TotalStatesConsidered { get { return _totalStatesConsidered; } set { if (value != _totalStatesConsidered) { _totalStatesConsidered = value; OnPropertyChanged(); } } }
        public ObservableCollection<ITileControlViewModel> Tiles { get; private set; } = new();
        public ObservableCollection<Move> SolveMoves { get; private set; } = new();

        public DelegateCommand NewGameCommand { get; private set; }
        public DelegateCommand UndoCommand { get; private set; }
        public DelegateCommand SolveCommand { get; private set; }

        private DispatcherTimer _gameTimer = new DispatcherTimer();

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private IModel _model;
        private IOptions _options;
        private ITileControlViewModelFactory _viewModelFactory;
        private IUserAlert _userAlert;

        public MainViewModel(IModel model, ITileControlViewModelFactory viewModelFactory, IOptions options, IUserAlert userAlert)
        {
            _model = model;
            _options = options;
            _viewModelFactory = viewModelFactory;
            _userAlert = userAlert;
            _model.BoardLayoutChanged += Model_BoardLayoutChanged;
            _model.BoardSolved += Model_BoardSolved;
            NewGameCommand = new (NewGameCommand_Executed);
            UndoCommand = new(UndoCommand_Executed, UndoCommand_CanExecute);
            SolveCommand = new(SolveCommand_Executed, SolveCommand_CanExecute);

            _gameTimer.Interval = TimeSpan.FromMilliseconds(500);
            _gameTimer.Tick += GameTimer_Tick;
        }

        public bool UndoCommand_CanExecute()
        {
            return _model.CanUndo;  
        }

        public void UndoCommand_Executed()
        {
            _model.Undo();
            OnPropertyChanged(nameof(NumberOfMoves));
            OnPropertyChanged(nameof(Heuristic));
            RecalculateTilePositions();
            UndoCommand.RaiseCanExecuteChanged();
        }

        public void NewGameCommand_Executed()
        {
            IsSolveDataAvailable = false;
            SolveMoves.Clear();
            _model.New();
            OnPropertyChanged(nameof(Heuristic));
            _startTime = DateTime.Now;
            _gameTimer.Start();
        }
        public void SolveCommand_Executed()
        {
            IsSolveDataAvailable = false;
            SolveMoves.Clear();
            SolveResult result = _model.Solve();
            if (result.Result != SolveResultType.Solved)
            {
                _userAlert.Alert("The puzzle could not be solved!", "Sliding Puzzle");
                return;
            }
            IsSolveDataAvailable = true;
            SolveTime = result.TimeSpent;
            SolveMoveCount = result.MoveCount;
            TotalStatesConsidered = result.TotalStatesConsidered;
            ForwardDictonarySize = result.ForwardDictonarySize;
            BackwardDictonarySize = result.BackwardDictonarySize;
            foreach (Move move in result.Moves)
            {
                SolveMoves.Add(move);
            }
            SetHighlightedTile(SolveMoves[0].FromRow, SolveMoves[0].FromColumn);
        }

        public bool SolveCommand_CanExecute()
        {
            return true;
        }

        private void GameTimer_Tick(object? sender, EventArgs e)
        {
            TimeSpan elapsed = DateTime.Now - _startTime;
            TimeElapsed = elapsed.ToString(@"hh\:mm\:ss");
        }
        private void Model_BoardSolved(object? sender, EventArgs e)
        {
            _gameTimer.Stop();
            OnPropertyChanged(nameof(NumberOfMoves));
            OnPropertyChanged(nameof(Heuristic));
            _userAlert.Alert("Congratulations! You solved the puzzle!", "Sliding Puzzle");
        }

        private void SetHighlightedTile(int row, int column)
        {
            foreach (ITileControlViewModel tile in Tiles)
            {
                tile.IsHighlighted = (tile.Row == row) && (tile.Column == column);
            }
        }

        private void ClearHighligths()
        {
            foreach (ITileControlViewModel tile in Tiles)
            {
                tile.IsHighlighted = false;
            }
        }

        private void Model_BoardLayoutChanged(object? sender, EventArgs e)
        {
            TimeElapsed = "00:00:00";
            SolveMoves.Clear();
            _gameTimer.Stop();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NumberOfMoves)));
            OnPropertyChanged(nameof(NumberOfMoves));
            OnPropertyChanged(nameof(Heuristic));
            UndoCommand.RaiseCanExecuteChanged();
            Tiles.Clear();
            for (int i = 0; i < _model.Board.Count; i++)
            {
                Tiles.Add(_viewModelFactory.CreateViewModel(_model.Board[i], this));
            }
            RecalculateTilePositions();
        }

        public void CanvasSizeChanged(SizeChangedEventArgs e)
        {
            _canvasWidth = e.NewSize.Width;

            int tileSize = GetTileSize();
            RecalculateTilePositions();
        }

        public int GetTileSize()
        {
            return (int)(_canvasWidth / _options.GridSize);
        }

        private void RecalculateTilePosition(ITileControlViewModel tile)
        {
            tile.X = tile.Column * tile.TileSize;
            tile.Y = tile.Row * tile.TileSize;
            tile.TileSize = tile.TileSize;
        }
        private void RecalculateTilePositions()
        {
            int tileSize = GetTileSize();
            for (int i = 0; i < Tiles.Count; i++)
            {
                Tiles[i].TileSize = tileSize;
                RecalculateTilePosition(Tiles[i]);
            }
        }
        public AllowedMove CanMove(ITileControlViewModel tile)
        {
            return _model.CanMove(tile.BoardTile);
        }

        public void MoveTile(ITileControlViewModel tile)
        {
            if (CanMove(tile) == AllowedMove.None)
                return;
            if ((SolveMoves.Count > 0) && (tile.Row == SolveMoves[0].FromRow) && (tile.Column == SolveMoves[0].FromColumn))
            {
                SolveMoves.RemoveAt(0);
                if (SolveMoves.Count > 0)
                    SetHighlightedTile(SolveMoves[0].FromRow, SolveMoves[0].FromColumn);
                else
                    ClearHighligths();  
            }

            _model.MoveTile(tile.BoardTile);
            ITileControlViewModel emptyTile = Tiles.First(t => t.IsEmpty);
            RecalculateTilePosition(emptyTile);
            OnPropertyChanged(nameof(NumberOfMoves));
            OnPropertyChanged(nameof(Heuristic));
            UndoCommand.RaiseCanExecuteChanged();
        }
    }
}

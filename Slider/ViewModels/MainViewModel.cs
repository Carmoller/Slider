using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Prism.Commands;
using Slider.Common.Interfaces;
using Slider.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Slider.ViewModels
{
    public partial class MainViewModel : ObservableObject, IMainViewModel
    {
        private DateTime _startTime;

        public int GridSize { get => _options.GridSize; set { _options.GridSize = value; OnPropertyChanged(); } } 
        public int AnimationDelay { get => _options.AnimationDelay; set { _options.AnimationDelay = value; OnPropertyChanged(); } }
        public int Heuristic { get => _model.Heuristic; }
        public int NumberOfMoves { get => _model.NumberOfMoves; }
        public bool CanSelect { get { return State == GameState.Editing; } }
        public bool CanMove { get { return State != GameState.Editing; } }
        [ObservableProperty]
        public partial GameState State { get; set; } = GameState.Playing;
        [ObservableProperty]
        public partial ITileControlViewModel? SelectedItem { get; set; }
        [ObservableProperty]
        public partial string TimeElapsed { get; set; } = string.Empty;
        [ObservableProperty]
        public partial SolvableStatus SolvableStatus { get; set; }
        [ObservableProperty]
        public partial bool IsSolveDataAvailable { get; set; }
        [ObservableProperty]
        public partial TimeSpan SolveTime { get; set; }
        [ObservableProperty]
        public partial int SolveMoveCount { get; set; }
        [ObservableProperty]
        public partial SolveResultType SolveResult { get; set; }
        [ObservableProperty]
        public partial int MinimumH { get; set; }
        [ObservableProperty]
        public partial int MinimumHNodeIndex { get; set; }
        [ObservableProperty]
        public partial TimeSpan MinimumHTime { get; set; }
        [ObservableProperty]
        public partial long ForwardDictonarySize { get; set; }
        [ObservableProperty]
        public partial long BackwardDictonarySize { get; set; }
        [ObservableProperty]
        public partial long ForwardCollisionCount { get; set; }
        [ObservableProperty]
        public partial long BackwardCollisionCount { get; set; }
        [ObservableProperty]
        public partial long ForwardHitCount { get; set; }
        [ObservableProperty]
        public partial long BackwardHitCount { get; set; }
        [ObservableProperty]
        public partial long TotalStatesConsidered { get; set; }
        [ObservableProperty]
        public partial long IDAStarIterations { get; set; }
        [ObservableProperty]
        public partial ObservableCollection<ITileControlViewModel> Tiles { get; private set; } = new();
        [ObservableProperty]
        public partial ObservableCollection<Move> SolveMoves { get; private set; } = new();
        public DelegateCommand GenerateCommand { get; private set; }
        public DelegateCommand EditCommand { get; private set; }
        public DelegateCommand UndoCommand { get; private set; }
        public DelegateCommand SolveCommand { get; private set; }
        public DelegateCommand AutoPlayCommand { get; private set; }

        private DispatcherTimer _gameTimer = new DispatcherTimer();

        private IModel _model;
        private IOptions _options;
        private ITileControlViewModelFactory _viewModelFactory;
        private IUserAlert _userAlert;

        public MainViewModel(IModel model, ITileControlViewModelFactory viewModelFactory, IOptions options, IUserAlert userAlert)
        {
            _model = model;
            _options = options;
            _options.PropertyChanged += Options_PropertyChanged;
            _viewModelFactory = viewModelFactory;
            _userAlert = userAlert;
            _model.BoardLayoutChanged += Model_BoardLayoutChanged;
            _model.BoardSolved += Model_BoardSolved;
            GenerateCommand = new (GenerateCommand_Executed);
            EditCommand = new(EditCommand_Executed);
            UndoCommand = new(UndoCommand_Executed, UndoCommand_CanExecute);
            SolveCommand = new(SolveCommand_Executed, SolveCommand_CanExecute);
            AutoPlayCommand = new(AutoPlayCommand_Executed, AutoPlayCommand_CanExecute);
            _gameTimer.Interval = TimeSpan.FromMilliseconds(500);
            _gameTimer.Tick += GameTimer_Tick;
            Model_BoardLayoutChanged(null, new EventArgs());
        }

        private void Options_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
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
            UndoCommand.RaiseCanExecuteChanged();
        }
        public bool AutoPlayCommand_CanExecute()
        {
            return SolveMoveCount > 0;
        }

        public void AutoPlayCommand_Executed()
        {
            State = GameState.Playing;
            while (SolveMoves.Count > 0)
            {
                Move move = SolveMoves[0];
                ITileControlViewModel tile = Tiles.First(p => p.Row == move.FromRow && p.Column == move.FromColumn);
                MoveTile(tile);
                OnPropertyChanged(nameof(Heuristic));
            }
        }

        public void GenerateCommand_Executed()
        {
            State = GameState.Playing;
            IsSolveDataAvailable = false;
            SolveMoves.Clear();
            _model.New();
            OnPropertyChanged(nameof(Heuristic));
            _startTime = DateTime.Now;
            _gameTimer.Start();
        }
        public void EditCommand_Executed()
        {
            State = GameState.Editing;
            if (Tiles.Count > 0)
            {
                SelectedItem = Tiles[0];
            }
        }

        public void SolveCommand_Executed()
        {
            State = GameState.Playing;
            IsSolveDataAvailable = false;
            SolveMoves.Clear();
            SolveResult result = _model.Solve();
            if ((result.Result != SolveResultType.Solved) && (result.Result != SolveResultType.Timeout))
            {
                _userAlert.Alert("The puzzle could not be solved!", "Sliding Puzzle");
                return;
            }
            IsSolveDataAvailable = true;
            SolveTime = result.TimeSpent;
            SolveMoveCount = result.MoveCount;
            SolveResult = result.Result;
            MinimumH = result.MinimumH;
            MinimumHNodeIndex = result.MinimumHNodeIndex;
            MinimumHTime = result.MinimumHTime;
            TotalStatesConsidered = result.TotalStatesConsidered;
            ForwardDictonarySize = result.ForwardDictonarySize;
            BackwardDictonarySize = result.BackwardDictonarySize;
            IDAStarIterations = result.IDAStarIterations;
            foreach (Move move in result.Moves)
            {
                SolveMoves.Add(move);
            }
            if (SolveMoves.Count > 0)
                SetHighlightedTile(SolveMoves[0].FromRow, SolveMoves[0].FromColumn);
            AutoPlayCommand.RaiseCanExecuteChanged();
        }

        public bool SolveCommand_CanExecute()
        {
            return true;
        }
        partial void OnStateChanged(GameState oldValue, GameState newValue)
        {
            OnPropertyChanged(nameof(CanSelect));
            OnPropertyChanged(nameof(CanMove));
            if (oldValue == GameState.Editing)
            {
                _model.EditFinished();
            }
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
            State = GameState.Playing;
            TimeElapsed = "00:00:00";
            SolveMoves.Clear();
            _gameTimer.Stop();
            OnPropertyChanged(nameof(NumberOfMoves));
            OnPropertyChanged(nameof(Heuristic));
            UndoCommand.RaiseCanExecuteChanged();
            Tiles.Clear();
            for (int i = 0; i < _model.Board.Count; i++)
            {
                ITileControlViewModel tileVm = _viewModelFactory.CreateViewModel(_model.Board[i]);
                tileVm.CanMove = true;
                tileVm.AnimationDelay = AnimationDelay;
                Tiles.Add(tileVm);
            }
        }

        public AllowedMove GetAllowedMoves(ITileControlViewModel tile)
        {
            return _model.CanMove(tile.BoardTile);
        }

        public void MoveTile(ITileControlViewModel tile)
        {
            if (GetAllowedMoves(tile) == AllowedMove.None)
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
            OnPropertyChanged(nameof(NumberOfMoves));
            OnPropertyChanged(nameof(Heuristic));
            UndoCommand.RaiseCanExecuteChanged();
        }

        
        private bool TileMove(ITileControlViewModel tile)
        {
            if (!CanMove)
                return false;
            AllowedMove allowedMove = GetAllowedMoves(tile);
            if (allowedMove == AllowedMove.None)
                return false;
            MoveTile(tile);
            return true;
        }
        private bool LaunchSelectTileWindow(ITileControlViewModel clickedTile)
        {
            List<ITileControlViewModel> alreadyPickedNumbers = Tiles.Where(p => p.Value != 0).ToList();
            SelectTileViewModel selectTileVm = new(GridSize, alreadyPickedNumbers);
            bool? result = WeakReferenceMessenger.Default.Send(new ShowSelectTileWindowMessage { ViewModel = selectTileVm });
            if (result == true)
            {
                if (selectTileVm.SelectedValue != 0)
                {
                    ITileControlViewModel? previouslyPicked = alreadyPickedNumbers.FirstOrDefault(p => p.Value == selectTileVm.SelectedValue);
                    if (previouslyPicked != null)
                    {
                        // Picked a tile which is already placed - remove it from its previous location
                        previouslyPicked.Value = 0;
                    }
                }
                clickedTile.Value = selectTileVm.SelectedValue;
                SolvableStatus = PuzzleChecker.IsSolvable(Tiles, GridSize);

            }
            return true;
        }

        public bool TileSelected(ITileControlViewModel tile, BoardSelectionMethod selectionMethod)
        {
            switch (State)
            {
                case GameState.Playing:
                    return TileMove(tile);
                case GameState.Editing:
                    return LaunchSelectTileWindow(tile);
            }
            return false;
        }
    }
}

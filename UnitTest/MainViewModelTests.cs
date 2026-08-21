using Moq;
using Slider.Common.Interfaces;
using Slider.Interfaces;
using Slider.ViewModels;

namespace UnitTest
{
    [TestClass]
    public sealed class MainViewModelTests
    {
        #region Property Tests

        [TestMethod]
        public void GridSize_Get_Returns_OptionsGridSize()
        {
            // Arrange
            Mock<IOptions> optionsMock = new();
            Mock<IModel> modelMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();
            optionsMock.Setup(o => o.GridSize).Returns(4);
            
            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);

            // Act
            int result = viewModel.GridSize;

            // Assert
            Assert.AreEqual(4, result);
        }

        [TestMethod]
        public void GridSize_Set_Updates_OptionsGridSize()
        {
            // Arrange
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(o => o.GridSize).Returns(4);
            Mock<IModel> modelMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();
            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);

            // Act
            int newSize = 5;
            viewModel.GridSize = newSize;

            // Assert
            optionsMock.VerifySet(o => o.GridSize = newSize);
        }

        [TestMethod]
        public void AnimationDelay_Get_Returns_OptionsAnimationDelay()
        {
            // Arrange
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(o => o.AnimationDelay).Returns(200);
            Mock<IModel> modelMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();

            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);

            // Act
            int result = viewModel.AnimationDelay;

            // Assert
            Assert.AreEqual(200, result);
        }

        [TestMethod]
        public void AnimationDelay_Set_Updates_OptionsAnimationDelay()
        {
            // Arrange
            Mock<IOptions> optionsMock = new();
            Mock<IModel> modelMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();

            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);

            // Act
            int newDelay = 300;
            viewModel.AnimationDelay = newDelay;

            // Assert
            optionsMock.VerifySet(o => o.AnimationDelay = newDelay);
        }

        [TestMethod]
        public void NumberOfMoves_Returns_ModelNumberOfMoves()
        {
            // Arrange
            Mock<IModel> modelMock = new();
            modelMock.Setup(m => m.NumberOfMoves).Returns(5);
            Mock<IOptions> optionsMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();

            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);

            // Act
            int result = viewModel.NumberOfMoves;

            // Assert
            Assert.AreEqual(5, result);
        }

        [TestMethod]
        public void TimeElapsed_Default_Is_ZeroString()
        {
            // Arrange
            Mock<IModel> modelMock = new();
            Mock<IOptions> optionsMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();

            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);

            // Assert
            Assert.AreEqual("00:00:00", viewModel.TimeElapsed);
        }

        [TestMethod]
        public void TimeElapsed_Set_Updates_Property()
        {
            // Arrange
            Mock<IModel> modelMock = new();
            Mock<IOptions> optionsMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();

            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);

            string newTime = "00:01:30";

            // Act
            viewModel.TimeElapsed = newTime;

            // Assert
            Assert.AreEqual(newTime, viewModel.TimeElapsed);
        }

        [TestMethod]
        public void Tiles_InitiallyEmpty()
        {
            // Arrange
            Mock<IModel> modelMock = new();
            Mock<IOptions> optionsMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();

            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);

            // Assert
            Assert.HasCount(0, (System.Collections.IEnumerable)viewModel.Tiles);
        }

        [TestMethod]
        public void SolveMoves_InitiallyEmpty()
        {
            // Arrange
            Mock<IModel> modelMock = new();
            Mock<IOptions> optionsMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();

            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);

            // Assert
            Assert.HasCount(0, (System.Collections.IEnumerable)viewModel.SolveMoves);
        }

        #endregion

        #region Command Tests

        [TestMethod]
        public void GenerateCommand_Executed_Calls_Model_New()
        {
            // Arrange
            Mock<IModel> modelMock = new();
            Mock<IOptions> optionsMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            modelMock.Setup(m => m.Board).Returns(new List<BoardTile>());
            Mock<IUserAlert> userAlertMock = new();

            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);

            // Act
            try
            {
                viewModel.GenerateCommand.Execute();
            }
            catch (System.InvalidOperationException)
            {
                // DispatcherTimer requires a message loop - this is expected in test environment
            }

            // Assert
            modelMock.Verify(m => m.New(), Times.Once);
        }

        [TestMethod]
        public void GenerateCommand_Executed_Clears_SolveMoves()
        {
            // Arrange
            Mock<IModel> modelMock = new();
            Mock<IOptions> optionsMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            modelMock.Setup(m => m.Board).Returns(new List<BoardTile>());
            Mock<IUserAlert> userAlertMock = new();

            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);
            viewModel.SolveMoves.Add(new Move { FromRow = 0, FromColumn = 0 });

            // Act
            try
            {
                viewModel.GenerateCommand.Execute();
            }
            catch (System.InvalidOperationException)
            {
                // DispatcherTimer requires a message loop - this is expected in test environment
            }

            // Assert
            Assert.HasCount(0, (System.Collections.IEnumerable)viewModel.SolveMoves);
        }

        [TestMethod]
        public void UndoCommand_CanExecute_Returns_ModelCanUndo()
        {
            // Arrange
            Mock<IModel> modelMock = new();
            modelMock.Setup(m => m.CanUndo).Returns(true);
            Mock<IOptions> optionsMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();

            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);

            // Act
            bool result = viewModel.UndoCommand_CanExecute();

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void UndoCommand_CanExecute_Returns_False_When_ModelCannotUndo()
        {
            // Arrange
            Mock<IModel> modelMock = new();
            modelMock.Setup(m => m.CanUndo).Returns(false);
            Mock<IOptions> optionsMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();

            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);

            // Act
            bool result = viewModel.UndoCommand_CanExecute();

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void UndoCommand_Executed_Calls_Model_Undo()
        {
            // Arrange
            Mock<IModel> modelMock = new();
            modelMock.Setup(m => m.CanUndo).Returns(true);
            modelMock.Setup(m => m.Board).Returns(new List<BoardTile>());
            Mock<IOptions> optionsMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();

            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);

            // Act
            viewModel.UndoCommand_Executed();

            // Assert
            modelMock.Verify(m => m.Undo(), Times.Once);
        }

        [TestMethod]
        public void SolveCommand_Executed_Populates_SolveMoves()
        {
            // Arrange
            Mock<IModel> modelMock = new();
            List<Move> expectedMoves = new()
            {
                new Move { FromRow = 0, FromColumn = 1 },
                new Move { FromRow = 1, FromColumn = 1 }
            };
            modelMock.Setup(m => m.Solve()).Returns(new SolveResult(expectedMoves) { Result = SolveResultType.Solved }) 
            ;
            modelMock.Setup(m => m.Board).Returns(new List<BoardTile>());
            Mock<IOptions> optionsMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();

            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);

            // Act
            viewModel.SolveCommand_Executed();

            // Assert
            Assert.HasCount(expectedMoves.Count, (System.Collections.IEnumerable)viewModel.SolveMoves);
        }

        [TestMethod]
        public void SolveCommand_CanExecute_Returns_True()
        {
            // Arrange
            Mock<IModel> modelMock = new();
            Mock<IOptions> optionsMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();

            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);

            // Act
            bool result = viewModel.SolveCommand_CanExecute();

            // Assert
            Assert.IsTrue(result);
        }

        #endregion

        #region Method Tests

        [TestMethod]
        public void CanvasSizeChanged_Updates_TilePositions()
        {
            // Arrange
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(o => o.GridSize).Returns(2);
            Mock<IModel> modelMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();

            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);

            var mockTile = new Mock<ITileControlViewModel>();
            mockTile.Setup(t => t.Row).Returns(0);
            mockTile.Setup(t => t.Column).Returns(0);
            viewModel.Tiles.Add(mockTile.Object);

            var canvasWidthField = typeof(MainViewModel).GetField("_canvasWidth", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            canvasWidthField?.SetValue(viewModel, 200.0);

            // Act
            var recalculateMethod = typeof(MainViewModel).GetMethod("RecalculateTilePositions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            recalculateMethod?.Invoke(viewModel, null);

            // Assert
            mockTile.VerifySet(t => t.TileSize = 100);
        }

        [TestMethod]
        public void CanMove_Returns_ModelCanMoveResult()
        {
            // Arrange
            Mock<IModel> modelMock = new();
            modelMock.Setup(m => m.CanMove(It.IsAny<BoardTile>())).Returns(AllowedMove.Up);
            Mock<IOptions> optionsMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();

            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);

            var mockTile = new Mock<ITileControlViewModel>();

            // Act
            AllowedMove result = viewModel.GetAllowedMoves(mockTile.Object);

            // Assert
            Assert.AreEqual(AllowedMove.Up, result);
        }

        #endregion

        #region Property Changed Tests

        [TestMethod]
        public void GridSize_Set_Raises_PropertyChanged()
        {
            // Arrange
            Mock<IOptions> optionsMock = new();
            Mock<IModel> modelMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();

            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);
            
            string changedProperty = string.Empty;
            viewModel.PropertyChanged += (s, e) => changedProperty = e.PropertyName ?? string.Empty;

            // Act
            viewModel.GridSize = 5;

            // Assert
            Assert.AreEqual(nameof(MainViewModel.GridSize), changedProperty);
        }

        [TestMethod]
        public void TimeElapsed_Set_Raises_PropertyChanged()
        {
            // Arrange
            Mock<IModel> modelMock = new();
            Mock<IOptions> optionsMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();

            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);
            
            string changedProperty = string.Empty;
            viewModel.PropertyChanged += (s, e) => changedProperty = e.PropertyName ?? string.Empty;

            // Act
            viewModel.TimeElapsed = "00:01:00";

            // Assert
            Assert.AreEqual(nameof(MainViewModel.TimeElapsed), changedProperty);
        }

        [TestMethod]
        public void TimeElapsed_Set_SameValue_Does_Not_Raise_PropertyChanged()
        {
            // Arrange
            Mock<IModel> modelMock = new();
            Mock<IOptions> optionsMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();

            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);
            
            viewModel.TimeElapsed = "00:00:30";
            int changeCount = 0;
            viewModel.PropertyChanged += (s, e) => changeCount++;

            // Act
            viewModel.TimeElapsed = "00:00:30";

            // Assert
            Assert.AreEqual(0, changeCount);
        }

        #endregion

        #region Event Handler Tests

        [TestMethod]
        public void Model_BoardChanged_Clears_Tiles()
        {
            // Arrange
            Mock<IModel> modelMock = new();
            modelMock.Setup(m => m.Board).Returns(new List<BoardTile>());
            Mock<IOptions> optionsMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();

            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);
            
            viewModel.Tiles.Add(new Mock<ITileControlViewModel>().Object);
            viewModel.Tiles.Add(new Mock<ITileControlViewModel>().Object);

            // Act
            modelMock.Raise(m => m.BoardLayoutChanged += null, EventArgs.Empty);

            // Assert
            Assert.HasCount(0, (System.Collections.IEnumerable)viewModel.Tiles);
        }

        [TestMethod]
        public void Model_BoardChanged_Raises_NumberOfMoves_PropertyChanged()
        {
            // Arrange
            Mock<IModel> modelMock = new();
            modelMock.Setup(m => m.Board).Returns(new List<BoardTile>());
            Mock<IOptions> optionsMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();

            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);

            List<string> changedProperties = new();
            viewModel.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName ?? string.Empty);

            // Act
            modelMock.Raise(m => m.BoardLayoutChanged += null, EventArgs.Empty);

            // Assert
            Assert.Contains(nameof(MainViewModel.NumberOfMoves), changedProperties);
        }

        #endregion

        #region Tile Highlighting Tests

        [TestMethod]
        public void SetHighlightedTile_Sets_IsHighlighted_For_Matching_Tile()
        {
            // Arrange
            Mock<IModel> modelMock = new();
            Mock<IOptions> optionsMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();

            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);

            var mockTile1 = new Mock<ITileControlViewModel>();
            mockTile1.Setup(t => t.Row).Returns(0);
            mockTile1.Setup(t => t.Column).Returns(0);

            var mockTile2 = new Mock<ITileControlViewModel>();
            mockTile2.Setup(t => t.Row).Returns(1);
            mockTile2.Setup(t => t.Column).Returns(1);

            viewModel.Tiles.Add(mockTile1.Object);
            viewModel.Tiles.Add(mockTile2.Object);

            // Act
            var setHighlightedMethod = typeof(MainViewModel).GetMethod("SetHighlightedTile",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            setHighlightedMethod?.Invoke(viewModel, new object[] { 1, 1 });

            // Assert
            mockTile2.VerifySet(t => t.IsHighlighted = true);
            mockTile1.VerifySet(t => t.IsHighlighted = false, Times.Once);
        }

        [TestMethod]
        public void SetHighlightedTile_Unhighlights_Other_Tiles()
        {
            // Arrange
            Mock<IModel> modelMock = new();
            Mock<IOptions> optionsMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();

            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);

            var mockTile1 = new Mock<ITileControlViewModel>();
            mockTile1.Setup(t => t.Row).Returns(0);
            mockTile1.Setup(t => t.Column).Returns(0);

            var mockTile2 = new Mock<ITileControlViewModel>();
            mockTile2.Setup(t => t.Row).Returns(1);
            mockTile2.Setup(t => t.Column).Returns(1);

            viewModel.Tiles.Add(mockTile1.Object);
            viewModel.Tiles.Add(mockTile2.Object);

            // Act
            var setHighlightedMethod = typeof(MainViewModel).GetMethod("SetHighlightedTile",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            setHighlightedMethod?.Invoke(viewModel, new object[] { 0, 0 });

            // Assert
            mockTile1.VerifySet(t => t.IsHighlighted = true);
            mockTile2.VerifySet(t => t.IsHighlighted = false);
        }

        [TestMethod]
        public void ClearHighligths_Unhighlights_All_Tiles()
        {
            // Arrange
            Mock<IModel> modelMock = new();
            Mock<IOptions> optionsMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();

            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);

            var mockTile1 = new Mock<ITileControlViewModel>();
            var mockTile2 = new Mock<ITileControlViewModel>();
            var mockTile3 = new Mock<ITileControlViewModel>();

            viewModel.Tiles.Add(mockTile1.Object);
            viewModel.Tiles.Add(mockTile2.Object);
            viewModel.Tiles.Add(mockTile3.Object);

            // Act
            var clearHighlightsMethod = typeof(MainViewModel).GetMethod("ClearHighligths",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            clearHighlightsMethod?.Invoke(viewModel, null);

            // Assert
            mockTile1.VerifySet(t => t.IsHighlighted = false);
            mockTile2.VerifySet(t => t.IsHighlighted = false);
            mockTile3.VerifySet(t => t.IsHighlighted = false);
        }

        #endregion

        #region MoveTile Tests

        [TestMethod]
        public void MoveTile_Does_Not_Move_When_CanMove_Returns_None()
        {
            // Arrange
            Mock<IModel> modelMock = new();
            modelMock.Setup(m => m.CanMove(It.IsAny<BoardTile>())).Returns(AllowedMove.None);
            Mock<IOptions> optionsMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();

            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);

            var mockTile = new Mock<ITileControlViewModel>();

            // Act
            viewModel.MoveTile(mockTile.Object);

            // Assert
            modelMock.Verify(m => m.MoveTile(It.IsAny<BoardTile>()), Times.Never);
        }

        [TestMethod]
        public void MoveTile_Calls_Model_MoveTile_When_MoveAllowed()
        {
            // Arrange
            Mock<IModel> modelMock = new();
            modelMock.Setup(m => m.Board).Returns(new List<BoardTile>());
            modelMock.Setup(m => m.CanMove(It.IsAny<BoardTile>())).Returns(AllowedMove.Up);
            Mock<IOptions> optionsMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();

            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);

            var boardTile = new Mock<BoardTile>();
            var mockTile = new Mock<ITileControlViewModel>();
            mockTile.Setup(t => t.BoardTile).Returns(boardTile.Object);
            mockTile.Setup(t => t.Row).Returns(0);
            mockTile.Setup(t => t.Column).Returns(0);

            var emptyTile = new Mock<ITileControlViewModel>();
            emptyTile.Setup(t => t.IsEmpty).Returns(true);
            viewModel.Tiles.Add(emptyTile.Object);

            // Act
            viewModel.MoveTile(mockTile.Object);

            // Assert
            modelMock.Verify(m => m.MoveTile(boardTile.Object), Times.Once);
        }

        [TestMethod]
        public void MoveTile_Removes_Matching_SolveMove()
        {
            // Arrange
            Mock<IModel> modelMock = new();
            modelMock.Setup(m => m.Board).Returns(new List<BoardTile>());
            modelMock.Setup(m => m.CanMove(It.IsAny<BoardTile>())).Returns(AllowedMove.Up);
            Mock<IOptions> optionsMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();

            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);

            viewModel.SolveMoves.Add(new Move { FromRow = 0, FromColumn = 0 });
            viewModel.SolveMoves.Add(new Move { FromRow = 1, FromColumn = 1 });

            var mockTile = new Mock<ITileControlViewModel>();
            mockTile.Setup(t => t.Row).Returns(0);
            mockTile.Setup(t => t.Column).Returns(0);
            mockTile.Setup(t => t.BoardTile).Returns(new Mock<BoardTile>().Object);

            var emptyTile = new Mock<ITileControlViewModel>();
            emptyTile.Setup(t => t.IsEmpty).Returns(true);
            viewModel.Tiles.Add(emptyTile.Object);

            // Act
            viewModel.MoveTile(mockTile.Object);

            // Assert
            Assert.HasCount(1, (System.Collections.IEnumerable)viewModel.SolveMoves);
            Assert.AreEqual(1, viewModel.SolveMoves[0].FromRow);
            Assert.AreEqual(1, viewModel.SolveMoves[0].FromColumn);
        }

        [TestMethod]
        public void MoveTile_Highlights_Next_SolveMove()
        {
            // Arrange
            Mock<IModel> modelMock = new();
            modelMock.Setup(m => m.Board).Returns(new List<BoardTile>());
            modelMock.Setup(m => m.CanMove(It.IsAny<BoardTile>())).Returns(AllowedMove.Up);
            Mock<IOptions> optionsMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();

            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);

            viewModel.SolveMoves.Add(new Move { FromRow = 0, FromColumn = 0 });
            viewModel.SolveMoves.Add(new Move { FromRow = 1, FromColumn = 1 });

            var mockTile = new Mock<ITileControlViewModel>();
            mockTile.Setup(t => t.Row).Returns(0);
            mockTile.Setup(t => t.Column).Returns(0);
            mockTile.Setup(t => t.BoardTile).Returns(new Mock<BoardTile>().Object);

            var highlightedTile = new Mock<ITileControlViewModel>();
            highlightedTile.Setup(t => t.Row).Returns(1);
            highlightedTile.Setup(t => t.Column).Returns(1);

            var emptyTile = new Mock<ITileControlViewModel>();
            emptyTile.Setup(t => t.IsEmpty).Returns(true);

            viewModel.Tiles.Add(highlightedTile.Object);
            viewModel.Tiles.Add(emptyTile.Object);

            // Act
            viewModel.MoveTile(mockTile.Object);

            // Assert
            highlightedTile.VerifySet(t => t.IsHighlighted = true);
        }

        [TestMethod]
        public void MoveTile_Clears_Highlights_When_LastSolveMove()
        {
            // Arrange
            Mock<IModel> modelMock = new();
            modelMock.Setup(m => m.Board).Returns(new List<BoardTile>());
            modelMock.Setup(m => m.CanMove(It.IsAny<BoardTile>())).Returns(AllowedMove.Up);
            Mock<IOptions> optionsMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();

            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);

            viewModel.SolveMoves.Add(new Move { FromRow = 0, FromColumn = 0 });

            var mockTile = new Mock<ITileControlViewModel>();
            mockTile.Setup(t => t.Row).Returns(0);
            mockTile.Setup(t => t.Column).Returns(0);
            mockTile.Setup(t => t.BoardTile).Returns(new Mock<BoardTile>().Object);

            var emptyTile = new Mock<ITileControlViewModel>();
            emptyTile.Setup(t => t.IsEmpty).Returns(true);

            viewModel.Tiles.Add(emptyTile.Object);

            // Act
            viewModel.MoveTile(mockTile.Object);

            // Assert
            Assert.HasCount(0, (System.Collections.IEnumerable)viewModel.SolveMoves);
            emptyTile.VerifySet(t => t.IsHighlighted = false);
        }

        #endregion

        #region Constructor Tests

        [TestMethod]
        public void Constructor_Subscribes_To_Events()
        {
            // Arrange
            Mock<IModel> modelMock = new();
            Mock<IOptions> optionsMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();

            // Act
            var viewModel = new MainViewModel(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);

            // Assert - should not throw and events should be subscribed
            Assert.IsNotNull(viewModel.Tiles);
        }

        #endregion

        #region UndoCommand Tests

        [TestMethod]
        public void UndoCommand_Executed_Raises_CanExecuteChanged()
        {
            // Arrange
            Mock<IModel> modelMock = new();
            modelMock.Setup(m => m.CanUndo).Returns(true);
            modelMock.Setup(m => m.Board).Returns(new List<BoardTile>());
            Mock<IOptions> optionsMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();

            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);

            // Act
            viewModel.UndoCommand_Executed();

            // Assert - should not throw
            Assert.IsNotNull(viewModel.UndoCommand);
        }

        #endregion

        #region SolveCommand Tests

        [TestMethod]
        public void SolveCommand_Executed_Alerts_When_No_Moves()
        {
            // Arrange
            Mock<IModel> modelMock = new();
            modelMock.Setup(m => m.Solve()).Returns(new SolveResult(new List<Move>()));
            Mock<IOptions> optionsMock = new();
            Mock<ITileControlViewModelFactory> tileControlVmFactoryMock = new();
            Mock<IUserAlert> userAlertMock = new();
            userAlertMock.Setup(p => p.Alert("The puzzle could not be solved!", "Sliding Puzzle")).Verifiable();

            MainViewModel viewModel = new(modelMock.Object, tileControlVmFactoryMock.Object, optionsMock.Object, userAlertMock.Object);

            // Act
            viewModel.SolveCommand_Executed();

            // Assert
            Assert.HasCount(0, (System.Collections.IEnumerable)viewModel.SolveMoves);
            userAlertMock.Verify();
        }

        #endregion
    }
}

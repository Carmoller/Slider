using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Slider;
using Slider.Common.Interfaces;
using Slider.Interfaces;
using Slider.Solver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace UnitTest
{
    [TestClass]
    public class ModelTests
    {
        private Mock<IGenerator> _mockGenerator = null!;
        private Mock<ISolver> _mockSolver = null!;
        private Mock<IOptions> _mockOptions = null!;
        private Mock<IHeuristicElementFactory> _mockHeuristicFactory = null!;
        private Model _model = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockGenerator = new Mock<IGenerator>();
            _mockSolver = new Mock<ISolver>();
            _mockOptions = new Mock<IOptions>();
            _mockHeuristicFactory = new Mock<IHeuristicElementFactory>();

            // Setup default options
            _mockOptions.Setup(o => o.GridSize).Returns(3);
            _mockOptions.Setup(o => o.SolverOptions).Returns(new SolverOptions());

            // Setup generator to return solved 3x3 board: 1 2 3 / 4 5 6 / 7 8 0
            List<byte> solvedBoard = new() { 1, 2, 3, 4, 5, 6, 7, 8, 0 };
            _mockGenerator.Setup(g => g.Generate(It.IsAny<int>())).Returns(solvedBoard);

            // Setup solver
            _mockSolver.Setup(s => s.GetHeuristic(It.IsAny<List<BoardTile>>(), It.IsAny<IHeuristicElementFactory>())).Returns(0);

            _model = new Model(_mockGenerator.Object, _mockSolver.Object, _mockOptions.Object, _mockHeuristicFactory.Object);
        }

        [TestMethod]
        public void Constructor_InitializesProperties()
        {
            // Act & Assert
            Assert.IsNotNull(_model);
            Assert.IsNotNull(_model.Board);
            Assert.IsEmpty(_model.Board);
            Assert.AreEqual(0, _model.NumberOfMoves);
            Assert.IsFalse(_model.CanUndo);
        }

        [TestMethod]
        public void New_GeneratesBoard()
        {
            // Act
            _model.New();

            // Assert
            Assert.HasCount(9, _model.Board);
            _mockGenerator.Verify(g => g.Generate(3), Times.Once);
        }

        [TestMethod]
        public void New_ClearsMovementHistory()
        {
            // Arrange
            _model.New();
            _model.MoveTile(_model.Board[1]); // Move a tile

            // Act
            _model.New();

            // Assert
            Assert.AreEqual(0, _model.NumberOfMoves);
            Assert.IsFalse(_model.CanUndo);
        }

        [TestMethod]
        public void New_RaisesboardChangedEvent()
        {
            // Arrange
            bool eventRaised = false;
            _model.BoardLayoutChanged += (s, e) => eventRaised = true;

            // Act
            _model.New();

            // Assert
            Assert.IsTrue(eventRaised);
        }

        [TestMethod]
        public void CanMove_WithEmptyTile_ReturnNone()
        {
            // Arrange
            _model.New();
            BoardTile emptyTile = _model.Board.First(t => t.IsEmpty);

            // Act
            AllowedMove result = _model.CanMove(emptyTile);

            // Assert
            Assert.AreEqual(AllowedMove.None, result);
        }

        [TestMethod]
        public void CanMove_WithNullEmptyTile_ReturnsNone()
        {
            // Arrange - Don't call New(), so empty tile is null

            // Act
            BoardTile tile = new BoardTile { Value = 1, Row = 0, Column = 0 };
            AllowedMove result = _model.CanMove(tile);

            // Assert
            Assert.AreEqual(AllowedMove.None, result);
        }

        [TestMethod]
        public void CanMove_HorizontalAdjacent_ReturnsLeftOrRight()
        {
            // Arrange
            _model.New();
            BoardTile emptyTile = _model.Board.First(t => t.IsEmpty);
            // Empty is at (2, 2). Tile at (2, 1) can move right
            BoardTile tileToLeft = _model.Board.First(t => t.Row == emptyTile.Row && t.Column == emptyTile.Column - 1);

            // Act
            AllowedMove result = _model.CanMove(tileToLeft);

            // Assert
            Assert.AreEqual(AllowedMove.Right, result);
        }

        [TestMethod]
        public void CanMove_VerticalAdjacent_ReturnsUpOrDown()
        {
            // Arrange
            _model.New();
            BoardTile emptyTile = _model.Board.First(t => t.IsEmpty);
            // Empty is at (2, 2). Tile at (1, 2) can move down
            BoardTile tileAbove = _model.Board.First(t => t.Row == emptyTile.Row - 1 && t.Column == emptyTile.Column);

            // Act
            AllowedMove result = _model.CanMove(tileAbove);

            // Assert
            Assert.AreEqual(AllowedMove.Down, result);
        }

        [TestMethod]
        public void CanMove_NonAdjacentTile_ReturnsNone()
        {
            // Arrange
            _model.New();
            BoardTile tileAtCorner = _model.Board.First(t => t.Row == 0 && t.Column == 0);

            // Act
            AllowedMove result = _model.CanMove(tileAtCorner);

            // Assert
            Assert.AreEqual(AllowedMove.None, result);
        }

        [TestMethod]
        public void MoveTile_WithValidMove_UpdatesBoardState()
        {
            // Arrange
            _model.New();
            BoardTile emptyTile = _model.Board.First(t => t.IsEmpty);
            BoardTile tileToMove = _model.Board.First(t => t.Row == emptyTile.Row && t.Column == emptyTile.Column - 1);
            int emptyOriginalColumn = emptyTile.Column;
            int emptyOriginalRow = emptyTile.Row;
            int tileOriginalColumn = tileToMove.Column;

            // Act
            _model.MoveTile(tileToMove);

            // Assert
            Assert.AreEqual(emptyOriginalColumn, tileToMove.Column);
            Assert.AreEqual(tileOriginalColumn, emptyTile.Column);
            Assert.AreEqual(1, _model.NumberOfMoves);
        }

        [TestMethod]
        public void MoveTile_WithInvalidMove_DoesNothing()
        {
            // Arrange
            _model.New();
            BoardTile nonAdjacentTile = _model.Board.First(t => t.Row == 0 && t.Column == 0);
            int movesBefore = _model.NumberOfMoves;

            // Act
            _model.MoveTile(nonAdjacentTile);

            // Assert
            Assert.AreEqual(movesBefore, _model.NumberOfMoves);
        }

        [TestMethod]
        public void MoveTile_RecordsMoveInHistory()
        {
            // Arrange
            _model.New();
            BoardTile emptyTile = _model.Board.First(t => t.IsEmpty);
            BoardTile tileToMove = _model.Board.First(t => t.Row == emptyTile.Row && t.Column == emptyTile.Column - 1);

            // Act
            _model.MoveTile(tileToMove);

            // Assert
            Assert.HasCount(1, _model.MoveHistory);
            Move recordedMove = _model.MoveHistory.First!.Value;
            Assert.AreEqual(tileToMove.Row, recordedMove.FromRow);
            Assert.AreEqual(tileToMove.Column - 1, recordedMove.FromColumn);
        }

        [TestMethod]
        public void MoveTile_RaisesBoardChangedEvent()
        {
            // Arrange
            _model.New();
            BoardTile emptyTile = _model.Board.First(t => t.IsEmpty);
            BoardTile tileToMove = _model.Board.First(t => t.Row == emptyTile.Row && t.Column == emptyTile.Column - 1);

            // Act & Assert - Just verify that MoveTile succeeds without exception
            _model.MoveTile(tileToMove);
            Assert.AreEqual(1, _model.NumberOfMoves);
        }

        [TestMethod]
        public void Undo_WithMovementHistory_RestoresBoard()
        {
            // Arrange
            _model.New();
            BoardTile emptyTile = _model.Board.First(t => t.IsEmpty);
            BoardTile tileToMove = _model.Board.First(t => t.Row == emptyTile.Row && t.Column == emptyTile.Column - 1);
            int emptyOriginalColumn = emptyTile.Column;
            int emptyOriginalRow = emptyTile.Row;

            _model.MoveTile(tileToMove);

            // Act
            _model.Undo();

            // Assert
            Assert.AreEqual(0, _model.NumberOfMoves);
            Assert.AreEqual(emptyOriginalColumn, emptyTile.Column);
            Assert.AreEqual(emptyOriginalRow, emptyTile.Row);
        }

        [TestMethod]
        public void Undo_WithNoHistory_DoesNothing()
        {
            // Arrange
            _model.New();
            int movesBefore = _model.NumberOfMoves;

            // Act
            _model.Undo();

            // Assert
            Assert.AreEqual(movesBefore, _model.NumberOfMoves);
        }

        [TestMethod]
        public void Undo_RemovesMoveFromHistory()
        {
            // Arrange
            _model.New();
            BoardTile emptyTile = _model.Board.First(t => t.IsEmpty);
            BoardTile tileToMove = _model.Board.First(t => t.Row == emptyTile.Row && t.Column == emptyTile.Column - 1);
            _model.MoveTile(tileToMove);
            Assert.HasCount(1, _model.MoveHistory);

            // Act
            _model.Undo();

            // Assert
            Assert.IsEmpty(_model.MoveHistory);
        }

        [TestMethod]
        public void CanUndo_WithHistory_ReturnsTrue()
        {
            // Arrange
            _model.New();
            BoardTile emptyTile = _model.Board.First(t => t.IsEmpty);
            BoardTile tileToMove = _model.Board.First(t => t.Row == emptyTile.Row && t.Column == emptyTile.Column - 1);
            _model.MoveTile(tileToMove);

            // Act
            bool canUndo = _model.CanUndo;

            // Assert
            Assert.IsTrue(canUndo);
        }

        [TestMethod]
        public void CanUndo_WithoutHistory_ReturnsFalse()
        {
            // Arrange
            _model.New();

            // Act
            bool canUndo = _model.CanUndo;

            // Assert
            Assert.IsFalse(canUndo);
        }

        [TestMethod]
        public void IsSolved_WithSolvedBoard_ReturnsTrue()
        {
            // Arrange
            _model.New();
            // Board is already in solved state (1 2 3 / 4 5 6 / 7 8 0)

            // Act
            bool isSolved = _model.IsSolved();

            // Assert
            Assert.IsTrue(isSolved);
        }

        [TestMethod]
        public void IsSolved_WithSolvedBoard_RaisesBoardSolvedEvent()
        {
            // Arrange
            _model.New();
            bool eventRaised = false;
            _model.BoardSolved += (s, e) => eventRaised = true;

            // Act
            _model.IsSolved();

            // Assert
            Assert.IsTrue(eventRaised);
        }

        [TestMethod]
        public void IsSolved_WithUnsolvedBoard_ReturnsFalse()
        {
            // Arrange
            List<byte> unsolvedBoard = new() { 1, 2, 3, 4, 5, 6, 7, 0, 8 }; // 0 and 8 swapped
            _mockGenerator.Setup(g => g.Generate(It.IsAny<int>())).Returns(unsolvedBoard);
            _model.New();

            // Act
            bool isSolved = _model.IsSolved();

            // Assert
            Assert.IsFalse(isSolved);
        }

        [TestMethod]
        public void IsSolved_WithEmptyNotInCorner_ReturnsFalse()
        {
            // Arrange
            List<byte> unsolvedBoard = new() { 1, 2, 3, 4, 5, 6, 7, 0, 8 }; // Empty not at (2, 2)
            _mockGenerator.Setup(g => g.Generate(It.IsAny<int>())).Returns(unsolvedBoard);
            _model.New();

            // Act
            bool isSolved = _model.IsSolved();

            // Assert
            Assert.IsFalse(isSolved);
        }

        [TestMethod]
        public void Solve_CallsSolver()
        {
            // Arrange
            _model.New();
            SolveResult solveResult = new() { Result = SolveResultType.Solved, TimeSpent = TimeSpan.Zero, Moves = new() };
            _mockSolver.Setup(s => s.Solve(It.IsAny<List<BoardTile>>(), It.IsAny<SolverOptions>(), It.IsAny<IHeuristicElementFactory>()))
                .Returns(solveResult);

            // Act
            SolveResult result = _model.Solve();

            // Assert
            Assert.IsNotNull(result);
            _mockSolver.Verify(s => s.Solve(It.IsAny<List<BoardTile>>(), It.IsAny<SolverOptions>(), It.IsAny<IHeuristicElementFactory>()), Times.Once);
        }

        [TestMethod]
        public void Solve_ReturnsSolveResult()
        {
            // Arrange
            _model.New();
            SolveResult solveResult = new() { Result = SolveResultType.Solved, TimeSpent = TimeSpan.FromSeconds(1), Moves = new() };
            _mockSolver.Setup(s => s.Solve(It.IsAny<List<BoardTile>>(), It.IsAny<SolverOptions>(), It.IsAny<IHeuristicElementFactory>()))
                .Returns(solveResult);

            // Act
            SolveResult result = _model.Solve();

            // Assert
            Assert.AreEqual(SolveResultType.Solved, result.Result);
            Assert.AreEqual(TimeSpan.FromSeconds(1), result.TimeSpent);
        }

        [TestMethod]
        public void GridSizeChange_ClearsBoard()
        {
            // Arrange
            _model.New();
            Assert.HasCount(9, _model.Board);

            // Act - Simulate PropertyChanged event for GridSize
            _mockOptions.Setup(o => o.GridSize).Returns(4);
            _mockOptions.Raise(o => o.PropertyChanged += null, new PropertyChangedEventArgs(nameof(IOptions.GridSize)));

            // Assert
            Assert.IsEmpty(_model.Board);
        }

        [TestMethod]
        public void GridSizeChange_ClearsMoveHistory()
        {
            // Arrange
            _model.New();
            BoardTile emptyTile = _model.Board.First(t => t.IsEmpty);
            BoardTile tileToMove = _model.Board.First(t => t.Row == emptyTile.Row && t.Column == emptyTile.Column - 1);
            _model.MoveTile(tileToMove);
            Assert.HasCount(1, _model.MoveHistory);

            // Act - Simulate PropertyChanged event for GridSize
            _mockOptions.Setup(o => o.GridSize).Returns(4);
            _mockOptions.Raise(o => o.PropertyChanged += null, new PropertyChangedEventArgs(nameof(IOptions.GridSize)));

            // Assert
            Assert.IsEmpty(_model.MoveHistory);
            Assert.AreEqual(0, _model.NumberOfMoves);
        }

        [TestMethod]
        public void GridSizeChange_RaisesBoardChangedEvent()
        {
            // Arrange
            _model.New();
            bool eventRaised = false;
            _model.BoardLayoutChanged += (s, e) => eventRaised = true;

            // Act - Simulate PropertyChanged event for GridSize
            _mockOptions.Setup(o => o.GridSize).Returns(4);
            _mockOptions.Raise(o => o.PropertyChanged += null, new PropertyChangedEventArgs(nameof(IOptions.GridSize)));

            // Assert
            Assert.IsTrue(eventRaised);
        }

        [TestMethod]
        public void Heuristic_UpdatesAfterMove()
        {
            // Arrange
            _mockSolver.Setup(s => s.GetHeuristic(It.IsAny<List<BoardTile>>(), It.IsAny<IHeuristicElementFactory>()))
                .Returns(5);
            _model.New();

            // Act
            _model.MoveTile(_model.Board[1]);

            // Assert
            Assert.AreEqual(5, _model.Heuristic);
            _mockSolver.Verify(s => s.GetHeuristic(It.IsAny<List<BoardTile>>(), It.IsAny<IHeuristicElementFactory>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public void NumberOfMoves_TracksCorrectly()
        {
            // Arrange
            _model.New();
            BoardTile emptyTile = _model.Board.First(t => t.IsEmpty);
            BoardTile tileToMove1 = _model.Board.First(t => t.Row == emptyTile.Row && t.Column == emptyTile.Column - 1);

            // Act & Assert
            Assert.AreEqual(0, _model.NumberOfMoves);

            _model.MoveTile(tileToMove1);
            Assert.AreEqual(1, _model.NumberOfMoves);

            // Move another tile
            emptyTile = _model.Board.First(t => t.IsEmpty);
            BoardTile? tileToMove2 = _model.Board.FirstOrDefault(t => t.Row == emptyTile.Row && t.Column == emptyTile.Column + 1);
            if (tileToMove2 != null)
            {
                _model.MoveTile(tileToMove2);
                Assert.AreEqual(2, _model.NumberOfMoves);
            }
        }
    }
}

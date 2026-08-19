using Moq;
using Slider.Common.Interfaces;
using Slider.Interfaces;
using Slider.ViewModels;
using System.ComponentModel;

namespace UnitTest
{
    [TestClass]
    public sealed class TileControlViewModelTests
    {
        private Mock<IOptions> CreateOptionsMock()
        {
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(o => o.AnimationDelay).Returns(100);
            return optionsMock;
        }

        #region Constructor Tests

        [TestMethod]
        public void Constructor_InitializesWithBoardTile()
        {
            // Arrange
            BoardTile boardTile = new BoardTile{ Value = 5, Row = 1, Column = 2 };

            // Act
            TileControlViewModel viewModel = new(boardTile);

            // Assert
            Assert.AreEqual(boardTile, viewModel.BoardTile);
        }

        #endregion

        #region Value Property Tests

        [TestMethod]
        public void Value_ReturnsValueFromBoardTile()
        {
            // Arrange
            BoardTile boardTile = new BoardTile { Value = 42, Row = 0, Column = 0 };
            TileControlViewModel viewModel = new(boardTile);

            // Act
            int value = viewModel.Value;

            // Assert
            Assert.AreEqual(42, value);
        }

        [TestMethod]
        public void Value_WithZero_ReturnsZero()
        {
            // Arrange
            BoardTile boardTile = new BoardTile {Value = 0, Row = 0, Column = 0};
            TileControlViewModel viewModel = new(boardTile);

            // Act
            int value = viewModel.Value;

            // Assert
            Assert.AreEqual(0, value);
        }

        #endregion

        #region IsEmpty Property Tests

        [TestMethod]
        public void IsEmpty_WithZeroValue_ReturnsTrue()
        {
            // Arrange
            BoardTile boardTile = new BoardTile {Value = 0, Row = 0, Column = 0};
            TileControlViewModel viewModel = new(boardTile);

            // Act
            bool isEmpty = viewModel.IsEmpty;

            // Assert
            Assert.IsTrue(isEmpty);
        }

        [TestMethod]
        public void IsEmpty_WithNonZeroValue_ReturnsFalse()
        {
            // Arrange
            BoardTile boardTile = new BoardTile {Value = 5, Row = 0, Column = 0};
            TileControlViewModel viewModel = new(boardTile);

            // Act
            bool isEmpty = viewModel.IsEmpty;

            // Assert
            Assert.IsFalse(isEmpty);
        }

        #endregion

        #region IsHighlighted Property Tests

        [TestMethod]
        public void IsHighlighted_Get_ReturnsBoardTileValue()
        {
            // Arrange
            BoardTile boardTile = new BoardTile {Value = 5, Row = 0, Column = 0};
            boardTile.IsHighlighted = true;
            TileControlViewModel viewModel = new(boardTile);

            // Act
            bool isHighlighted = viewModel.IsHighlighted;

            // Assert
            Assert.IsTrue(isHighlighted);
        }

        [TestMethod]
        public void IsHighlighted_Set_UpdatesBoardTileAndRaisesPropertyChanged()
        {
            // Arrange
            BoardTile boardTile = new BoardTile {Value = 5, Row = 0, Column = 0};
            boardTile.IsHighlighted = false;
            TileControlViewModel viewModel = new(boardTile);
            bool propertyChangedRaised = false;
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TileControlViewModel.IsHighlighted))
                {
                    propertyChangedRaised = true;
                }
            };

            // Act
            viewModel.IsHighlighted = true;

            // Assert
            Assert.IsTrue(boardTile.IsHighlighted);
            Assert.IsTrue(propertyChangedRaised);
        }

        [TestMethod]
        public void IsHighlighted_Set_WithSameValue_DoesNotRaisePropertyChanged()
        {
            // Arrange
            BoardTile boardTile = new BoardTile {Value = 5, Row = 0, Column = 0};
            boardTile.IsHighlighted = true;
            TileControlViewModel viewModel = new(boardTile);
            bool propertyChangedRaised = false;
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TileControlViewModel.IsHighlighted))
                {
                    propertyChangedRaised = true;
                }
            };

            // Act
            viewModel.IsHighlighted = true;

            // Assert
            Assert.IsFalse(propertyChangedRaised);
        }

        #endregion

        #region X Property Tests

        [TestMethod]
        public void X_Get_ReturnsInitialValue()
        {
            // Arrange
            BoardTile boardTile = new BoardTile {Value = 5, Row = 0, Column = 0};
            TileControlViewModel viewModel = new(boardTile);

            // Act
            int x = viewModel.X;

            // Assert
            Assert.AreEqual(0, x);
        }

        [TestMethod]
        public void X_Set_UpdatesValueAndRaisesPropertyChanged()
        {
            // Arrange
            BoardTile boardTile = new BoardTile {Value = 5, Row = 0, Column = 0};
            TileControlViewModel viewModel = new(boardTile);
            bool propertyChangedRaised = false;
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TileControlViewModel.X))
                {
                    propertyChangedRaised = true;
                }
            };

            // Act
            viewModel.X = 100;

            // Assert
            Assert.AreEqual(100, viewModel.X);
            Assert.IsTrue(propertyChangedRaised);
        }

        [TestMethod]
        public void X_Set_WithSameValue_DoesNotRaisePropertyChanged()
        {
            // Arrange
            BoardTile boardTile = new BoardTile {Value = 5, Row = 0, Column = 0};
            TileControlViewModel viewModel = new(boardTile);
            viewModel.X = 50;
            bool propertyChangedRaised = false;
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TileControlViewModel.X))
                {
                    propertyChangedRaised = true;
                }
            };

            // Act
            viewModel.X = 50;

            // Assert
            Assert.IsFalse(propertyChangedRaised);
        }

        #endregion

        #region Y Property Tests

        [TestMethod]
        public void Y_Get_ReturnsInitialValue()
        {
            // Arrange
            BoardTile boardTile = new BoardTile {Value = 5, Row = 0, Column = 0};
            TileControlViewModel viewModel = new(boardTile);

            // Act
            int y = viewModel.Y;

            // Assert
            Assert.AreEqual(0, y);
        }

        [TestMethod]
        public void Y_Set_UpdatesValueAndRaisesPropertyChanged()
        {
            // Arrange
            BoardTile boardTile = new BoardTile {Value = 5, Row = 0, Column = 0};
            TileControlViewModel viewModel = new(boardTile);
            bool propertyChangedRaised = false;
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TileControlViewModel.Y))
                {
                    propertyChangedRaised = true;
                }
            };

            // Act
            viewModel.Y = 200;

            // Assert
            Assert.AreEqual(200, viewModel.Y);
            Assert.IsTrue(propertyChangedRaised);
        }

        [TestMethod]
        public void Y_Set_WithSameValue_DoesNotRaisePropertyChanged()
        {
            // Arrange
            BoardTile boardTile = new BoardTile {Value = 5, Row = 0, Column = 0};
            TileControlViewModel viewModel = new(boardTile);
            viewModel.Y = 75;
            bool propertyChangedRaised = false;
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TileControlViewModel.Y))
                {
                    propertyChangedRaised = true;
                }
            };

            // Act
            viewModel.Y = 75;

            // Assert
            Assert.IsFalse(propertyChangedRaised);
        }

        #endregion

        #region Row Property Tests

        [TestMethod]
        public void Row_Get_ReturnsBoardTileRow()
        {
            // Arrange
            BoardTile boardTile = new BoardTile { Value = 5, Row = 2, Column = 3 };
            TileControlViewModel viewModel = new(boardTile);

            // Act
            int row = viewModel.Row;

            // Assert
            Assert.AreEqual(2, row);
        }

        [TestMethod]
        public void Row_Set_UpdatesBoardTileRow()
        {
            // Arrange
            BoardTile boardTile = new BoardTile {Value = 5, Row = 0, Column = 0};
            TileControlViewModel viewModel = new(boardTile);

            // Act
            viewModel.Row = 3;

            // Assert
            Assert.AreEqual(3, boardTile.Row);
        }

        #endregion

        #region Column Property Tests

        [TestMethod]
        public void Column_Get_ReturnsBoardTileColumn()
        {
            // Arrange
            BoardTile boardTile = new BoardTile { Value = 5, Row = 2, Column = 3 };
            TileControlViewModel viewModel = new(boardTile);

            // Act
            int column = viewModel.Column;

            // Assert
            Assert.AreEqual(3, column);
        }

        [TestMethod]
        public void Column_Set_UpdatesBoardTileColumn()
        {
            // Arrange
            BoardTile boardTile = new BoardTile {Value = 5, Row = 0, Column = 0};
            TileControlViewModel viewModel = new(boardTile);

            // Act
            viewModel.Column = 2;

            // Assert
            Assert.AreEqual(2, boardTile.Column);
        }

        #endregion

        #region TileSize Property Tests

        [TestMethod]
        public void TileSize_Get_ReturnsInitialValue()
        {
            // Arrange
            BoardTile boardTile = new BoardTile {Value = 5, Row = 0, Column = 0};
            TileControlViewModel viewModel = new(boardTile);

            // Act
            int tileSize = viewModel.TileSize;

            // Assert
            Assert.AreEqual(0, tileSize);
        }

        [TestMethod]
        public void TileSize_Set_UpdatesValueAndRaisesPropertyChanged()
        {
            // Arrange
            BoardTile boardTile = new BoardTile {Value = 5, Row = 0, Column = 0};
            TileControlViewModel viewModel = new(boardTile);
            bool propertyChangedRaised = false;
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TileControlViewModel.TileSize))
                {
                    propertyChangedRaised = true;
                }
            };

            // Act
            viewModel.TileSize = 150;

            // Assert
            Assert.AreEqual(150, viewModel.TileSize);
            Assert.IsTrue(propertyChangedRaised);
        }

        [TestMethod]
        public void TileSize_Set_WithSameValue_DoesNotRaisePropertyChanged()
        {
            // Arrange
            BoardTile boardTile = new BoardTile {Value = 5, Row = 0, Column = 0};
            TileControlViewModel viewModel = new(boardTile);
            viewModel.TileSize = 100;
            bool propertyChangedRaised = false;
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TileControlViewModel.TileSize))
                {
                    propertyChangedRaised = true;
                }
            };

            // Act
            viewModel.TileSize = 100;

            // Assert
            Assert.IsFalse(propertyChangedRaised);
        }

        #endregion

        #region PropertyChanged Event Tests

        [TestMethod]
        public void PropertyChanged_IsRaisedWhenXChanges()
        {
            // Arrange
            BoardTile boardTile = new BoardTile {Value = 5, Row = 0, Column = 0};
            TileControlViewModel viewModel = new(boardTile);
            List<string> changedProperties = new();
            viewModel.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName ?? "");

            // Act
            viewModel.X = 50;
            viewModel.Y = 60;
            viewModel.TileSize = 100;

            // Assert
            Assert.Contains(nameof(TileControlViewModel.X), changedProperties);
            Assert.Contains(nameof(TileControlViewModel.Y), changedProperties);
            Assert.Contains(nameof(TileControlViewModel.TileSize), changedProperties);
        }

        [TestMethod]
        public void PropertyChanged_ImplementsINotifyPropertyChanged()
        {
            // Arrange
            BoardTile boardTile = new BoardTile {Value = 5, Row = 0, Column = 0};
            TileControlViewModel viewModel = new(boardTile);

            // Assert
            Assert.IsInstanceOfType(viewModel, typeof(INotifyPropertyChanged));
        }

        #endregion

        #region Integration Tests

        [TestMethod]
        public void MultiplePropertyChanges_TrackAllChanges()
        {
            // Arrange
            BoardTile boardTile = new BoardTile {Value = 5, Row = 0, Column = 0};
            TileControlViewModel viewModel = new(boardTile);
            Dictionary<string, int> propertyChanges = new();
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != null)
                {
                    if (propertyChanges.ContainsKey(e.PropertyName))
                        propertyChanges[e.PropertyName]++;
                    else
                        propertyChanges[e.PropertyName] = 1;
                }
            };

            // Act
            viewModel.X = 10;
            viewModel.X = 20;
            viewModel.Y = 30;
            viewModel.IsHighlighted = true;
            viewModel.IsHighlighted = false;

            // Assert
            Assert.AreEqual(2, propertyChanges[nameof(TileControlViewModel.X)]);
            Assert.AreEqual(1, propertyChanges[nameof(TileControlViewModel.Y)]);
            Assert.AreEqual(2, propertyChanges[nameof(TileControlViewModel.IsHighlighted)]);
        }
        #endregion
    }
}

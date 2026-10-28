using Slider;
using Slider.Common.Interfaces;
using System.Collections.Generic;
using System.Reflection;

namespace UnitTest
{
    [TestClass]
    public sealed class PuzzleGeneratorTests
    {
        #region Support methods
        private int CountAdjacentTiles(PuzzleGenerator generator, GeneratedTile[] board, int row, int col)
        {
            int gridSize = (int)(Math.Sqrt(board.Length));
            MethodInfo? method = typeof(PuzzleGenerator).GetMethod("CountMovableAdjacentTiles", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null)
            {
                Assert.Fail("The private method 'CountMovableAdjacentTiles' was not found.");
            }
            int result = -1;
            object? resultObj = method.Invoke(generator, [board, gridSize, row, col]);
            if (resultObj != null)
            {
                result = (int)resultObj;
            }
            return result;
        }

        private bool IsSolvableManual(GeneratedTile[] board, int gridSize)
        {
            int emptyPos = 0;
            int inversions = 0;
            for (int i = 0; i < board.Length; i++)
            {
                for (int j = i + 1; j < board.Length; j++)
                {
                    if (board[i].TileValue > board[j].TileValue && board[i].TileValue != 0 && board[j].TileValue != 0)
                    {
                        inversions++;
                    }
                }
                if (board[i].TileValue == 0)
                    emptyPos = i;
            }

            if (gridSize % 2 == 1)
            {
                return inversions % 2 == 0;
            }
            else
            {
                int blankRowFromBottom = gridSize - (emptyPos / gridSize);
                return (blankRowFromBottom % 2 == 0) == (inversions % 2 == 1);
            }
        }
        #endregion

        [TestMethod]
        public void Generate_Returns_Correct_Tile_Count()
        {
            // Arrange
            int gridSize = 3;
            int expectedCount = gridSize * gridSize;
            PuzzleGenerator generator = new();
            // Act
            var result = generator.Generate(gridSize, 0);

            // Assert
            Assert.HasCount(expectedCount, result);
        }

        [TestMethod]
        public void Generate_Returns_Valid_Permutation()
        {
            // Arrange
            int gridSize = 3;
            int expectedCount = gridSize * gridSize;
            PuzzleGenerator generator = new();

            // Act
            GeneratedTile[] result = generator.Generate(gridSize, 0);

            // Assert
            Assert.HasCount(expectedCount, result);
            HashSet<int> seen = new();
            foreach (var tile in result)
            {
                Assert.IsTrue(tile.TileValue >= 0 && tile.TileValue < expectedCount, $"Tile {tile} is out of valid range [0, {expectedCount - 1}]");
                Assert.IsTrue(seen.Add(tile.TileValue), $"Tile {tile} appears more than once");
            }
        }

        [TestMethod]
        public void Generate_3x3_Returns_Solvable_Configuration()
        {
            // Arrange
            int gridSize = 3;
            PuzzleGenerator generator = new();

            // Act
            GeneratedTile[] result = generator.Generate(gridSize, 0);

            // Assert - verify the configuration is solvable by checking inversion parity
            Assert.IsTrue(IsSolvableManual(result, gridSize));
        }

        [TestMethod]
        public void Generate_4x4_Returns_Solvable_Configuration()
        {
            // Arrange
            int gridSize = 4;
            PuzzleGenerator generator = new();

            // Act
            var result = generator.Generate(gridSize, 0);

            // Assert
            Assert.IsTrue(IsSolvableManual(result, gridSize));
        }

        [TestMethod]
        public void Generate_2x2_Returns_Solvable_Configuration()
        {
            // Arrange
            int gridSize = 2;
            PuzzleGenerator generator = new();

            // Act
            var result = generator.Generate(gridSize, 0);

            // Assert
            Assert.IsTrue(IsSolvableManual(result, gridSize));
        }

        [TestMethod]
        public void Generate_5x5_Returns_Solvable_Configuration()
        {
            // Arrange
            int gridSize = 5;
            PuzzleGenerator generator = new();

            // Act
            var result = generator.Generate(gridSize, 0);

            // Assert
            Assert.IsTrue(IsSolvableManual(result, gridSize));
        }

        [TestMethod]
        public void Generate_Multiple_Calls_Produce_Different_Results()
        {
            // Arrange
            int gridSize = 3;
            List<GeneratedTile[]> results = new();
            PuzzleGenerator generator = new();

            // Act - generate multiple puzzles
            for (int i = 0; i < 10; i++)
            {
                results.Add(generator.Generate(gridSize, 0));
            }

            // Assert - at least some should be different (with high probability)
            bool hasDifference = false;
            for (int i = 0; i < results.Count - 1; i++)
            {
                if (!Enumerable.SequenceEqual(results[i].Select(p=>p.TileValue), results[i + 1].Select(p=>p.TileValue)))
                {
                    hasDifference = true;
                    break;
                }
            }

            Assert.IsTrue(hasDifference, "Generated puzzles should not all be identical");
        }

        [TestMethod]
        public void CountMovableAdjacentTiles_Should_Count_Correctly_ForCorners()
        {
            PuzzleGenerator generator = new();
            GeneratedTile[] board = new GeneratedTile[16];
            for (int i = 0; i < board.Length; i++)
                board[i] = new GeneratedTile { TileValue = (byte)i, IsLocked = false };

            int result = CountAdjacentTiles(generator, board, 0, 0);
            Assert.AreEqual(2, result);
            result = CountAdjacentTiles(generator, board, 0, 3);
            Assert.AreEqual(2, result);
            result = CountAdjacentTiles(generator, board, 3, 0);
            Assert.AreEqual(2, result);
            result = CountAdjacentTiles(generator, board, 3, 3);
            Assert.AreEqual(2, result);
        }
        [TestMethod]
        public void CountMovableAdjacentTiles_Should_Count_Correctly_ForEdges()
        {
            PuzzleGenerator generator = new();
            GeneratedTile[] board = new GeneratedTile[16];
            for (int i = 0; i < board.Length; i++)
                board[i] = new GeneratedTile { TileValue = (byte)i, IsLocked = false };

            int result;
            for (int i = 1; i < 3; i++)
            {
                result = CountAdjacentTiles(generator, board, 0, i);
                Assert.AreEqual(3, result);
                result = CountAdjacentTiles(generator, board, 3, i);
                Assert.AreEqual(3, result);
                result = CountAdjacentTiles(generator, board, i, 0);
                Assert.AreEqual(3, result);
                result = CountAdjacentTiles(generator, board, i, 3);
                Assert.AreEqual(3, result);
            }
        }

        [TestMethod]
        public void CountMovableAdjacentTiles_Should_Count_Correctly_ForCenterTiles()
        {
            PuzzleGenerator generator = new();
            GeneratedTile[] board = new GeneratedTile[16];
            for (int i = 0; i < board.Length; i++)
                board[i] = new GeneratedTile { TileValue = (byte)i, IsLocked = false };

            int result;
            for (int i = 1; i < 3; i++)
            {
                result = CountAdjacentTiles(generator, board, 1, i);
                Assert.AreEqual(4, result);
                result = CountAdjacentTiles(generator, board, 2, i);
                Assert.AreEqual(4, result);
            }
        }
        [TestMethod]
        public void CountMovableAdjacentTiles_Should_Count_Correctly_ForLockedTiles()
        {
            PuzzleGenerator generator = new();
            GeneratedTile[] board = new GeneratedTile[16];
            for (int i = 0; i < board.Length; i++)
                board[i] = new GeneratedTile { TileValue = (byte)i, IsLocked = false };

            // Lock a tile adjacent to each corner, along an edge. 
            // Check the corner itself, a tile along the edge, and a center til adjacent to the locked tile
            // Board layout
            // x L x x
            // x x x L
            // L x x x
            // x x L x
            board[1].IsLocked = true;
            board[7].IsLocked = true;
            board[8].IsLocked = true;
            board[14].IsLocked = true;
            int result;
            result = CountAdjacentTiles(generator, board, 0, 0);
            Assert.AreEqual(1, result);
            result = CountAdjacentTiles(generator, board, 0, 2);
            Assert.AreEqual(2, result);
            result = CountAdjacentTiles(generator, board, 0, 3);
            Assert.AreEqual(1, result);
            result = CountAdjacentTiles(generator, board, 1, 0);
            Assert.AreEqual(2, result);
            result = CountAdjacentTiles(generator, board, 1, 1);
            Assert.AreEqual(3, result);
            result = CountAdjacentTiles(generator, board, 1, 2);
            Assert.AreEqual(3, result);
            result = CountAdjacentTiles(generator, board, 2, 1);
            Assert.AreEqual(3, result);
            result = CountAdjacentTiles(generator, board, 2, 2);
            Assert.AreEqual(3, result);
            result = CountAdjacentTiles(generator, board, 3, 0);
            Assert.AreEqual(1, result);
            result = CountAdjacentTiles(generator, board, 3, 1);
            Assert.AreEqual(2, result);
            result = CountAdjacentTiles(generator, board, 3, 3);
            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public void LockingTileIn3x3_MustLockValidPosition()
        {
            // Valid locked tiles in a 3x3 are the 3 corners (lock can't be done on the final position of the blank) + the center tile
            // So generate a large number of boards and verify that the centers of the edges are never chosen
            for (int boardCount = 0; boardCount < 1000; boardCount++)
            {
                PuzzleGenerator generator = new();
                GeneratedTile[] board = generator.Generate(3, 1);

                Assert.AreEqual(1, board.Count(p => p.IsLocked));
                int index = -1;
                for (int i = 0; i < board.Length; i++)
                {
                    if (board[i].IsLocked)
                    {
                        index = i;
                        break;
                    }
                }
                bool expected = index != 1 && index != 3 && index != 5 && index != 7 && index != 8;
                Assert.IsTrue(expected);
            }
        }
    }
}

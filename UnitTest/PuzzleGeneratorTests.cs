using Slider;
using System.Collections.Generic;

namespace UnitTest
{
    [TestClass]
    public sealed class PuzzleGeneratorTests
    {
        [TestMethod]
        public void Generate_Returns_Correct_Tile_Count()
        {
            // Arrange
            int gridSize = 3;
            int expectedCount = gridSize * gridSize;
            PuzzleGenerator generator = new();
            // Act
            var result = generator.Generate(gridSize);

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
            var result = generator.Generate(gridSize);

            // Assert
            Assert.HasCount(expectedCount, result);
            var seen = new HashSet<int>();
            foreach (var tile in result)
            {
                Assert.IsTrue(tile >= 0 && tile < expectedCount, $"Tile {tile} is out of valid range [0, {expectedCount - 1}]");
                Assert.IsTrue(seen.Add(tile), $"Tile {tile} appears more than once");
            }
        }

        [TestMethod]
        public void Generate_3x3_Returns_Solvable_Configuration()
        {
            // Arrange
            int gridSize = 3;
            PuzzleGenerator generator = new();

            // Act
            byte[] result = generator.Generate(gridSize);

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
            var result = generator.Generate(gridSize);

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
            var result = generator.Generate(gridSize);

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
            var result = generator.Generate(gridSize);

            // Assert
            Assert.IsTrue(IsSolvableManual(result, gridSize));
        }

        [TestMethod]
        public void Generate_Multiple_Calls_Produce_Different_Results()
        {
            // Arrange
            int gridSize = 3;
            var results = new List<byte[]>();
            PuzzleGenerator generator = new();

            // Act - generate multiple puzzles
            for (int i = 0; i < 10; i++)
            {
                results.Add(generator.Generate(gridSize));
            }

            // Assert - at least some should be different (with high probability)
            bool hasDifference = false;
            for (int i = 0; i < results.Count - 1; i++)
            {
                if (!Enumerable.SequenceEqual(results[i], results[i + 1]))
                {
                    hasDifference = true;
                    break;
                }
            }

            Assert.IsTrue(hasDifference, "Generated puzzles should not all be identical");
        }

        private bool IsSolvableManual(byte[] board, int gridSize)
        {
            int emptyPos = 0;
            int inversions = 0;
            for (int i = 0; i < board.Length; i++)
            {
                for (int j = i + 1; j < board.Length; j++)
                {
                    if (board[i] > board[j] && board[i] != 0 && board[j] != 0)
                    {
                        inversions++;
                    }
                }
                if (board[i] == 0)
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
    }
}

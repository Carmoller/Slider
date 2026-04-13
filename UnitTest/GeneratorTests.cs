using Slider;
using System.Collections.Generic;

namespace UnitTest
{
    [TestClass]
    public sealed class GeneratorTests
    {
        [TestMethod]
        public void Generate_Returns_Correct_Tile_Count()
        {
            // Arrange
            int gridSize = 3;
            int expectedCount = gridSize * gridSize;
            Generator generator = new();
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
            Generator generator = new();

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
            Generator generator = new();

            // Act
            var result = generator.Generate(gridSize);

            // Assert - verify the configuration is solvable by checking inversion parity
            Assert.IsTrue(IsSolvableManual(result, gridSize));
        }

        [TestMethod]
        public void Generate_4x4_Returns_Solvable_Configuration()
        {
            // Arrange
            int gridSize = 4;
            Generator generator = new();

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
            Generator generator = new();

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
            Generator generator = new();

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
            var results = new List<List<int>>();
            Generator generator = new();

            // Act - generate multiple puzzles
            for (int i = 0; i < 10; i++)
            {
                results.Add(generator.Generate(gridSize));
            }

            // Assert - at least some should be different (with high probability)
            bool hasDifference = false;
            for (int i = 0; i < results.Count - 1; i++)
            {
                if (!ListsEqual(results[i], results[i + 1]))
                {
                    hasDifference = true;
                    break;
                }
            }

            Assert.IsTrue(hasDifference, "Generated puzzles should not all be identical");
        }

        private bool IsSolvableManual(List<int> tiles, int gridSize)
        {
            int inversions = 0;
            for (int i = 0; i < tiles.Count; i++)
            {
                for (int j = i + 1; j < tiles.Count; j++)
                {
                    if (tiles[i] > tiles[j] && tiles[i] != 0 && tiles[j] != 0)
                    {
                        inversions++;
                    }
                }
            }

            if (gridSize % 2 == 1)
            {
                return inversions % 2 == 0;
            }
            else
            {
                int blankRowFromBottom = gridSize - (tiles.IndexOf(0) / gridSize);
                return (blankRowFromBottom % 2 == 0) == (inversions % 2 == 1);
            }
        }

        private bool ListsEqual(List<int> list1, List<int> list2)
        {
            if (list1.Count != list2.Count)
                return false;

            for (int i = 0; i < list1.Count; i++)
            {
                if (list1[i] != list2[i])
                    return false;
            }

            return true;
        }
    }
}

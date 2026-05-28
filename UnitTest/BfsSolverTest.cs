using Slider.BfsSolver;
using Slider.Interfaces;
using Slider.Solver;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTest
{
    [TestClass]
    public class BfsSolverTest
    {
        [TestMethod]
        public void BfsSolverTest_TilePosition()
        {
            // Making sure that TilePosition can be used as a key in a dictionary and that equality works as expected
            TilePosition pos1 = new TilePosition { Row = 0, Col = 0 };
            TilePosition pos2 = new TilePosition { Row = 0, Col = 0 };

            Dictionary<TilePosition, string> dict = new();
            dict[pos1] = "Tile 1";

            Assert.IsTrue(dict.ContainsKey(pos2));
            Assert.AreEqual("Tile 1", dict[pos2]);

            Assert.IsTrue(pos1 == pos2);
        }
        [TestMethod]
        public void BfsSolverTest_SolveTopRow_3x3_1()
        {
            byte[,] board = new byte[,]
            {
                { 4, 5, 6 },
                { 1, 2, 3 },
                { 7, 8, 0 }
            };
            BfsSolver solver = new(board);
            solver.SolveTopRow();
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, Enumerable.Range(0, board.GetLength(1)).Select(i => board[0, i]).ToArray());
        }
        [TestMethod]
        public void BfsSolverTest_SolveTopRow_3x3_Train1()
        {
            byte[,] board = new byte[,]
            {
                { 0, 5, 6 },
                { 1, 2, 3 },
                { 7, 8, 4 }
            };
            BfsSolver solver = new(board);
            solver.SolveTopRowTrain();
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, Enumerable.Range(0, board.GetLength(1)).Select(i => board[0, i]).ToArray());
        }
        [TestMethod]
        public void BfsSolverTest_SolveTopRow_3x3_2()
        {
            byte[,] board = new byte[,]
            {
                { 4, 5, 3 },
                { 1, 2, 6 },
                { 7, 8, 0 }
            };
            BfsSolver solver = new(board);
            solver.SolveTopRow();
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, Enumerable.Range(0, board.GetLength(1)).Select(i => board[0, i]).ToArray());
        }

        [TestMethod]
        public void BfsSolverTest_SolveTopRow_4x4_1()
        {
            byte[,] board = new byte[,]
            {
                { 6, 15, 7, 5 },
                { 1, 2, 14, 9 },
                { 3, 4, 8, 0 },
                { 10, 11, 12, 13 }
            };
            BfsSolver solver = new(board);
            solver.SolveTopRow();
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, Enumerable.Range(0, board.GetLength(1)).Select(i => board[0, i]).ToArray());
        }
        [TestMethod]
        public void BfsSolverTest_SolveLeftCol_3x3_Train1()
        {
            byte[,] board = new byte[,]
            {
                { 0, 1, 6 },
                { 5, 2, 7 },
                { 3, 8, 4 }
            };
            BfsSolver solver = new(board);
            solver.SolveLeftColumnTrain();
            CollectionAssert.AreEqual(new byte[] { 1, 4, 7 }, Enumerable.Range(0, board.GetLength(1)).Select(i => board[i,0]).ToArray());
        }
        [TestMethod]
        public void BfsSolverTest_SolveTopRowAndLeftCol_4x4_Train()
        {
            byte[,] board = new byte[,]
            {
                { 6, 15, 7, 5 },
                { 1, 2, 14, 9 },
                { 3, 4, 8, 0 },
                { 10, 11, 12, 13 }
            }; BfsSolver solver = new(board);
            solver.SolveTopRowTrain();
            solver.SolveLeftColumnTrain();
            CollectionAssert.AreEqual(new byte[] { 1, 5, 9, 13 }, Enumerable.Range(0, board.GetLength(1)).Select(i => board[i, 0]).ToArray());
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, Enumerable.Range(0, board.GetLength(1)).Select(i => board[0, i]).ToArray());
        }

        [TestMethod]
        public void BfsSolverTest_SolveTopRowAndLeftCol_4x4_AStar()
        {
            byte[,] board = new byte[,]
            {
                { 6, 15, 7, 5 },
                { 1, 2, 14, 9 },
                { 3, 4, 8, 0 },
                { 10, 11, 12, 13 }
            };
            SolverIDAStarPlus aStarSolver = new();

            BfsSolver solver = new(board);
            solver.SolveTopRowTrain();
            solver.SolveLeftColumnTrain();
            CollectionAssert.AreEqual(new byte[] { 1, 5, 9, 13 }, Enumerable.Range(0, board.GetLength(1)).Select(i => board[i, 0]).ToArray());
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, Enumerable.Range(0, board.GetLength(1)).Select(i => board[0, i]).ToArray());
        }

    }
}

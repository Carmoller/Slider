using PDBGenerator;
using Slider.BfsSolver;
using Slider.Heuristics;
using Slider.Interfaces;
using Slider.Solver;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
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
            SolveResult result = solver.SolveTopRow();
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
            SolveResult resulRow = solver.SolveTopRowTrain();
            SolveResult resultColumn = solver.SolveLeftColumnTrain();
            CollectionAssert.AreEqual(new byte[] { 1, 5, 9, 13 }, Enumerable.Range(0, board.GetLength(1)).Select(i => board[i, 0]).ToArray());
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, Enumerable.Range(0, board.GetLength(1)).Select(i => board[0, i]).ToArray());
        }

        [TestMethod]
        public void BfsSolverTest_SolveTopRowAndLeftCol_4x4_AStar()
        {
            List<BoardTile> board = new();
            board.Add(new BoardTile{ Value=6, Row=0, Column=0});
            board.Add(new BoardTile { Value = 15, Row = 0, Column = 1 });
            board.Add(new BoardTile { Value = 7, Row = 0, Column = 2 });
            board.Add(new BoardTile { Value = 5, Row = 0, Column = 3 });
            board.Add(new BoardTile { Value = 1, Row = 1, Column = 0 });
            board.Add(new BoardTile { Value = 2, Row = 1, Column = 1 });
            board.Add(new BoardTile { Value = 14, Row = 1, Column = 2 });
            board.Add(new BoardTile { Value = 9, Row = 1, Column = 3 });
            board.Add(new BoardTile { Value = 3, Row = 2, Column = 0 });
            board.Add(new BoardTile { Value = 4, Row = 2, Column = 1 });
            board.Add(new BoardTile { Value = 8, Row = 2, Column = 2 });
            board.Add(new BoardTile { Value = 0, Row = 2, Column = 3 });
            board.Add(new BoardTile { Value = 10, Row = 3, Column = 0 });
            board.Add(new BoardTile { Value = 11, Row = 3, Column = 1 });
            board.Add(new BoardTile { Value = 12, Row = 3, Column = 2 });
            board.Add(new BoardTile { Value = 13, Row = 3, Column = 3 });
            int gridSize = (int)Math.Sqrt(board.Count);

            BfsSolver solver = new(board);
            SolveResult resultRow = solver.SolveTopRowTrain();
            SolveResult resultColumn = solver.SolveLeftColumnTrain();
            CollectionAssert.AreEqual(new byte[] { 1, 5, 9, 13 }, Enumerable.Range(0, gridSize).Select(i => board.Where(p=>p.Row==0 && p.Column==i).First().Value).ToArray());
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, Enumerable.Range(0, gridSize).Select(i => board.Where(p => p.Row == i && p.Column == 0).First().Value).ToArray());
        }
    }
}

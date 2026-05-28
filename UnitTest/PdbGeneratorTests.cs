using PDBGenerator;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace UnitTest
{
    [TestClass]
    public class PdbGeneratorTests
    {
        private class Result
        {
            public byte[]? Board { get; set; }
            public int Distance { get; set; }
        }

        private readonly Result[] AllLegalBoards = 
        [
           new Result {Board = [0, 1, 3, 2], Distance = 2},
           new Result {Board = [0, 2, 1, 3], Distance = 2},
           new Result {Board = [0, 3, 2, 1], Distance = 6},
           new Result {Board = [1, 0, 3, 2], Distance = 1},
           new Result {Board = [1, 2, 0, 3], Distance = 1},
           new Result {Board = [1, 2, 3, 0], Distance = 0},
           new Result {Board = [2, 0, 1, 3], Distance = 3},
           new Result {Board = [2, 3, 0, 1], Distance = 5},
           new Result {Board = [2, 3, 1, 0], Distance = 4},
           new Result {Board = [3, 0, 2, 1], Distance = 5},
           new Result {Board = [3, 1, 0, 2], Distance = 3},
           new Result {Board = [3, 1, 2, 0], Distance = 4}
        ];

        [TestMethod]
        public void Test_GeneratorCompleteness_Mem()
        {
            // Test that the generator considers all legal states
            Generator gen = new(2, 3);
            PatternDatabase db = gen.GeneratePdb(new Generator.PatternState
            {
                TilePositions = [0, 1, 2],
                BlankPosition = 3
            });
            Codec codec = new(2, 3);

            int[] trackedTiles = [1, 2, 3];
            PdbHelper helper = new(trackedTiles);

            // All reachable permutations of a 2x2 sliding puzzle (0 = blank)
            foreach (Result result in AllLegalBoards)
            {
                long index = helper.EncodeCurrentState(result.Board!, codec);
                byte distance = db.GetDistance(index);

                string boardText = "(";
                boardText += string.Join(",", result.Board!);
                boardText += ")";
                Assert.AreEqual(result.Distance, distance, $"Board {boardText}");
            }
        }

        [TestMethod]
        public void Test_GeneratorCompleteness_File()
        {
            // Test that the generator considers all legal states
            Generator gen = new(2, 3, true);
            PatternDatabase db = gen.GeneratePdb(new Generator.PatternState
            {
                TilePositions = [0, 1, 2],
                BlankPosition = 3
            });
            Codec codec = new(2, 3);

            int[] trackedTiles = [1, 2, 3];
            PdbHelper helper = new(trackedTiles);

            // All reachable permutations of a 2x2 sliding puzzle (0 = blank)
            foreach (Result result in AllLegalBoards)
            {
                long index = helper.EncodeCurrentState(result.Board!, codec);
                byte distance = db.GetDistance(index);

                string boardText = "(";
                boardText += string.Join(",", result.Board!);
                boardText += ")";
                Assert.AreEqual(result.Distance, distance, $"Board {boardText}");
            }
        }

        [TestMethod]
        public void PdbGeneratorPerformance3x3()
        {
            byte boardSize =4;
            byte k = 6;
            byte[] trackedTiles = new byte[k];
            long factor = boardSize * boardSize;
            long numberOfStates = 1;

            for (int i=0; i<k; i++)
            {
                numberOfStates*= factor;
                trackedTiles[i] = (byte)(i + 1);
                factor--;
            }
            // NumberOfStates should also consider the blank
            numberOfStates *= factor;
            Generator gen = new(boardSize, k, false);
            PatternDatabase db = gen.GeneratePdb(new Generator.PatternState
            {
                TilePositions = trackedTiles,
                BlankPosition = (byte)((boardSize * boardSize) - 1)
            });
            Assert.IsGreaterThan(1, gen.StatesProcessed);
            Assert.AreEqual(numberOfStates, gen.StatesProcessed);
            Console.WriteLine($"Generated {boardSize}-tile PDB in {gen.ElapsedMs} ms, processed {gen.StatesProcessed} states");
            Console.WriteLine("States per second: " + (gen.StatesProcessed / (gen.ElapsedMs / 1000.0)));
        }
    }
}

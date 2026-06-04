using Mono.Cecil.Cil;
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
            PdbGenerator gen = new(2, 3);
            PatternDatabase db = gen.GeneratePdb(new PdbGenerator.PatternState
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
            PdbGenerator gen = new(2, 3, true);
            PatternDatabase db = gen.GeneratePdb(new PdbGenerator.PatternState
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
            byte k = 4;
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
            PdbGenerator gen = new(boardSize, k, false);
            PatternDatabase db = gen.GeneratePdb(new PdbGenerator.PatternState
            {
                TilePositions = trackedTiles,
                BlankPosition = (byte)((boardSize * boardSize) - 1)
            });
            Assert.IsGreaterThan(1, gen.StatesProcessed);
            Assert.AreEqual(numberOfStates, gen.StatesProcessed);
            Console.WriteLine($"Generated {boardSize}-tile PDB in {gen.ElapsedMs} ms, processed {gen.StatesProcessed} states");
            Console.WriteLine("States per second: " + (gen.StatesProcessed / (gen.ElapsedMs / 1000.0)));
        }

        [TestMethod]
        public void Test_Generator_LoadAndSave_YieldsSameByteArray()
        {
            PdbGenerator gen = new(4, 3);
            PatternDatabase db = gen.GeneratePdb(new PdbGenerator.PatternState
            {
                TilePositions = [12, 13, 14],
                BlankPosition = 15
            });
            string tempFile = Path.GetTempFileName();
            db.SaveToFile(tempFile);

            PatternDatabase? loadedDb = PatternDatabase.LoadFromFile(tempFile);
            File.Delete(tempFile);

            Assert.IsNotNull(loadedDb);
            Assert.AreEqual(3, loadedDb.K);

            Codec codec = new(4, 3);
            long index = codec.Encode(new byte[] { 2, 0, 13 }, 7);
            Assert.AreEqual(27591, index);
            byte distance1 = db.GetDistance(index);
            byte distance2 = loadedDb.GetDistance(index);
            Console.WriteLine($"Distance from original db: {distance1}, distance from loaded db: {distance2}");
            Assert.AreEqual(distance1, distance2);

            Assert.IsNotNull(loadedDb);
            List<ByteDifference> differences = ByteSearcher.CompareByteArrays(db._pdbChunks![0], loadedDb._pdbChunks![0]);
            Assert.HasCount(0, differences);

            // Check that the tracked tiles are the same
            Assert.IsTrue(db.TrackedTiles.SequenceEqual(loadedDb.TrackedTiles));
        }

        [TestMethod]
        public void Create4x4Pdbs()
        {
            byte boardSize = 4; // 4x4 grid
            byte[][] trackedTileSets = [[0, 1, 4, 5], [2, 3, 6, 7], [8, 9, 12, 13], [10, 11, 14, byte.MaxValue]];
            for (int tileSet = 0; tileSet < trackedTileSets.Length; tileSet++)
            {
                int byteCount = trackedTileSets[tileSet].Count(p => p != byte.MaxValue);
                byte[] trackedTiles = new byte[byteCount];
                long factor = boardSize * boardSize;
                long numberOfStates = 1;
                string fileName = string.Empty;
                for (int i = 0; i < trackedTiles.Length; i++)
                {
                    numberOfStates *= factor;
                    trackedTiles[i] = trackedTileSets[tileSet][i];
                    factor--;
                    fileName += (trackedTiles[i] + 1).ToString("D2");
                }
                // NumberOfStates should also consider the blank
                numberOfStates *= factor;
                Codec codec = new(boardSize, (byte)byteCount);
                PdbGenerator gen = new(boardSize, (byte)byteCount, false);
                PatternDatabase db = gen.GeneratePdb(new PdbGenerator.PatternState
                {
                    TilePositions = trackedTiles,
                    BlankPosition = (byte)((boardSize * boardSize) - 1)
                });
                db.SaveToFile($"E:\\src\\net\\Slider\\{boardSize}x{boardSize}_{fileName}.pdb");
            }
        }

        [TestMethod]
        public void Create5x5Pdbs()
        {
            byte boardSize = 5; // 5x5 grid
            byte[][] trackedTileSets = [[0, 1, 2, 5, 6], [3, 4, 7, 8, 9], [10, 15, 16, 20, 21], [17, 18, 19, 22, 23], [11, 12, 13, 14]];
            for (int tileSet = 0; tileSet < trackedTileSets.Length; tileSet++)
            {
                int byteCount = trackedTileSets[tileSet].Count(p => p != byte.MaxValue);
                byte[] trackedTiles = new byte[byteCount];
                long factor = boardSize * boardSize;
                long numberOfStates = 1;
                string fileName = string.Empty;
                for (int i = 0; i < trackedTiles.Length; i++)
                {
                    numberOfStates *= factor;
                    trackedTiles[i] = trackedTileSets[tileSet][i];
                    factor--;
                    fileName += (trackedTiles[i] + 1).ToString("D2");
                }
                // NumberOfStates should also consider the blank
                numberOfStates *= factor;
                Codec codec = new(boardSize, (byte)byteCount);
                PdbGenerator gen = new(boardSize, (byte)byteCount, false);
                PatternDatabase db = gen.GeneratePdb(new PdbGenerator.PatternState
                {
                    TilePositions = trackedTiles,
                    BlankPosition = (byte)((boardSize * boardSize) - 1)
                });
                db.SaveToFile($"E:\\src\\net\\Slider\\{boardSize}x{boardSize}_{fileName}.pdb");
            }
        }

    }
}

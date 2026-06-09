using Mono.Cecil.Cil;
using PDBGenerator;
using Slider.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Text;

namespace UnitTest
{
    [TestClass]
    public class PdbGeneratorTests
    {
        private class Result
        {
            public required byte[] Board { get; set; }
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
            PdbGenerator gen = new(2, 3, true);
            PatternDatabase db = gen.GeneratePdb(new PdbGenerator.PatternState
            {
                TilePositions = new byte[] { 0, 1, 2 },
                BlankPosition = 3,
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
                TilePositions = new byte[] { 0, 1, 2 },
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
        public void PdbGeneratorPerformance4x4_4TrackedTiles()
        {
            byte boardSize = 4;
            byte k = 4;
            byte[] trackedTiles = new byte[k];
            long factor = boardSize * boardSize;
            long numberOfStates = 1;

            for (int i = 0; i < k; i++)
            {
                numberOfStates *= factor;
                trackedTiles[i] = (byte)i;
                factor--;
            }
            // NumberOfStates should also consider the blank
            numberOfStates *= factor;
            PdbGenerator gen = new(boardSize, k, true);
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
            PdbGenerator gen = new(4, 3, true);
            PatternDatabase db = gen.GeneratePdb(new PdbGenerator.PatternState
            {
                TilePositions = new byte[] { 12, 13, 14 },
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
                int factor = boardSize * boardSize;
                int numberOfStates = 1;
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
                PdbGenerator gen = new(boardSize, (byte)byteCount);
                PatternDatabase db = gen.GeneratePdb(new PdbGenerator.PatternState
                {
                    TilePositions = trackedTiles,
                    BlankPosition = (byte)((boardSize * boardSize) - 1)
                });
                db.SaveToFile($"E:\\src\\net\\Slider\\{boardSize}x{boardSize}_{fileName}.pdb");
            }
        }

        [TestMethod]
        [DataRow(5, 5)]
        [DataRow(5, 6)]
        [DataRow(5, 7)]
        [DataRow(6, 5)]
        [DataRow(6, 6)]
        [DataRow(6, 7)]
        [DataRow(10, 4)]
        [DataRow(10, 5)]
        public void CalculateNumberOfStates(int boardSize, int trackedTilesCount)
        {
            string FormatNumber(long number)
            {
                if (number < 1000)
                {
                    return number.ToString();
                }
                int exp = 0;
                double dNumber = number;
                while (dNumber >= 1000)
                {
                    dNumber /= 1000.0;
                    exp += 3;
                }
                return $"{Math.Round(dNumber, 2)}E{exp}";
            }
            // Not a test method, as such, just a calculator of predicted number of steps
            int factor = boardSize * boardSize;
            long numberOfStates = 1;
            for (int i = 0; i < trackedTilesCount; i++)
            {
                numberOfStates *= factor;
                factor--;
            }
            long numberOfTrackedTileStates = numberOfStates;
            // NumberOfStates should also consider the blank
            numberOfStates *= factor;
            Console.WriteLine($"BoardSize: {boardSize}, tracked tiles: {trackedTilesCount}, numberOfStates: {FormatNumber(numberOfTrackedTileStates)}, including blank: {FormatNumber(numberOfStates)} ({FormatNumber(numberOfStates / 8)} bytes)");
        }
        [TestMethod]
        public void Create5x5Pdbs()
        {

            // PatternState implementation
            // Duraction 3 min
            // Processed 248957839 states.Queue size: 0.Time spent 00:02:49.0897390
            // Max queue length 14631242
            // Peak Working Set during execution: 7,030,505,472 bytes


            byte boardSize = 5; // 5x5 grid
                                //            byte[][] trackedTileSets = [[0, 1, 2, 5, 6], [3, 4, 7, 8, 9], [10, 11, 12, 15, 16], [13, 14, 18, 19, 23], [17, 20, 21, 22]];
                                //            byte[][] trackedTileSets = [[0, 1, 2, 5, 10], [3, 4, 9, 14, 19], [15,20,21, 22, 23], [6,7,8, 11, 16], [12, 13, 17, 18]];
            byte[][] trackedTileSets = [[0, 1, 2, 5, 6]];
            // Verify tile sets
            //byte[] testBoard = new byte[boardSize * boardSize];
            //foreach (byte[] tileSet in trackedTileSets)
            //{
            //    foreach (byte trackedTile in tileSet)
            //    {
            //        Assert.AreEqual(0, testBoard[trackedTile], $"Tile with index {trackedTile} occurs multiple times");
            //        testBoard[trackedTile] = 1;
            //    }
            //}
            //for (int i = 0; i < testBoard.Length -1; i++)
            //{
            //    Assert.AreNotEqual(0, testBoard[i], $"Index {i} is not tracked");
            //}
            for (int tileSet = 0; tileSet < trackedTileSets.Length; tileSet++)
            {
                int byteCount = trackedTileSets[tileSet].Count(p => p != byte.MaxValue);
                byte[] trackedTiles = new byte[byteCount];
                int factor = boardSize * boardSize;
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

                PatternDatabase? db;
                using (Process currentProcess = Process.GetCurrentProcess())
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    currentProcess.Refresh();

                    Codec codec = new(boardSize, (byte)byteCount);
                    PdbGenerator gen = new(boardSize, (byte)byteCount, false);
                    db = gen.GeneratePdb(new PdbGenerator.PatternState
                    {
                        TilePositions = trackedTiles,
                        BlankPosition = (byte)((boardSize * boardSize) - 1)
                    });
                    currentProcess.Refresh();
                    long peakWorkingSet = currentProcess.PeakWorkingSet64;
                    Console.WriteLine($"Peak Working Set during execution: {peakWorkingSet:N0} bytes");
                }
                db.SaveToFile($"E:\\src\\net\\Slider\\{boardSize}x{boardSize}_{fileName}.pdb");
            }
        }

        [TestMethod]
        public void VerifyAgainstTruth()
        {
            PatternDatabase? pdbTruth = PatternDatabase.LoadFromFile(@"E:\src\net\Slider\PDB Backups\5x5_0102030607.pdb", true);
            Assert.IsNotNull(pdbTruth);
            byte boardSize = 5; // 5x5 grid
            byte[] trackedTiles = [0, 1, 2, 5, 6];
            byte blankPos = 24;
            Codec codecNew = new(boardSize, 5);
            Codec codecTruth = new(boardSize, 5);
            PdbGenerator gen = new(boardSize, 5, false);
            int equals = 0;
            PatternDatabase db = gen.GeneratePdb(new PdbGenerator.PatternState
            {
                TilePositions = trackedTiles,
                BlankPosition = (byte)((boardSize * boardSize) - 1)
            }, ((index, cost) =>
            {
                if (index == 5)
                {
                    int a = 1;
                }
                DecodeResult result = codecNew.Decode(index); // Get the byte pattern
                blankPos = result.BlankPosition;
                long truthIndex = codecTruth.Encode(result.TilePositions, blankPos);
                byte truthDistance = pdbTruth.GetDistance(truthIndex);
                if (truthDistance != cost)
                {
                    string message = $"{string.Join(",", result.TilePositions)}: Truth PDB says {truthDistance}, but new PDB says {cost}\r\nNew index was {index}, equals so far {equals}";
                    Debug.WriteLine(message);
                    //Assert.AreEqual(truthDistance, cost, message);
                }
                else
                    equals++;
            }));
            long[] keys = pdbTruth._pdbChunks.Keys.ToArray();
            for (int i = 0; i < pdbTruth._pdbChunks.Keys.Count; i++)
            {
                List<ByteDifference> diffs = ByteSearcher.CompareByteArrays(pdbTruth._pdbChunks[keys[i]], db._pdbChunks[keys[i]]);
                Assert.IsEmpty(diffs);
            }
        }
    }
}

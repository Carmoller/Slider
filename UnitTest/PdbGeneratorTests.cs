using Microsoft.Testing.Platform.Extensions.Messages;
using Mono.Cecil.Cil;
using PDBGenerator;
using Slider.Interfaces;
using System;
using System.Collections;
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
           new Result {Board = [1, 2, 3, 0], Distance = 0},
           new Result {Board = [1, 0, 3, 2], Distance = 1},
           new Result {Board = [1, 2, 0, 3], Distance = 1},
           new Result {Board = [0, 1, 3, 2], Distance = 2},
           new Result {Board = [0, 2, 1, 3], Distance = 2},
           new Result {Board = [0, 3, 2, 1], Distance = 6},
           new Result {Board = [2, 0, 1, 3], Distance = 3},
           new Result {Board = [2, 3, 0, 1], Distance = 5},
           new Result {Board = [2, 3, 1, 0], Distance = 4},
           new Result {Board = [3, 0, 2, 1], Distance = 5},
           new Result {Board = [3, 1, 0, 2], Distance = 3},
           new Result {Board = [3, 1, 2, 0], Distance = 4}
        ];

        public static bool AllowDebugShortcuts { get; private set; }

//        [TestMethod]
        [TestCategory("DebugOnly")] // Flagging this test
        public void Test_GeneratorCompleteness()
        {
            // Test that the generator considers all legal states
            PdbGenerator gen = new(2, 4, false);
            PatternDatabase db = gen.GeneratePdb([0, 1, 2, 3 ], 3);
            Codec codec = new(2, 4);

            Span<byte> trackedTiles = new byte[4];

            // All reachable permutations of a 2x2 sliding puzzle (0 = blank)
            foreach (Result result in AllLegalBoards)
            {
                byte blankPos = byte.MaxValue;
                for (int i = 0; i < result.Board.Length; i++)
                {
                    if (result.Board[i] == 0)
                    {
                        blankPos = (byte)i;
                        trackedTiles[3] = (byte)i;
                    }
                    else
                    {
                        trackedTiles[result.Board[i] - 1] = (byte)i;
                    }
                }
                long index = codec.Encode(trackedTiles);

                byte distance = db.GetDistance(index);

                string boardText = "(";
                boardText += string.Join(",", result.Board!);
                boardText += ")";
                Assert.AreEqual(result.Distance, distance, $"Board {boardText}");
            }
        }

//        [TestMethod]
        [TestCategory("DebugOnly")] // Flagging this test
        public void PdbGeneratorPerformance4x4_4TrackedTiles()
        {
            byte boardSize = 4;
            byte k = 7;
            byte[] goalState = new byte[k];
            long factor = boardSize*boardSize;
            long numberOfStates = 1;
            int blankPos = byte.MaxValue;

            // goalState should be of the form [0,1,2, ..., boardSize*boardSize-1] since we're tracking the blank
            for (int i = 0; i < k; i++)
            {
                numberOfStates *= (boardSize * boardSize - i);
                if (i == goalState.Length - 1)
                {
                    blankPos = i;
                    goalState[i] = (byte)(boardSize * boardSize - 1);
                }
                else
                {
                    goalState[i] = (byte)i;
                }
                factor--;
            }
            PdbGenerator gen = new(boardSize, k, false);
            PatternDatabase db = gen.GeneratePdb(goalState, blankPos, null);
            Assert.AreEqual(numberOfStates, gen.StatesProcessed);
            Console.WriteLine($"Generated {boardSize}-tile PDB with {k} elements in {gen.ElapsedMs} ms, processed {gen.StatesProcessed} states");
            Console.WriteLine("States per ms: " + Math.Round(((double)gen.StatesProcessed / (gen.ElapsedMs))));
        }

 //       [TestMethod]
        [TestCategory("DebugOnly")] // Flagging this test
        public void Test_Generator_LoadAndSave_YieldsSameByteArray()
        {
            PdbGenerator gen = new(4, 4, false);
            PatternDatabase db = gen.GeneratePdb([12, 13, 14, 15], 3);
            string tempFile = Path.GetTempFileName();
            db.SaveToFile(tempFile);

            PatternDatabase? loadedDb = PatternDatabase.LoadFromFile(tempFile);
            File.Delete(tempFile);

            Assert.IsNotNull(loadedDb);
            Assert.AreEqual(4, loadedDb.K);

            Codec codec = new(4, 4);
            long index = codec.Encode(new byte[] { 2, 0, 13, 7 });
            Assert.AreEqual(18031, index);
            byte distance1 = db.GetDistance(index);
            byte distance2 = loadedDb.GetDistance(index);

            // Verify the header fields, we can access from outside
            Assert.AreEqual(db.GridSize, loadedDb.GridSize);
            Assert.AreEqual(db.K, loadedDb.K);
            Assert.AreEqual(db.TotalStates, loadedDb.TotalStates);
            Assert.AreEqual(db.BlankIndex, loadedDb.BlankIndex);
            Console.WriteLine($"Distance from original db: {distance1}, distance from loaded db: {distance2}");
            Assert.AreEqual(distance1, distance2);

            Assert.IsNotNull(loadedDb);
            List<ByteDifference> differences = ByteSearcher.CompareByteArrays(db._pdbChunks![0], loadedDb._pdbChunks![0]);
            Assert.HasCount(0, differences);

            // Check that the tracked tiles are the same
            Assert.IsTrue(db.TrackedTiles.SequenceEqual(loadedDb.TrackedTiles));
        }

 //       [TestMethod]
        [TestCategory("DebugOnly")] // Flagging this test
        public void Create4x4Pdbs()
        {
            byte boardSize = 4; // 4x4 grid
            //byte[][] trackedTileSets = [[0, 1, 4, 5, 14], [2, 3, 6, 7, 15], [8, 9, 12, 13, 15], [10, 11, 14, 15]];
            byte[][] trackedTileSets = [[0, 1, 2, 3, 4, 5, 15], [6, 7, 8, 10,11, 12, 15], [9, 13, 14, 15]];
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
                PatternDatabase db = gen.GeneratePdb(trackedTiles, trackedTileSets.Length - 1);
                db.SaveToFile($"E:\\src\\net\\Slider\\{boardSize}x{boardSize}_{fileName}.pdb");
            }
        }

        [TestMethod]
        [TestCategory("DebugOnly")] // Flagging this test
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
            Assert.Inconclusive("Not meant to be run from the test explorer");
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
//        [TestMethod]
        [TestCategory("DebugOnly")] // Flagging this test
        public void Create5x5Pdbs()
        {
            byte boardSize = 5; // 5x5 grid
                                //            byte[][] trackedTileSets = [[0, 1, 2, 5, 6], [3, 4, 7, 8, 9], [10, 11, 12, 15, 16], [13, 14, 18, 19, 23], [17, 20, 21, 22]];
                                byte[][] trackedTileSets = [[0, 1, 2, 5, 10, 24], [3, 4, 9, 14, 19, 24], [15,20,21, 22, 23, 24], [6,7,8, 11, 16, 24], [12, 13, 17, 18, 24]];
                                //byte[][] trackedTileSets = [[0, 1, 2, 5, 10, 24]];
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
            int blankPos;
            for (int tileSet = 0; tileSet < trackedTileSets.Length; tileSet++)
            {
                int byteCount = trackedTileSets[tileSet].Count(p => p != byte.MaxValue);
                byte[] trackedTiles = new byte[byteCount];
                int factor = boardSize * boardSize;
                string fileName = string.Empty;
                long totalStates = 1;
                for (int i = 0; i < trackedTiles.Length; i++)
                {
                    totalStates *= factor;
                    trackedTiles[i] = trackedTileSets[tileSet][i];
                    factor--;
                    if (trackedTiles[i] == boardSize * boardSize - 1) // This is the blank
                    {
                        blankPos = i;
                    }
                    else
                        fileName += (trackedTiles[i] + 1).ToString("D2");
                }

                PatternDatabase? db;
                using (Process currentProcess = Process.GetCurrentProcess())
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    currentProcess.Refresh();

                    Codec codec = new(boardSize, (byte)byteCount);
                    PdbGenerator gen = new(boardSize, (byte)byteCount, false);
                    db = gen.GeneratePdb(trackedTileSets[tileSet], byteCount -1);
                    currentProcess.Refresh();
                    long peakWorkingSet = currentProcess.PeakWorkingSet64;
                    TimeSpan timeSpent = TimeSpan.FromMilliseconds(gen.ElapsedMs);
                    Console.WriteLine($"Peak Working Set during execution: {peakWorkingSet:N0} bytes");
                    Console.WriteLine($"Expected total states: {totalStates:N0}");
                    Console.WriteLine($"Processed {gen.StatesProcessed:N0} states. Time spent {timeSpent}");
                    Console.WriteLine($"({Math.Round(gen.StatesProcessed / (double)gen.ElapsedMs)} states/ms)");
                    Console.WriteLine($"Max queue length {gen.MaxQueueLength:N0} (= {gen.MaxQueueLength *100 / totalStates}% of total states)");
                    Console.WriteLine($"Max cost {gen.MaxCost:N0}");
                }

                db.SaveToFile($"E:\\src\\net\\Slider\\{boardSize}x{boardSize}_{fileName}.pdb");
            }
        }

//        [TestMethod]
        [TestCategory("DebugOnly")] // Flagging this test
        public void VerifyAgainstTruth()
        {
            PatternDatabase? pdbTruth = PatternDatabase.LoadFromFile(@"E:\src\net\Slider\5x5_0102030607.pdb_truth");
            Assert.IsNotNull(pdbTruth);
            byte boardSize = 5; // 5x5 grid
            byte[] trackedTiles = [0, 1, 2, 5, 6, 24];
            Codec codecNew = new(boardSize, 6);
            Codec codecTruth = new(boardSize, 5);
            PdbGenerator gen = new(boardSize, 6, false);
            int equals = 0;
            PatternDatabase db = gen.GeneratePdb(trackedTiles, 5, ((index, cost) =>
            {
                Span<byte> tiles = new byte[6];
                codecNew.DecodeMem(index, tiles); // Get the byte pattern
                byte blankPos = tiles[5];
                byte[] truthTiles = new byte[5];
                for (int i = 0; i < 5; i++)
                {
                    truthTiles[i] = tiles[i];
                }
                long truthIndex = codecTruth.Encode(truthTiles, blankPos);
                byte truthDistance = pdbTruth.GetDistance(truthIndex);
                if (truthDistance != cost)
                {
                    string tileString = string.Empty;
                    for (int i = 0; i < tiles.Length; i++)
                    {
                        tileString += " " + tiles[i].ToString("D2");
                    }
                    string message = $"{tileString}: Truth PDB says {truthDistance}, but new PDB says {cost}\r\nNew index was {index}, equals so far {equals}";
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

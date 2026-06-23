using Slider.Heuristics;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTest.HeuristicElementTests
{
    [TestClass]
    public class ReverseManhattanTest
    {
        private byte[] TargetStateToTargetPositions(byte[] targetState)
        {
            byte[] result = new byte[targetState.Length];

            for (int i = 0; i < targetState.Length; i++)
            {
                result[targetState[i]] = (byte)i;
            }
            return result;
        }

        [TestMethod]
        public void ReverseManhattan_TargetState_MustReturn0_3x3()
        {
            byte[] targetState = [0, 8, 7, 
                                  6, 5, 4, 
                                  3, 2, 1];

            byte[] board = [0, 8, 7, 
                            6, 5, 4, 
                            3, 2,1];
            byte[] targetPositions = TargetStateToTargetPositions(targetState);
            int h = ReverseManhattanCalculator.Calculate(board, targetPositions, 3);

            Assert.AreEqual(0, h);
        }

        [TestMethod]
        public void ReverseManhattan_OneMoveOff_MustReturn1_3x3()
        {
            byte[] targetState = [0, 8, 7, 6, 5, 4, 3, 2, 1];

            byte[] board = [8, 0, 7, 6, 5, 4, 3, 2, 1];

            byte[] targetPositions = TargetStateToTargetPositions(targetState);
            int h = ReverseManhattanCalculator.Calculate(board, targetPositions, 3);

            Assert.AreEqual(1, h);
        }
        [TestMethod]
        public void ReverseManhattan_ReverseBoard_MustReturn20_3x3()
        {
            byte[] targetState = [0, 8, 7, 
                                  6, 5, 4, 
                                  3, 2, 1];

            byte[] board = [1, 2, 3,
                            4, 5, 6, 
                            7, 8, 0];

            byte[] targetPositions = TargetStateToTargetPositions(targetState);
            int h = ReverseManhattanCalculator.Calculate(board, targetPositions, 3);

            // 1 is 4 moves away
            // 2 is 2 moves away
            // 3 is 4 moves away
            // 4 is 2 move away
            // 5 is in place
            // 6 is 2 moves away
            // 7 is 4 moves away
            // 8 is 2 moves away
            Assert.AreEqual(20, h);
        }
        [TestMethod]
        public void ReverseManhattan_TargetState_MustReturn0_4x4()
        {
            byte[] targetState = [00, 15, 14, 13, 
                                  12, 11, 10, 09,
                                  08, 07, 06, 05, 
                                  04, 03, 02, 01];

            byte[] board = [00, 15, 14, 13,
                            12, 11, 10, 09,
                            08, 07, 06, 05,
                            04, 03, 02, 01];

            byte[] targetPositions = TargetStateToTargetPositions(targetState);
            int h = ReverseManhattanCalculator.Calculate(board, targetPositions, 4);

            Assert.AreEqual(0, h);
        }

        [TestMethod]
        public void ReverseManhattan_OneMoveOff_MustReturn1_4x4()
        {
            byte[] targetState = [00, 15, 14, 13,
                                  12, 11, 10, 09,
                                  08, 07, 06, 05,
                                  04, 03, 02, 01];

            byte[] board = [15, 00, 14, 13,
                            12, 11, 10, 09,
                            08, 07, 06, 05,
                            04, 03, 02, 01];

            byte[] targetPositions = TargetStateToTargetPositions(targetState);
            int h = ReverseManhattanCalculator.Calculate(board, targetPositions, 3);

            Assert.AreEqual(1, h);
        }
        [TestMethod]
        public void ReverseManhattan_ReverseBoard_MustReturn20_4x4()
        {
            byte[] targetState = [00, 15, 14, 13,
                                  12, 11, 10, 09,
                                  08, 07, 06, 05,
                                  04, 03, 02, 01];


            byte[] board = [01, 02, 03, 04,
                            05, 06, 07, 08,
                            09, 10, 11, 12,
                            13, 14, 15, 00];

            byte[] targetPositions = TargetStateToTargetPositions(targetState);
            int h = ReverseManhattanCalculator.Calculate(board, targetPositions, 4);

            // 01 is 6 moves away
            // 02 is 4 moves away
            // 03 is 4 moves away
            // 04 is 6 move away
            // 05 is 4 moves away
            // 06 is 2 moves away
            // 07 is 2 moves away
            // 08 is 4 moves away
            // 09 is 4 moves away
            // 10 is 2 moves away
            // 11 is 2 moves away
            // 12 is 4 moves away
            // 13 is 6 moves away
            // 14 is 4 moves away
            // 15 is 4 moves away
            Assert.AreEqual(58, h);
        }

    }
}

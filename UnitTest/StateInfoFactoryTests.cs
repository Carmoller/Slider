using Moq;
using Slider.Common;
using Slider.Common.Interfaces;
using Slider.Solver;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace UnitTest
{
    // Need a fake class, since Moq can't handle ref returns
    public class FakeObjectPool : IChunkedStructPool<StateInfo>
    {
        private StateInfo _state;
        private StateInfo _newState = new StateInfo();
        private int _currentIndex;

        public FakeObjectPool(int currentIndex = 0)
        {
            _currentIndex = currentIndex;
        }
        public int Get<TState>(TState state, RefInitializer<StateInfo, TState> initializer)
        {
            initializer(ref _newState, state);
            return _currentIndex++;
        }

        public ref StateInfo GetRef(int index) => ref _state;

        public void Release(int index, RefAction<StateInfo>? Dispose = null) { }
    }
    public class FakeArrayPool : IChunkedArrayPoolUnsafe
    {
        int _size;
        public FakeArrayPool(int size)
        {
            _size = size;
        }
        private int index;
        public void Dispose()
        {
        }

        public int Get()
        {
            return index++;
        }
        public unsafe PointerToken GetToken()
        {
            byte[] board = new byte[_size];
            fixed (byte* pByte = board)
            {
                PointerToken token = new PointerToken(pByte, _size, index++);
                return token;
            }
        }
        public void Release(int index)
        {
        }

        public void Release(PointerToken token)
        {
        }
    }
    [TestClass]
    public class StateInfoFactoryTests
    {
        private struct UnitTestContext
        {
            public int Value;
        }

        [TestMethod]
        public void StateInfoFactory_MustReturnAllDirections()
        {
            int length = 9;
            FakeObjectPool objectPool = new();
            FakeArrayPool arrayPool = new(length);
            StateInfo currentState = new StateInfo { PreviousMove = MoveDirection.None, BlankPos = 4, BoardToken = arrayPool.GetToken()};
            List<MoveDirection> directions = new();
            StateInfoFactory testObject = new();
            UnitTestContext context = new UnitTestContext { Value = 4 };
            testObject.GetAvailableMoves(ref currentState, 3, objectPool, arrayPool, ref context, (ref p, ref context) => { directions.Add(p.PreviousMove); });

            Assert.HasCount(4, directions);
            Assert.Contains(MoveDirection.Up, directions);
            Assert.Contains(MoveDirection.Down, directions);
            Assert.Contains(MoveDirection.Left, directions);
            Assert.Contains(MoveDirection.Right, directions);
        }
        [TestMethod]
        public void StateInfoFactory_MustNotReturnInverseOfPreviousMove()
        {
            int length = 9;
            FakeObjectPool objectPool = new();
            FakeArrayPool arrayPool = new(length);
            UnitTestContext context = new UnitTestContext { Value = 4 };
            StateInfo currentState = new StateInfo { PreviousMove = MoveDirection.Up, BlankPos = 4, BoardToken = arrayPool.GetToken() };
            StateInfoFactory testObject = new();
            // Previous move is up
            testObject.GetAvailableMoves(ref currentState, 3, objectPool, arrayPool, ref context, (ref p, ref context) => { Assert.AreNotEqual(MoveDirection.Down, p.PreviousMove); });
            // Previous move is down
            currentState.PreviousMove = MoveDirection.Down;
            testObject.GetAvailableMoves(ref currentState, 3, objectPool, arrayPool, ref context, (ref p, ref context) => { Assert.AreNotEqual(MoveDirection.Up, p.PreviousMove); });
            // Previous move is left
            currentState.PreviousMove = MoveDirection.Left;
            testObject.GetAvailableMoves(ref currentState, 3, objectPool, arrayPool, ref context, (ref p, ref context) => { Assert.AreNotEqual(MoveDirection.Right, p.PreviousMove); });
            // Previous move is right
            currentState.PreviousMove = MoveDirection.Right;
            testObject.GetAvailableMoves(ref currentState, 3, objectPool, arrayPool, ref context, (ref p, ref context) => { Assert.AreNotEqual(MoveDirection.Left, p.PreviousMove); });
        }
        [TestMethod]
        public void StateInfoFactory_ContentsMustBeCorrectForUpMove()
        {
            int nodeIndex = 123;
            int currentG = 23;
            int blankPos = 4;
            int currentIndex = 222;

            int length = 9;
            FakeObjectPool objectPool = new(currentIndex);
            FakeArrayPool arrayPool = new(length);

            UnitTestContext context = new UnitTestContext { Value = 4 };

            byte[] board = [1, 2, 3, 4, 0, 6, 7, 8, 5];

            StateInfo currentState = new StateInfo
            {
                PreviousMove = MoveDirection.None,
                BlankPos = blankPos,
                BoardToken = arrayPool.GetToken() ,
                CurrentG = currentG,
                NodeIndex = nodeIndex
            };
            board.CopyTo(currentState.BoardToken.AsSpan());
            int gridSize = (int)(Math.Sqrt(length));
            List<MoveDirection> directions = new();
            StateInfoFactory testObject = new();
            StateInfo newState = new();
            testObject.GetAvailableMoves(ref currentState, gridSize, objectPool, arrayPool, ref context, (ref p, ref context) => { if (p.PreviousMove == MoveDirection.Up) { newState = p; }; });

            Assert.AreEqual(MoveDirection.Up, newState.PreviousMove);
            // We don't know the which order the new states are allocated, so NodeIndex can be between currentIndex and currentIndex + 4
            Assert.IsGreaterThanOrEqualTo(currentIndex, newState.NodeIndex);
            Assert.IsLessThanOrEqualTo(currentIndex + 4, newState.NodeIndex);
            Assert.AreEqual(nodeIndex, newState.ParentIndex);
            Assert.AreEqual(currentG + 1, newState.CurrentG);
            Assert.AreEqual(currentG, newState.BestG);
            Assert.AreEqual(blankPos - gridSize, newState.BlankPos);
            Assert.AreEqual(0, newState.BoardToken.AsSpan()[newState.BlankPos]); // Board at Blankpos must indeed be empty
            Assert.IsTrue(newState.BoardToken.AsSpan().SequenceEqual<byte>([1, 0, 3, 4, 2, 6, 7, 8, 5]));
        }
        [TestMethod]
        public void StateInfoFactory_ContentsMustBeCorrectForDownMove()
        {
            int nodeIndex = 123;
            int currentG = 23;
            int blankPos = 4;
            int currentIndex = 222;

            int length = 9;
            FakeObjectPool objectPool = new(currentIndex);
            FakeArrayPool arrayPool = new(length);
            StateInfo currentState = new StateInfo
            {
                PreviousMove = MoveDirection.None,
                BlankPos = blankPos,
                BoardToken = arrayPool.GetToken(),
                CurrentG = currentG,
                NodeIndex = nodeIndex
            };
            byte[] board = [1, 2, 3,
                         4, 0, 6,
                         7, 8, 5];
            board.CopyTo(currentState.BoardToken.AsSpan());
            int gridSize = (int)(Math.Sqrt(length));
            List<MoveDirection> directions = new();
            StateInfoFactory testObject = new();
            StateInfo newState = new();
            UnitTestContext context = new UnitTestContext { Value = 23 };
            testObject.GetAvailableMoves(ref currentState, gridSize, objectPool, arrayPool, ref context, (ref p, ref context) => { if (p.PreviousMove == MoveDirection.Down) { newState = p; }; });

            Assert.AreEqual(MoveDirection.Down, newState.PreviousMove);
            // We don't know the which order the new states are allocated, so NodeIndex can be between currentIndex and currentIndex + 4
            Assert.IsGreaterThanOrEqualTo(currentIndex, newState.NodeIndex);
            Assert.IsLessThanOrEqualTo(currentIndex + 4, newState.NodeIndex);
            Assert.AreEqual(nodeIndex, newState.ParentIndex);
            Assert.AreEqual(currentG + 1, newState.CurrentG);
            Assert.AreEqual(currentG, newState.BestG);
            Assert.AreEqual(blankPos + gridSize, newState.BlankPos);
            Assert.AreEqual(0, newState.BoardToken.AsSpan()[newState.BlankPos]); // Board at Blankpos must indeed be empty
            Assert.AreEqual(0, newState.BoardToken.AsSpan().SequenceCompareTo((byte[])([1, 2, 3, 4, 8, 6, 7, 0,5])));
        }
        [TestMethod]
        public void StateInfoFactory_ContentsMustBeCorrectForLeftMove()
        {
            int gridSize = 3;
            int nodeIndex = 123;
            int currentG = 23;
            int blankPos = 4;
            int currentIndex = 222;

            FakeObjectPool objectPool = new(currentIndex);
            FakeArrayPool arrayPool = new(gridSize*gridSize);
            StateInfo currentState = new StateInfo
            {
                PreviousMove = MoveDirection.None,
                BlankPos = blankPos,
                BoardToken = arrayPool.GetToken(),
                CurrentG = currentG,
                NodeIndex = nodeIndex
            };
            ((byte[])[1, 2, 3, 4, 0, 6, 7, 8, 5]).CopyTo(currentState.BoardToken.AsSpan());
            List<MoveDirection> directions = new();
            StateInfoFactory testObject = new();
            StateInfo newState = new();
            UnitTestContext context = new UnitTestContext { Value = 45 };
            testObject.GetAvailableMoves(ref currentState, gridSize, objectPool, arrayPool, ref context, (ref p, ref context) => { if (p.PreviousMove == MoveDirection.Left) { newState = p; }; });

            Assert.AreEqual(MoveDirection.Left, newState.PreviousMove);
            // We don't know the which order the new states are allocated, so NodeIndex can be between currentIndex and currentIndex + 4
            Assert.IsGreaterThanOrEqualTo(currentIndex, newState.NodeIndex);
            Assert.IsLessThanOrEqualTo(currentIndex + 4, newState.NodeIndex);
            Assert.AreEqual(nodeIndex, newState.ParentIndex);
            Assert.AreEqual(currentG + 1, newState.CurrentG);
            Assert.AreEqual(currentG, newState.BestG);
            Assert.AreEqual(blankPos - 1, newState.BlankPos);
            Assert.AreEqual(0, newState.BoardToken.AsSpan()[newState.BlankPos]); // Board at Blankpos must indeed be empty
            Assert.AreEqual(0, newState.BoardToken.AsSpan().SequenceCompareTo((byte[])[1, 2, 3, 0, 4, 6, 7, 8, 5]));
        }
        [TestMethod]
        public void StateInfoFactory_ContentsMustBeCorrectForRightMove()
        {
            int nodeIndex = 123;
            int currentG = 23;
            int blankPos = 4;
            int currentIndex = 222;

            int gridSize = 3;
            FakeObjectPool objectPool = new(currentIndex);
            FakeArrayPool arrayPool = new(gridSize * gridSize);
            StateInfo currentState = new StateInfo
            {
                PreviousMove = MoveDirection.None,
                BlankPos = blankPos,
                BoardToken = arrayPool.GetToken(),
                CurrentG = currentG,
                NodeIndex = nodeIndex
            };
            ((byte[])[1, 2, 3, 4, 0, 6, 7, 8, 5]).CopyTo(currentState.BoardToken.AsSpan());
            List<MoveDirection> directions = new();
            StateInfoFactory testObject = new();
            StateInfo newState = new();
            UnitTestContext context = new UnitTestContext { Value = 76 };
            testObject.GetAvailableMoves(ref currentState, gridSize, objectPool, arrayPool, ref context, (ref p, ref context) => { if (p.PreviousMove == MoveDirection.Right) { newState = p; }; });

            Assert.AreEqual(MoveDirection.Right, newState.PreviousMove);
            // We don't know the which order the new states are allocated, so NodeIndex can be between currentIndex and currentIndex + 4
            Assert.IsGreaterThanOrEqualTo(currentIndex, newState.NodeIndex);
            Assert.IsLessThanOrEqualTo(currentIndex + 4, newState.NodeIndex);
            Assert.AreEqual(nodeIndex, newState.ParentIndex);
            Assert.AreEqual(currentG + 1, newState.CurrentG);
            Assert.AreEqual(currentG, newState.BestG);
            Assert.AreEqual(blankPos + 1, newState.BlankPos);
            Assert.AreEqual(0, newState.BoardToken.AsSpan()[newState.BlankPos]); // Board at Blankpos must indeed be empty
            Assert.AreEqual(0, newState.BoardToken.AsSpan().SequenceCompareTo((byte[])([1, 2, 3, 4, 6, 0, 7, 8, 5])));
        }
    }
}

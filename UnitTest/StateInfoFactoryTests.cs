using Moq;
using Slider.Common;
using Slider.Common.Interfaces;
using Slider.Solver;
using System;
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

        public void Release(int index) { }
    }
    public class FakeArrayPool : IChunkedArrayPool<byte>
    {
        int _size;
        public FakeArrayPool(int size)
        {
            _size = size;
        }
        private int index;
        public int Get()
        {
            return index++;
        }

        public byte[] GetArray(int index)
        {
            return new byte[_size];
        }

        public void Release(int index)
        {
        }
    }
    [TestClass]
    public class StateInfoFactoryTests
    {
        [TestMethod]
        public void StateInfoFactory_MustReturnAllDirections()
        {
            StateInfo currentState = new StateInfo { PreviousMove = MoveDirection.None, BlankPos = 4, Board = new byte[9] };
            FakeObjectPool objectPool = new();
            FakeArrayPool arrayPool = new(currentState.Board.Length);
            List<MoveDirection> directions = new();
            StateInfoFactory testObject = new();
            testObject.GetAvailableMoves(currentState, 3, objectPool, arrayPool, (ref p) => { directions.Add(p.PreviousMove); });

            Assert.HasCount(4, directions);
            Assert.Contains(MoveDirection.Up, directions);
            Assert.Contains(MoveDirection.Down, directions);
            Assert.Contains(MoveDirection.Left, directions);
            Assert.Contains(MoveDirection.Right, directions);
        }
        [TestMethod]
        public void StateInfoFactory_MustNotReturnInverseOfPreviousMove()
        {
            StateInfo currentState = new StateInfo { PreviousMove = MoveDirection.Up, BlankPos = 4, Board = new byte[9] };
            FakeObjectPool objectPool = new();
            FakeArrayPool arrayPool = new(currentState.Board.Length);
            StateInfoFactory testObject = new();
            // Previous move is up
            testObject.GetAvailableMoves(currentState, 3, objectPool, arrayPool, (ref p) => { Assert.AreNotEqual(MoveDirection.Down, p.PreviousMove); });
            // Previous move is down
            currentState.PreviousMove = MoveDirection.Down;
            testObject.GetAvailableMoves(currentState, 3, objectPool, arrayPool, (ref p) => { Assert.AreNotEqual(MoveDirection.Up, p.PreviousMove); });
            // Previous move is left
            currentState.PreviousMove = MoveDirection.Left;
            testObject.GetAvailableMoves(currentState, 3, objectPool, arrayPool, (ref p) => { Assert.AreNotEqual(MoveDirection.Right, p.PreviousMove); });
            // Previous move is right
            currentState.PreviousMove = MoveDirection.Right;
            testObject.GetAvailableMoves(currentState, 3, objectPool, arrayPool, (ref p) => { Assert.AreNotEqual(MoveDirection.Left, p.PreviousMove); });
        }
        [TestMethod]
        public void StateInfoFactory_ContentsMustBeCorrectForUpMove()
        {
            int nodeIndex = 123;
            int currentG = 23;
            int blankPos = 4;
            int currentIndex = 222;

            StateInfo currentState = new StateInfo
            {
                PreviousMove = MoveDirection.None,
                BlankPos = blankPos,
                Board = [1, 2, 3, 4, 0, 6, 7, 8, 5],
                CurrentG = currentG,
                NodeIndex = nodeIndex
            };
            int gridSize = (int)(Math.Sqrt(currentState.Board.Length));
            FakeObjectPool objectPool = new(currentIndex);
            FakeArrayPool arrayPool = new(currentState.Board.Length);
            List<MoveDirection> directions = new();
            StateInfoFactory testObject = new();
            StateInfo newState = new();
            testObject.GetAvailableMoves(currentState, gridSize, objectPool, arrayPool, (ref p) => { if (p.PreviousMove == MoveDirection.Up) { newState = p; }; });

            Assert.AreEqual(MoveDirection.Up, newState.PreviousMove);
            // We don't know the which order the new states are allocated, so NodeIndex can be between currentIndex and currentIndex + 4
            Assert.IsGreaterThanOrEqualTo(currentIndex, newState.NodeIndex);
            Assert.IsLessThanOrEqualTo(currentIndex + 4, newState.NodeIndex);
            Assert.AreEqual(nodeIndex, newState.ParentIndex);
            Assert.AreEqual(currentG + 1, newState.CurrentG);
            Assert.AreEqual(currentG, newState.BestG);
            Assert.AreEqual(blankPos - gridSize, newState.BlankPos);
            Assert.IsTrue(Enumerable.SequenceEqual<byte>([1, 0, 3, 4, 2, 6, 7, 8, 5], newState.Board));
        }
        [TestMethod]
        public void StateInfoFactory_ContentsMustBeCorrectForDownMove()
        {
            int nodeIndex = 123;
            int currentG = 23;
            int blankPos = 4;
            int currentIndex = 222;

            StateInfo currentState = new StateInfo
            {
                PreviousMove = MoveDirection.None,
                BlankPos = blankPos,
                Board = [1, 2, 3, 4, 0, 6, 7, 8, 5],
                CurrentG = currentG,
                NodeIndex = nodeIndex
            };
            int gridSize = (int)(Math.Sqrt(currentState.Board.Length));
            FakeObjectPool objectPool = new(currentIndex);
            FakeArrayPool arrayPool = new(currentState.Board.Length);
            List<MoveDirection> directions = new();
            StateInfoFactory testObject = new();
            StateInfo newState = new();
            testObject.GetAvailableMoves(currentState, gridSize, objectPool, arrayPool, (ref p) => { if (p.PreviousMove == MoveDirection.Down) { newState = p; }; });

            Assert.AreEqual(MoveDirection.Down, newState.PreviousMove);
            // We don't know the which order the new states are allocated, so NodeIndex can be between currentIndex and currentIndex + 4
            Assert.IsGreaterThanOrEqualTo(currentIndex, newState.NodeIndex);
            Assert.IsLessThanOrEqualTo(currentIndex + 4, newState.NodeIndex);
            Assert.AreEqual(nodeIndex, newState.ParentIndex);
            Assert.AreEqual(currentG + 1, newState.CurrentG);
            Assert.AreEqual(currentG, newState.BestG);
            Assert.AreEqual(blankPos + gridSize, newState.BlankPos);
            Assert.IsTrue(Enumerable.SequenceEqual<byte>([1, 2, 3, 4, 8, 6, 7, 0,5], newState.Board));
        }
        [TestMethod]
        public void StateInfoFactory_ContentsMustBeCorrectForLeftMove()
        {
            int nodeIndex = 123;
            int currentG = 23;
            int blankPos = 4;
            int currentIndex = 222;

            StateInfo currentState = new StateInfo
            {
                PreviousMove = MoveDirection.None,
                BlankPos = blankPos,
                Board = [1, 2, 3, 4, 0, 6, 7, 8, 5],
                CurrentG = currentG,
                NodeIndex = nodeIndex
            };
            int gridSize = (int)(Math.Sqrt(currentState.Board.Length));
            FakeObjectPool objectPool = new(currentIndex);
            FakeArrayPool arrayPool = new(currentState.Board.Length);
            List<MoveDirection> directions = new();
            StateInfoFactory testObject = new();
            StateInfo newState = new();
            testObject.GetAvailableMoves(currentState, gridSize, objectPool, arrayPool, (ref p) => { if (p.PreviousMove == MoveDirection.Left) { newState = p; }; });

            Assert.AreEqual(MoveDirection.Left, newState.PreviousMove);
            // We don't know the which order the new states are allocated, so NodeIndex can be between currentIndex and currentIndex + 4
            Assert.IsGreaterThanOrEqualTo(currentIndex, newState.NodeIndex);
            Assert.IsLessThanOrEqualTo(currentIndex + 4, newState.NodeIndex);
            Assert.AreEqual(nodeIndex, newState.ParentIndex);
            Assert.AreEqual(currentG + 1, newState.CurrentG);
            Assert.AreEqual(currentG, newState.BestG);
            Assert.AreEqual(blankPos - 1, newState.BlankPos);
            Assert.IsTrue(Enumerable.SequenceEqual<byte>([1, 2, 3, 0, 4, 6, 7, 8, 5], newState.Board));
        }
        [TestMethod]
        public void StateInfoFactory_ContentsMustBeCorrectForRightMove()
        {
            int nodeIndex = 123;
            int currentG = 23;
            int blankPos = 4;
            int currentIndex = 222;

            StateInfo currentState = new StateInfo
            {
                PreviousMove = MoveDirection.None,
                BlankPos = blankPos,
                Board = [1, 2, 3, 4, 0, 6, 7, 8, 5],
                CurrentG = currentG,
                NodeIndex = nodeIndex
            };
            int gridSize = (int)(Math.Sqrt(currentState.Board.Length));
            FakeObjectPool objectPool = new(currentIndex);
            FakeArrayPool arrayPool = new(currentState.Board.Length);
            List<MoveDirection> directions = new();
            StateInfoFactory testObject = new();
            StateInfo newState = new();
            testObject.GetAvailableMoves(currentState, gridSize, objectPool, arrayPool, (ref p) => { if (p.PreviousMove == MoveDirection.Right) { newState = p; }; });

            Assert.AreEqual(MoveDirection.Right, newState.PreviousMove);
            // We don't know the which order the new states are allocated, so NodeIndex can be between currentIndex and currentIndex + 4
            Assert.IsGreaterThanOrEqualTo(currentIndex, newState.NodeIndex);
            Assert.IsLessThanOrEqualTo(currentIndex + 4, newState.NodeIndex);
            Assert.AreEqual(nodeIndex, newState.ParentIndex);
            Assert.AreEqual(currentG + 1, newState.CurrentG);
            Assert.AreEqual(currentG, newState.BestG);
            Assert.AreEqual(blankPos + 1, newState.BlankPos);
            Assert.IsTrue(Enumerable.SequenceEqual<byte>([1, 2, 3, 4, 6, 0, 7, 8, 5], newState.Board));
        }
    }
}

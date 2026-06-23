using Microsoft.VisualStudio.TestTools.UnitTesting;
using Slider.Solver;
using System;

namespace UnitTest
{
    [TestClass]
    public class SolveStateDictionaryTests
    {
        /// <summary>
        /// Helper method to create a SolveState with a specific board configuration
        /// </summary>
        private SolveState CreateSolveState(byte[] board, int gCost = 0, int hCost = 0, byte emptyPosition = 0)
        {
            return new SolveState(board, gCost, hCost, emptyPosition);
        }

        /// <summary>
        /// Helper method to create a simple 3x3 board
        /// </summary>
        private byte[] Create3x3Board(byte[] values)
        {
            byte[] board = new byte[9];
            for (int i = 0; i < values.Length && i < 9; i++)
            {
                board[i] = values[i];
            }
            return board;
        }

#warning Tests should be reintroduced
        //[TestMethod]
        //public void AddState_SingleState_IsAddedSuccessfully()
        //{
        //    // Arrange
        //    var dictionary = new SolveStateDictionary<byte>();
        //    byte[] board = Create3x3Board(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 0 });
        //    SolveState state = CreateSolveState(board);
        //    long hash = 12345L;

        //    // Act
        //    dictionary.AddState(hash, state);

        //    // Assert
        //    Assert.IsTrue(dictionary.ContainsKey(hash));
        //    Assert.HasCount(1, dictionary[hash]);
        //    Assert.AreEqual(state, dictionary[hash][0]);
        //}

        //[TestMethod]
        //public void AddState_MultipleStatesWithDifferentHashes_AreStoredSeparately()
        //{
        //    // Arrange
        //    var dictionary = new SolveStateDictionary();
        //    byte[] board1 = Create3x3Board(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 0 });
        //    byte[] board2 = Create3x3Board(new byte[] { 1, 2, 3, 4, 5, 6, 7, 0, 8 });
        //    SolveState state1 = CreateSolveState(board1);
        //    SolveState state2 = CreateSolveState(board2);

        //    // Act
        //    dictionary.AddState(100L, state1);
        //    dictionary.AddState(200L, state2);

        //    // Assert
        //    Assert.HasCount(2, dictionary);
        //    Assert.IsTrue(dictionary.ContainsKey(100L));
        //    Assert.IsTrue(dictionary.ContainsKey(200L));
        //}

        //[TestMethod]
        //public void AddState_MultipleStatesWithSameHash_CreatesCollision()
        //{
        //    // Arrange
        //    var dictionary = new SolveStateDictionary();
        //    byte[] board1 = Create3x3Board(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 0 });
        //    byte[] board2 = Create3x3Board(new byte[] { 1, 2, 3, 4, 5, 6, 0, 8, 7 });
        //    SolveState state1 = CreateSolveState(board1, emptyPosition : 8);
        //    SolveState state2 = CreateSolveState(board2, emptyPosition : 7);
        //    long sameHash = 12345L;

        //    // Act
        //    dictionary.AddState(sameHash, state1);
        //    dictionary.AddState(sameHash, state2);

        //    // Assert
        //    Assert.HasCount(1, dictionary); // Only one dictionary entry
        //    Assert.HasCount(2, dictionary[sameHash]); // But two states in the list
        //    Assert.HasCount(1, dictionary); // One collision occurred
        //}

        //[TestMethod]
        //public void AddState_TrackMaxLength_WhenCollisionsIncrease()
        //{
        //    // Arrange
        //    var dictionary = new SolveStateDictionary();
        //    byte[] board1 = Create3x3Board(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 0 });
        //    byte[] board2 = Create3x3Board(new byte[] { 1, 2, 3, 4, 5, 6, 0, 8, 7 });
        //    byte[] board3 = Create3x3Board(new byte[] { 1, 2, 3, 4, 5, 0, 6, 8, 7 });
        //    SolveState state1 = CreateSolveState(board1, emptyPosition:8);
        //    SolveState state2 = CreateSolveState(board2, emptyPosition: 7);
        //    SolveState state3 = CreateSolveState(board3, emptyPosition: 5);
        //    long hash = 12345L;

        //    // Act
        //    dictionary.AddState(hash, state1);
        //    Assert.AreEqual(1, dictionary.MaxLength);

        //    dictionary.AddState(hash, state2);
        //    Assert.AreEqual(2, dictionary.MaxLength);

        //    dictionary.AddState(hash, state3);
        //    Assert.AreEqual(3, dictionary.MaxLength);

        //    // Assert
        //    Assert.HasCount(3, dictionary[hash]);
        //}

        //[TestMethod]
        //public void Exists_StateExists_ReturnsTrue()
        //{
        //    // Arrange
        //    var dictionary = new SolveStateDictionary();
        //    byte[] board = Create3x3Board(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 0 });
        //    SolveState state = CreateSolveState(board, emptyPosition : 8);
        //    long hash = 12345L;
        //    dictionary.AddState(hash, state);

        //    // Act
        //    bool exists = dictionary.Exists(hash, state);

        //    // Assert
        //    Assert.IsTrue(exists);
        //    Assert.AreEqual(1, dictionary.HitCount);
        //}

        //[TestMethod]
        //public void Exists_MultipleStatesWithSameHash_FindsCorrectState()
        //{
        //    // Arrange
        //    var dictionary = new SolveStateDictionary();
        //    byte[] board1 = Create3x3Board(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 0 });
        //    byte[] board2 = Create3x3Board(new byte[] { 1, 2, 3, 4, 5, 6, 0, 8, 7 });
        //    byte[] board3 = Create3x3Board(new byte[] { 1, 2, 3, 4, 5, 0, 6, 8, 7 });
        //    SolveState state1 = CreateSolveState(board1, emptyPosition: 8);
        //    SolveState state2 = CreateSolveState(board2, emptyPosition: 6);
        //    SolveState state3 = CreateSolveState(board3, emptyPosition: 5);
        //    long hash = 12345L;
        //    dictionary.AddState(hash, state1);
        //    dictionary.AddState(hash, state2);

        //    // Act
        //    bool existsState1 = dictionary.Exists(hash, state1);
        //    bool existsState3 = dictionary.Exists(hash, state3);

        //    // Assert
        //    Assert.IsTrue(existsState1);
        //    Assert.IsFalse(existsState3);
        //    Assert.AreEqual(1, dictionary.HitCount); // Only one hit for state1
        //    Assert.AreEqual(1, dictionary.CollisionCount); // One collision for missing state3
        //}

        //[TestMethod]
        //public void TryGetState_StateExists_ReturnsStateAndTrue()
        //{
        //    // Arrange
        //    var dictionary = new SolveStateDictionary();
        //    byte[] board = Create3x3Board(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 0 });
        //    SolveState state = CreateSolveState(board, gCost: 5, hCost: 10, emptyPosition: 8);
        //    long hash = 12345L;
        //    dictionary.AddState(hash, state);

        //    // Act
        //    bool found = dictionary.TryGetState(hash, state, out SolveState? foundState);

        //    // Assert
        //    Assert.IsTrue(found);
        //    Assert.IsNotNull(foundState);
        //    Assert.AreEqual(state, foundState);
        //    Assert.AreEqual(5, foundState.GCost);
        //    Assert.AreEqual(10, foundState.HCost);
        //}

        //[TestMethod]
        //public void TryGetState_StateDoesNotExist_ReturnsFalseAndNullState()
        //{
        //    // Arrange
        //    var dictionary = new SolveStateDictionary();
        //    byte[] board = Create3x3Board(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 0 });
        //    SolveState state = CreateSolveState(board, emptyPosition: 8);

        //    // Act
        //    bool found = dictionary.TryGetState(12345L, state, out SolveState? foundState);

        //    // Assert
        //    Assert.IsFalse(found);
        //    Assert.IsNull(foundState);
        //}

        //[TestMethod]
        //public void TryGetState_HashExistsButStateDifferent_ReturnsFalse()
        //{
        //    // Arrange
        //    var dictionary = new SolveStateDictionary();
        //    byte[] board1 = Create3x3Board(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 0 });
        //    byte[] board2 = Create3x3Board(new byte[] { 1, 2, 3, 4, 5, 6, 0, 8, 7 });
        //    SolveState state1 = CreateSolveState(board1, emptyPosition: 8);
        //    SolveState state2 = CreateSolveState(board2, emptyPosition:6);
        //    long hash = 12345L;
        //    dictionary.AddState(hash, state1);

        //    // Act
        //    bool found = dictionary.TryGetState(hash, state2, out SolveState? foundState);

        //    // Assert
        //    Assert.IsFalse(found);
        //    Assert.IsNull(foundState);
        //}

        //[TestMethod]
        //public void TryGetState_MultipleStatesWithSameHash_ReturnsCorrectState()
        //{
        //    // Arrange
        //    var dictionary = new SolveStateDictionary();
        //    byte[] board1 = Create3x3Board(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 0 });
        //    byte[] board2 = Create3x3Board(new byte[] { 1, 2, 3, 4, 5, 6, 0, 8, 7 });
        //    SolveState state1 = CreateSolveState(board1, gCost: 5, emptyPosition: 8);
        //    SolveState state2 = CreateSolveState(board2, gCost: 6, emptyPosition: 6);
        //    long hash = 12345L;
        //    dictionary.AddState(hash, state1);
        //    dictionary.AddState(hash, state2);

        //    // Act
        //    bool foundState2 = dictionary.TryGetState(hash, state2, out SolveState? result);

        //    // Assert
        //    Assert.IsTrue(foundState2);
        //    Assert.IsNotNull(result);
        //    Assert.AreEqual(6, result.GCost);
        //    Assert.AreEqual(state2, result);
        //}

        //[TestMethod]
        //public void Statistics_HitCount_IncrementsOnExistingStateFound()
        //{
        //    // Arrange
        //    var dictionary = new SolveStateDictionary();
        //    byte[] board = Create3x3Board(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 0 });
        //    SolveState state = CreateSolveState(board, emptyPosition: 8);
        //    long hash = 12345L;
        //    dictionary.AddState(hash, state);

        //    // Act
        //    dictionary.Exists(hash, state);
        //    dictionary.Exists(hash, state);
        //    dictionary.Exists(hash, state);

        //    // Assert
        //    Assert.AreEqual(3, dictionary.HitCount);
        //}

        //[TestMethod]
        //public void InitialCapacity_IsSet()
        //{
        //    // Act & Assert
        //    var dictionary = new SolveStateDictionary();
        //    Assert.IsNotNull(dictionary);
        //    // Capacity should be at least the default specified in constructor (1000000)
        //    Assert.IsGreaterThanOrEqualTo(1000000, dictionary.Capacity);
        //}
    }
}

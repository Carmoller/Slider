using PDBGenerator;
using Slider.Common.Interfaces;
using Slider.Heuristics;
using Slider.Interfaces;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Shapes;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Slider.Solver
{
    public sealed class SolverIdaStarPdb : ISolver
    {
        private record MoveRecord
        {
            public MoveDirection Direction { get; set; }
            public required byte[] Board { get; set; }
            public int NewBlankPos { get; set; }
            public int H_value { get; set; }
            public int G_value { get; set; }
            public int ManhattanDistance { get; set; }
        }
        private class PdbDescriptor
        {
            public PatternDatabase Pdb { get; set; }
            public byte[] TrackedTiles { get; set; }
            public int BlankIndex { get; set; }
            public Codec Codec { get; private set; }
            private readonly Dictionary<int, int> _tileToTrackedTileMap = [];

            public PdbDescriptor(PatternDatabase pdb)
            {
                Pdb = pdb;
                TrackedTiles = new byte[pdb.K];
                _tileToTrackedTileMap = [];
                Codec = new(pdb.GridSize, pdb.K);
                BlankIndex = pdb.BlankIndex;
            }
            public void MoveTile(int tileNumber, int newPosition)
            {
                int trackedTileIndex = _tileToTrackedTileMap[tileNumber];
                TrackedTiles[trackedTileIndex] = (byte)newPosition;
            }
            public void AddTile(int tileNumber, int trackedTilePosition)
            {
                _tileToTrackedTileMap[tileNumber] = trackedTilePosition;
            }
            public void SetBlankPosition(byte blankPosition)
            {
                TrackedTiles[BlankIndex] = blankPosition;
            }
        }

        private class TranspositionEntry
        {
            public int Iteration { get; set; }
            public required Byte[] Board { get; set; }
            public int RemainingDepth { get; set; }
            public int RefinedH { get; set; }
        }
        private PatternDatabase[]? _pdbs;

        // _tileToPdbMap, Tuple<int, int>: First int is the index into _pdbs, second int is the index in trackedtiles
        private readonly Dictionary<byte, Tuple<int, int>> _tileToPdbMap = [];
        private PdbDescriptor[]? _pdbDescriptors;
        private int _gridSize;
        private int _max_g = 0;
        private int _min_h = int.MaxValue;
        private int _pdbsLoadedForSize = -1;
        private readonly string _pdbLocation;
        private readonly IOptions _options;
        private IHeuristicCalculator _heuristicCalculator;
        private readonly Dictionary<int, List<string>> _pdbFilenamesPerSize = [];

        private void FillPdbsPerSize()
        {
            string[] pdbFiles = Directory.GetFiles(_pdbLocation, "*.pdb");
            foreach (string possiblePdb in pdbFiles)
            {
                if (possiblePdb.Contains(" - Copy"))
                    continue;
                int size = PatternDatabase.GetSizeFromPdb(possiblePdb);
                if (size > 0)
                {
                    if (_pdbFilenamesPerSize.TryGetValue(size, out List<string>? pdbs))
                    {
                        pdbs.Add(possiblePdb);
                    }
                    else
                    {
                        _pdbFilenamesPerSize[size] = [possiblePdb];
                    }
                }
            }
        }
        private void LoadPdbs(int size)
        {

            if (!_pdbFilenamesPerSize.TryGetValue(size, out List<string>? pdbFileNames))
            {
                throw new ArgumentException($"No PDB found for size {size}");
            }
            _pdbs = new PatternDatabase[pdbFileNames.Count];
            _pdbDescriptors = new PdbDescriptor[pdbFileNames.Count];

            for (int i = 0; i < pdbFileNames.Count; i++)
            {
                string filename = pdbFileNames[i];
                PatternDatabase? pdb = PatternDatabase.LoadFromFile(filename);
                if (pdb == null)
                    continue; // Not one of our PDB files
                _pdbs[i] = pdb;
                PdbDescriptor descriptor = new(pdb);
                _pdbDescriptors[i] = descriptor;

                for (int j = 0; j < pdb.TrackedTiles.Length; j++)
                {
                    byte tile = pdb.TrackedTiles[j];
                    descriptor.AddTile(tile, j);
                    _tileToPdbMap[tile] = new Tuple<int, int>(i, j);
                    _pdbDescriptors[i] = descriptor;
                }
            }
            _pdbsLoadedForSize = size;
        }
        public SolverIdaStarPdb(IOptions options)
        {
            _options = options;
            _pdbLocation = options.PdbLocation;
            FillPdbsPerSize();
            //string[] pdbPaths = new string[]
            //{
            //@"E:\src\net\Slider\4x4_01020506.pdb",
            //@"E:\src\net\Slider\4x4_03040708.pdb",
            //@"E:\src\net\Slider\4x4_09101314.pdb",
            //@"E:\src\net\Slider\4x4_111215.pdb"
            //};
            //_pdbs = new PatternDatabase[pdbPaths.Length];
            //_pdbDescriptors = new PdbDescriptor[pdbPaths.Length];


            //for (int i = 0; i < pdbPaths.Length; i++)
            //{
            //    PatternDatabase? pdb = PatternDatabase.LoadFromFile(pdbPaths[i]);
            //    if (pdb == null)
            //        continue; // Not one of our PDB files
            //    _pdbs[i] = pdb;
            //    PdbDescriptor descriptor = new PdbDescriptor(pdb);
            //    _pdbDescriptors[i] = descriptor;

            //    for (int j = 0; j < pdb.TrackedTiles.Length; j++ )
            //    {
            //        byte tile = pdb.TrackedTiles[j];
            //        descriptor.AddTile(tile, j);
            //        _tileToPdbMap[tile] = new Tuple<int, int>(i, j);
            //        _pdbDescriptors[i] = descriptor;
            //    }
            //}
        }
        private byte[] BoardDataFromTileList(List<BoardTile> board, out int blankPosition)
        {
            if (_pdbDescriptors == null)
                throw new InvalidOperationException("PDBs not initialized");
            blankPosition = -1;
            byte[] boardData = new byte[board.Count];
            foreach (BoardTile boardTile in board)
            {
                if (boardTile.Value == 0)
                {
                    blankPosition = boardTile.Row * _gridSize + boardTile.Column;
                }
                boardData[boardTile.Row * _gridSize + boardTile.Column] = boardTile.Value;
                if (_tileToPdbMap.TryGetValue(boardTile.Value, out Tuple<int, int>? pdbInfo))
                {
                    _pdbDescriptors[pdbInfo.Item1].MoveTile(boardTile.Value, boardTile.Row * _gridSize + boardTile.Column);
                }
            }
            return boardData;
        }
        public SolveResult Solve(byte[] board, ISolverOptions solverOptions, IHeuristicElementFactory heuristicElementFactory)
        {
            throw new NotImplementedException();
        }
        public SolveResult Solve(List<BoardTile> board, ISolverOptions solverOptions, IHeuristicElementFactory heuristicElementFactory)
        {
            _gridSize = (int)Math.Sqrt(board.Count);
            Dictionary<long, List<TranspositionEntry>> transpositionTable = [];
            _heuristicCalculator = heuristicElementFactory.CreateHeuristicCalculator(Span<int>.Empty, _gridSize, _options, solverOptions);
            Stopwatch sw = Stopwatch.StartNew();
            LoadPdbs(_gridSize);
            long loadTime = sw.ElapsedMilliseconds;
            sw.Restart();
            int blankPos = -1;
            SolveResult result = new();
            byte[] boardData = BoardDataFromTileList(board, out blankPos);
            int bound = GetHeuristics(boardData);
            while (true)
            {
                result.IDAStarIterations++;
                int t = Search(result.IDAStarIterations, new MoveRecord { Board = boardData, Direction = MoveDirection.None, H_value = bound }, blankPos, 0, bound, result, [], transpositionTable);
                if (t == 0)
                {
                    Console.WriteLine($"Solution found in {result.Moves.Count} moves.");
                    result.Result = SolveResultType.Solved;
                    result.Moves.Reverse();
                    break;
                }
                if (t == bound)
                    throw new InvalidOperationException("t == bound");
                bound = t;

                //transpositionTable.Clear();
            }
            sw.Stop();
            result.TimeSpent = sw.Elapsed;
            return result;
        }

        private static void SwapTiles(byte[] board, int pos1, int pos2)
        {
            (board[pos2], board[pos1]) = (board[pos1], board[pos2]);
        }

        private void RowAndColumnFromPosition(int position, out int row, out int column)
        {
            row = position / _gridSize;
            column = position % _gridSize;
        }

        public int EvaluateChildNode(int rawPdbH, int currentBlankPos)
        {
            int blankRow = currentBlankPos % _gridSize;
            int blankCol = currentBlankPos % _gridSize;
            // 1. Calculate the Manhattan distance of the blank tile to its goal position
            int blankManhattanDistance = Math.Abs(blankRow - _gridSize) +
                                         Math.Abs(blankCol - _gridSize);

            // 2. Determine the physical parity of the board layout (0 for even, 1 for odd)
            int physicalParity = blankManhattanDistance % 2;

            // 3. Determine the parity of your current PDB lookup sum
            int pdbParity = rawPdbH % 2;

            // 4. Force the heuristic to align with physical board constraints.
            // If the parities don't match, the PDB is mathematically underestimating 
            // by an odd fractional amount, so we safely bump it up by 1.
            int adjustedH = (pdbParity != physicalParity) ? rawPdbH + 1 : rawPdbH;
            // 5. Compute the actual f-value used for pruning and the next threshold update
            return adjustedH;
        }

        private MoveRecord? MoveUp(byte[] boardData, MoveDirection parentMove, int blankPos)
        {
            int newBlankPos;
            if ((blankPos / _gridSize == 0) || (parentMove == MoveDirection.Down))
            {
                newBlankPos = blankPos;
                return null;
            }
            byte[] newBoard = (byte[])boardData.Clone();
            newBlankPos = blankPos - _gridSize;
            SwapTiles(newBoard, blankPos, newBlankPos);
            return new MoveRecord { Direction = MoveDirection.Up, Board = newBoard, NewBlankPos = newBlankPos };
        }
        private MoveRecord? MoveDown(byte[] boardData, MoveDirection parentMove, int blankPos)
        {
            int newBlankPos;
            if ((blankPos / _gridSize == _gridSize - 1) || (parentMove == MoveDirection.Up))
            {
                newBlankPos = blankPos;
                return null;
            }
            byte[] newBoard = (byte[])boardData.Clone();
            newBlankPos = blankPos + _gridSize;
            SwapTiles(newBoard, blankPos, newBlankPos);
            blankPos = newBlankPos;
            return new MoveRecord { Direction = MoveDirection.Down, Board = newBoard, NewBlankPos = newBlankPos };
        }
        private MoveRecord? MoveLeft(byte[] boardData, MoveDirection parentMove, int blankPos)
        {
            int newBlankPos;
            if ((blankPos % _gridSize == 0) || (parentMove == MoveDirection.Right))
            {
                newBlankPos = blankPos;
                return null;
            }
            byte[] newBoard = (byte[])boardData.Clone();
            newBlankPos = blankPos - 1;
            SwapTiles(newBoard, blankPos, newBlankPos);
            return new MoveRecord { Direction = MoveDirection.Left, Board = newBoard, NewBlankPos = newBlankPos };
        }
        private MoveRecord? MoveRight(byte[] boardData, MoveDirection parentMove, int blankPos)
        {
            int newBlankPos;
            if ((blankPos % _gridSize == _gridSize - 1) || (parentMove == MoveDirection.Left))
            {
                newBlankPos = blankPos;
                return null;
            }
            byte[] newBoard = (byte[])boardData.Clone();
            newBlankPos = blankPos + 1;
            SwapTiles(newBoard, blankPos, newBlankPos);
            return new MoveRecord { Direction = MoveDirection.Right, Board = newBoard, NewBlankPos = newBlankPos };
        }
        private int Search(int iteration, MoveRecord previousMove, int blankPos, int g, int bound, SolveResult result, List<MoveRecord> previousMoves, Dictionary<long, List<TranspositionEntry>> transpositionTable)
        {
            int h = previousMove.H_value; // GetHeuristics(previousMove.Board);
            //Console.WriteLine($"Search g: {g} h: {h}, bound: {bound}");
            //Debug.WriteLine($"Search g: {g} h: {h}, bound: {bound}, board: {previousMove.Board.ToCommaSeparatedString()}, previous: {previousMove.Direction}");

            if (g > _max_g)
                _max_g = g;
            if (h < _min_h)
                _min_h = h;

            if ((g + h) > bound)
            {
                return g + h;
            }
            if (h == 0)
            {
                return 0;
            }

            int remainingDepth = bound - g;
            long hash = StateHashes.FastHash(previousMove.Board);

            TranspositionEntry? current = null;
            if (transpositionTable.TryGetValue(hash, out List<TranspositionEntry>? hashValues))
            {
                foreach (TranspositionEntry entry in hashValues)
                {
                    if (entry.Board.SequenceCompareTo(previousMove.Board) == 0)
                    {
                        current = entry;
                        // We've seen this board before
                        if (entry.Iteration == iteration)
                        {
                            // We saw it in this iteration, stop immediately if we saw it with more moves remaining than what we have now
                            if (entry.RemainingDepth >= remainingDepth)
                            {
                                return int.MaxValue;
                            }
                            else
                            {
                                entry.RemainingDepth = remainingDepth;
                            }
                        }
                        else
                        {
                            // We saw it in another iteration; 
                            if (entry.RemainingDepth >= remainingDepth && entry.RefinedH > bound)
                            {
                                h = Math.Max(h, entry.RefinedH);
                                if ((g + h) > bound)
                                    return entry.RefinedH;
                            }
                        }
                    }
                }
            }
            result.TotalStatesConsidered++;
            List<MoveRecord> newPreviousMoves = new(previousMoves);

            MoveRecord[] moveArray = new MoveRecord[4];
            MoveRecord? newMove = MoveUp(previousMove.Board, previousMove.Direction, blankPos);
            if (newMove != null)
            {
                newMove.G_value = g + 1;
                newMove.H_value = EvaluateChildNode(GetHeuristics(newMove.Board), newMove.NewBlankPos);
                newMove.ManhattanDistance = _heuristicCalculator.GetHeuristic(newMove.Board, _gridSize);
                moveArray[0] = newMove;
            }

            newMove = MoveDown(previousMove.Board, previousMove.Direction, blankPos);
            if (newMove != null)
            {
                newMove.G_value = g + 1;
                newMove.H_value = EvaluateChildNode(GetHeuristics(newMove.Board), newMove.NewBlankPos);
                newMove.ManhattanDistance = _heuristicCalculator.GetHeuristic(newMove.Board,  _gridSize);
                moveArray[1] = newMove;
            }
            newMove = MoveLeft(previousMove.Board, previousMove.Direction, blankPos);
            if (newMove != null)
            {
                newMove.H_value = EvaluateChildNode(GetHeuristics(newMove.Board), newMove.NewBlankPos);
                newMove.G_value = g + 1;
                newMove.ManhattanDistance = _heuristicCalculator.GetHeuristic(newMove.Board, _gridSize);
                moveArray[2] = newMove;
            }
            newMove = MoveRight(previousMove.Board, previousMove.Direction, blankPos);
            if (newMove != null)
            {
                newMove.H_value = EvaluateChildNode(GetHeuristics(newMove.Board), newMove.NewBlankPos);
                newMove.G_value = g + 1;
                newMove.ManhattanDistance = _heuristicCalculator.GetHeuristic(newMove.Board, _gridSize);
                moveArray[3] = newMove;
            }
            List<MoveRecord> moveList = moveArray.Where(p => p != null).OrderBy(p => p.H_value).ThenBy(p => p.ManhattanDistance).ToList();
            int minNextBound = int.MaxValue;
            foreach (MoveRecord move in moveList)
            //            while (orderedMoves.TryDequeue(out MoveRecord? move, out int _))
            {
                //foreach (MoveRecord? moveRecord in newPreviousMoves)
                //{
                //    if (moveRecord.Board.SequenceEqual(move.Board))
                //    {
                //        continue;
                //    }
                //}
                newPreviousMoves.Add(move);
                int t = Search(iteration, move, move.NewBlankPos, g + 1, bound, result, newPreviousMoves, transpositionTable);
                if (t == 0)
                {
                    RowAndColumnFromPosition(blankPos, out int toRow, out int toCol);
                    RowAndColumnFromPosition(move.NewBlankPos, out int fromRow, out int fromCol);
                    result.Moves.Add(new Move { FromRow = fromRow, FromColumn = fromCol, ToRow = toRow, ToColumn = toCol });
                    return 0;
                }
                minNextBound = Math.Min(minNextBound, t);
            }
            if (hashValues == null)
            {
                hashValues = [];
                transpositionTable[hash] = hashValues;
            }
            if (current == null)
                hashValues.Add(new TranspositionEntry { Board = previousMove.Board, RemainingDepth = remainingDepth, RefinedH = minNextBound - g });
            return minNextBound;
        }

        public int GetHeuristic(List<BoardTile> board, IHeuristicElementFactory heuristicElementFactory)
        {
            _gridSize = (int)Math.Sqrt(board.Count);
            if (_pdbsLoadedForSize != _gridSize)
            {
                _pdbs = null;
                _pdbDescriptors = null;
                LoadPdbs(_gridSize);
            }
            byte[] boardData = BoardDataFromTileList(board, out int blankPosition);
            return GetHeuristics(boardData);
        }

        public int GetHeuristics(byte[] boardData)
        {
            int h1 = GetHeuristics(boardData, false);
            int h2 = GetHeuristics(boardData, true);
            return Math.Max(h1, h2);
        }
        public int GetHeuristics(byte[] boardData, bool swap)
        {
            if (swap)
            {
                return GetSwappedHeuristics(boardData);
            }
            // Gather the position of the numbers that fit into each PDB
            byte blankPosition = 255;
            for (int i = 0; i < boardData.Length; i++)
            {
                if (boardData[i] == 0)
                {
                    blankPosition = (byte)i;
                    continue;
                }
                Tuple<int, int> pdbIndex = _tileToPdbMap[boardData[i]];
                _pdbDescriptors![pdbIndex.Item1].MoveTile(boardData[i], i);
            }

            int h = 0;
            byte blankValue = (byte)boardData.Length;
            for (int i = 0; i < _pdbDescriptors!.Length; i++)
            {
                _pdbDescriptors[i].SetBlankPosition(blankPosition);

                long encoded = _pdbDescriptors[i].Codec.Encode(_pdbDescriptors[i].TrackedTiles);
                h += _pdbDescriptors[i].Pdb.GetDistance(encoded);
            }
            return h;
        }

        private int GetSwappedHeuristics(byte[] boardData)
        {
#warning should be combined!!!
            byte[] swapBoardData = (byte[])boardData.Clone();
            for (int i = 0; i < boardData.Length; i++)
            {
                int reflectedPosition = (i % _gridSize) * _gridSize + i / _gridSize;
                int value = boardData[i] - 1;
                int reflectedValue = (value % _gridSize) * _gridSize + value / _gridSize;
                swapBoardData[reflectedPosition] = (byte)(value + 1);
            }
            byte blankPositionSwap = 255;
            for (int i = 0; i < swapBoardData.Length; i++)
            {
                if (swapBoardData[i] == 0)
                {
                    blankPositionSwap = (byte)i;
                    continue;
                }
                Tuple<int, int> pdbIndex = _tileToPdbMap[swapBoardData[i]];
                _pdbDescriptors![pdbIndex.Item1].MoveTile(swapBoardData[i], i);
            }

            int h = 0;
            for (int i = 0; i < _pdbDescriptors!.Length; i++)
            {
                _pdbDescriptors[i].SetBlankPosition(blankPositionSwap);
                long encoded = _pdbDescriptors[i].Codec.Encode(_pdbDescriptors[i].TrackedTiles);
                h += _pdbDescriptors[i].Pdb.GetDistance(encoded);
            }
            return h;
        }
    }
}

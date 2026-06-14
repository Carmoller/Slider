using PDBGenerator;
using Slider.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Automation;

namespace Slider.Heuristics
{
    public class HeuristicPdb : IHeuristicElement
    {
        private class PdbDescriptor
        {
            public PatternDatabase Pdb { get; set; }
            public byte[] TrackedTiles { get; set; }
            public int BlankIndex { get; set; }
            public Codec Codec { get; private set; }
            private Dictionary<int, int> _tileToTrackedTileMap = new();

            public PdbDescriptor(PatternDatabase pdb)
            {
                Pdb = pdb;
                TrackedTiles = new byte[pdb.K];
                _tileToTrackedTileMap = new();
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

        private PdbDescriptor[]? _pdbDescriptors;
        private Dictionary<int, List<string>> _pdbFilenamesPerSize = new();
        private PatternDatabase[]? _pdbs;
        private string _pdbLocation;
        private Dictionary<byte, Tuple<int, int>> _tileToPdbMap = new();
        private int _pdbsLoadedForSize = -1;

        public string Name { get { return "Pdb"; } }
        public bool IsAdditive { get { return false; } }

        public HeuristicStatistics Statistics { get; }

        public HeuristicPdb(IOptions options)
        {
            Statistics = new();
            _pdbLocation = options.PdbLocation;
            FillPdbsPerSize();
        }

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
                        _pdbFilenamesPerSize[size] = new List<string> { possiblePdb };
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
                PdbDescriptor descriptor = new PdbDescriptor(pdb);
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

        public int Calculate(byte[] boardData, int gridSize)
        {
            if (_pdbsLoadedForSize == -1)
            {
                int size = gridSize;
                LoadPdbs(size);
            }
            Stopwatch sw = Stopwatch.StartNew();
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
            Statistics.NumberOfCalls++;
            Statistics.TicksSpent += sw.ElapsedTicks;
            return h;
        }

        public int Calculate(byte[,] board, (byte row, byte col)[] goalPositions, byte gridSize)
        {
            throw new NotImplementedException();
        }
    }
}

using Slider.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace Slider.BfsSolver
{
    public class BfsSolver
    {
        private int size;
        // List to store the final sequence of moves
        public List<Move> MoveHistory { get; private set; } = new();
        private Dictionary<TilePosition, bool> locked = new Dictionary<TilePosition, bool>();
        private byte[,] grid;

        public BfsSolver(byte[,] initialGrid)
        {
            grid = initialGrid;
            size = grid.GetLength(0);
        }

        private List<TilePosition>? FindPath(TilePosition start, int targetRow, int targetCol)
        {
            var queue = new Queue<List<TilePosition>>();
            var visited = new HashSet<TilePosition>();

            queue.Enqueue(new List<TilePosition> { new TilePosition { Row = start.Row, Col = start.Col } });
            visited.Add(start);

            while (queue.Count > 0)
            {
                var path = queue.Dequeue();
                var current = path[^1];

                if (current.Row == targetRow && current.Col == targetCol) return path;

                foreach (var dir in new[] { (-1, 0), (1, 0), (0, -1), (0, 1) })
                {
                    TilePosition next = new TilePosition { Row = current.Row + dir.Item1, Col = current.Col + dir.Item2 };

                    if (next.Row >= 0 && next.Row < size && next.Col >= 0 && next.Col < size)
                    {
                        // Do not step on locked tiles
                        bool isLocked = false;
                        if (locked.TryGetValue(next, out bool currentValue))
                        {
                            isLocked = currentValue;
                        }


                        if (!isLocked && !visited.Contains(next))
                        {
                            visited.Add(next);
                            var newPath = new List<TilePosition>(path) { next };
                            queue.Enqueue(newPath);
                        }
                    }
                }
            }
            return null; // No path found
        }
        public void SolveTopRowTrain()
        {
            // 1. Solve standard tiles from left to right (except the last two)
            for (int c = 0; c < size - 2; c++)
            {
                int targetTile = c + 1;
                MoveTileTo(targetTile, 0, c);
                locked[new TilePosition { Row = 0, Col = c }] = true; // Lock solved tiles in place
            }

            // 2. Identify the last two tiles of the row
            int secondLastTile = size - 1; // e.g., Tile 3 in a 4x4
            int lastTile = size;           // e.g., Tile 4 in a 4x4

            // 3. Set up the Train vertically in Row 1
            // First, position the second-to-last tile at (1, size - 2)
            MoveTileTo(secondLastTile, 1, size - 1);
            locked[new TilePosition { Row = 1, Col = size - 1 }] = true; // Lock temporarily so the next tile doesn't push it

            // Next, position the last tile immediately below the second to last tile
            MoveTileTo(lastTile, 2, size - 1);
            locked[new TilePosition { Row = 2, Col = size - 1 }] = true; // Lock temporarily
            // 4. Position the empty space (0) into the engine room (the top-right corner)
            // Unlock the train tiles so the pathfinder can safely skirt past or shift them if needed
            MoveEmptySpaceTo(0, size - 1);
            locked.Remove(new TilePosition { Row = 1, Col = size - 1 });
            locked.Remove(new TilePosition { Row = 2, Col = size - 1 });

            // 5. Fire the Train Macro to pull them into place
            ExecuteRowMacroTrain();

            // 6. Permanently lock the entire completed top row
            for (int c = 0; c < size; c++)
            {
                locked[new TilePosition { Row = 0, Col = c }] = true;
            }
        }

        public void SolveTopRow()
        {
            // 1. Solve standard tiles from left to right (except last two)
            for (int c = 0; c < size - 2; c++)
            {
                int targetTile = c + 1;
                MoveTileTo(targetTile, 0, c);
                locked[new TilePosition { Row = 0, Col = c }] = true; // Lock it in place
            }

            // 2. Special Case: Setup second-to-last and last tile
            int secondLast = size - 1;
            int last = size;

            // Move second-to-last tile to the top-right corner
            MoveTileTo(secondLast, 0, size - 1);
            // Temporarily lock it so the last tile doesn't displace it
            locked[new TilePosition { Row = 0, Col = size - 1 }] = true;

            // Move last tile directly underneath the corner
            MoveTileTo(last, 1, size - 1);
            // Also temporarily lock that one
            locked[new TilePosition { Row = 1, Col = size - 1 }] = true;

            MoveEmptySpaceTo(1, size - 2);

            // 3. Execute the Row End Macro Rotation
            locked.Remove(new TilePosition { Row = 0, Col = size - 1 }); // Unlock second-to-last tile
            locked.Remove(new TilePosition { Row = 1, Col = size - 1 }); // Unlock last tile
            ExecuteRowRotationMacro();
            // 4. Permanently lock the entire top row
            for (int c = 0; c < size; c++) locked[new TilePosition { Row = 0, Col = c }] = true;
        }
        private void ExecuteRowMacroTrain()
        {
            // SETUP: 
            // - Empty space (0) is parked at the top-right corner: (0, size - 1)
            // - The last tile is directly below it at: (1, size - 1)
            // - The second-to-last tile is to the left at: (1, size - 2)

            // 1. Move empty space DOWN (Slides the last tile UP into the corner)
            MoveEmptySpaceTo(1, size - 1);
            locked[new TilePosition { Row = 0, Col = size - 1 }] = true;

            // 2. Move empty space DOWN (Slides the second-to-last tile UP)
            MoveEmptySpaceTo(2, size - 1);
            locked[new TilePosition { Row = 1, Col = size - 1 }] = true;
            // 3. Move empty space UP (Slides the second-to-last tile UP into its final spot)
            MoveEmptySpaceTo(0, size - 2);

            // 3. Move empty space UP (Slides the second-to-last tile UP into its final spot)
            locked.Remove(new TilePosition { Row = 0, Col = size - 1 });
            locked.Remove(new TilePosition { Row = 1, Col = size - 1 });
            MoveEmptySpaceTo(0, size - 1);
            MoveEmptySpaceTo(1, size - 1);
        }

        private void ExecuteRowRotationMacro()
        {
            MoveEmptySpaceTo(0, size - 2);
            MoveEmptySpaceTo(0, size - 1);
            MoveEmptySpaceTo(1, size - 1);
//            MoveEmptySpaceTo(1, size - 2);
        }
        private void ExecuteColumnRotationMacro()
        {
            // Assumes empty space is brought to (size-2, 1) via pathfinder first
            // Move empty space: Left -> Down -> Right -> Up -> Left
            MoveEmptySpaceTo(size - 2, 0);
            MoveEmptySpaceTo(size - 1, 0);
            MoveEmptySpaceTo(size - 1, 1);
            MoveEmptySpaceTo(size - 2, 1);
            MoveEmptySpaceTo(size - 2, 0);
        }
        private void ExecuteColumnRotationMacroTrain()
        {
            // Assumes empty space is brought to (size-2, 0) via pathfinder first
            MoveEmptySpaceTo(size - 1, 0);
            MoveEmptySpaceTo(size - 1, 1);
        }


        public void SolveLeftColumn()
        {
            // 1. Solve standard column tiles down to the second-to-last
            for (int r = 1; r < size - 2; r++)
            {
                int targetTile = (r * size) + 1;
                MoveTileTo(targetTile, r, 0);
                locked[new TilePosition { Row = r, Col = 0 }] = true;
            }

            // 2. Special Case: Last two column tiles
            int secondLastTile = ((size - 2) * size) + 1;
            int lastTile = ((size - 1) * size) + 1;

            // Position second-to-last at the bottom-left corner
            MoveTileTo(secondLastTile, size - 1, 0);
            locked[new TilePosition { Row = size - 1, Col = 0 }] = true;

            // Position last tile to the immediate right of the corner
            MoveTileTo(lastTile, size - 1, 1);

            // 3. Execute Column End Macro Rotation
            locked[new TilePosition { Row = size - 1, Col = 0 }] = false;
            ExecuteColumnRotationMacro();

            // 4. Permanently lock the left column
            for (int r = 0; r < size; r++) locked[new TilePosition { Row = r, Col = 0 }] = true;
        }
        public void SolveLeftColumnTrain()
        {
            // 1. Solve standard column tiles except the last two
            for (int r = 0; r < size - 2; r++)
            {
                int targetTile = (r * size) + 1;
                MoveTileTo(targetTile, r, 0);
                locked[new TilePosition { Row = r, Col = 0 }] = true;
            }

            // 2. Special Case: Last two column tiles
            int secondLastTile = ((size - 2) * size) + 1;
            int lastTile = ((size - 1) * size) + 1;

            // 3. Set up the Train horizontally in last row
            MoveTileTo(secondLastTile, size - 1, 0);
            locked[new TilePosition { Row = size - 1, Col = 0 }] = true;

            // Position last tile in the bottom-right corner
            MoveTileTo(lastTile, size - 1, 1);
            locked[new TilePosition { Row = size - 1, Col = 1 }] = true;

            // Move empty to the bottom right corner
            MoveEmptySpaceTo(size-2,0);

            // unlock the two train tiles so we can move them into place
            locked.Remove(new TilePosition { Row = size - 1, Col = 0 });
            locked.Remove(new TilePosition { Row = size - 1, Col = 1 });

            // Execute Column End Macro Rotation
            ExecuteColumnRotationMacroTrain();

            // 4. Permanently lock the left column
            for (int r = 0; r < size; r++) locked[new TilePosition { Row = r, Col = 0 }] = true;
        }

        public void MoveTileTo(int tileValue, int targetRow, int targetCol)
        {
            while (true)
            {
                TilePosition tilePos = FindTilePosition(tileValue);
                if (tilePos.Row == targetRow && tilePos.Col == targetCol) break;

                // Find the shortest path for the tile to reach its destination
                List<TilePosition>? tilePath = FindPath(tilePos, targetRow, targetCol);
                if (tilePath == null || tilePath.Count < 2) break;

                // Next position the tile needs to step into
                var nextStep = tilePath[1];

                // Temporarily lock the target tile so the empty space doesn't move it by accident
                locked[new TilePosition { Row = tilePos.Row, Col = tilePos.Col }] = true;

                // Move the empty space to the position where the tile needs to go
                MoveEmptySpaceTo(nextStep.Row, nextStep.Col);

                // Unlock the tile so they can swap places
                locked.Remove(new TilePosition { Row = tilePos.Row, Col = tilePos.Col });

                // Swap the empty space and the target tile
                SwapTiles(FindEmptySpacePosition(), tilePos);
            }
        }

        private void MoveEmptySpaceTo(int targetRow, int targetCol)
        {
            TilePosition emptyPos = FindEmptySpacePosition();
            if (emptyPos.Row == targetRow && emptyPos.Col == targetCol) return;
            var emptyPath = FindPath(emptyPos, targetRow, targetCol);
            if (emptyPath == null || emptyPath.Count < 2)
            {
                throw new Exception("Deadlock: Empty space is trapped by locked tiles!");
            }

            for (int i=1; i<emptyPath.Count; i++)
            {
                var currentEmptyPos = FindEmptySpacePosition();
                // Move the empty space step by step along its path
                var nextStep = emptyPath[i];
                SwapTiles(currentEmptyPos, nextStep);
            }
        }

        // Swaps two coordinates in the grid array
        private void SwapTiles(TilePosition emptyPos, TilePosition tilePos)
        {
            // 1. Store the history
            MoveHistory.Add(new Move { FromRow = tilePos.Row , FromColumn = tilePos.Col, ToRow = emptyPos.Row, ToColumn= emptyPos.Col});    // Tile moves UP into empty space

            // 2. Perform the physical swap in your grid array
            byte temp = grid[emptyPos.Row, emptyPos.Col];
            grid[emptyPos.Row, emptyPos.Col] = grid[tilePos.Row, tilePos.Col];
            grid[tilePos.Row, tilePos.Col] = temp;
        }

        // Scans the grid to locate the empty space (0)
        private TilePosition FindEmptySpacePosition()
        {
            return FindTilePosition(0);
        }

        // Scans the grid to locate any specific tile number
        private TilePosition FindTilePosition(int value)
        {
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    if (grid[r, c] == value) return new TilePosition { Row = r, Col = c };
                }
            }
            throw new Exception($"Tile {value} missing from the board.");
        }

    }
}

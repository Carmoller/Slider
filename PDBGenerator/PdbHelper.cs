using System;
using System.Collections.Generic;
using System.Text;

namespace PDBGenerator
{
    public class PdbHelper
    {
        // Define the specific tiles this PDB is tracking (e.g., Tiles 1, 2, and 3)
        // The order in this array defines the identity of the slots for the Lehmer code!
        private int[] TrackedTiles;

        public PdbHelper(int[] trackedTiles)
        {
            TrackedTiles = trackedTiles;
        }
            
    /// <summary>
        /// Extracts the precise board positions of the tracked tiles to safely feed the Codec.
        /// </summary>
        /// <param name="fullBoard">A flat array of size 9 representing the current board layout.</param>
        public long EncodeCurrentState(byte[] fullBoard, Codec codec)
        {
            byte[] positions = new byte[TrackedTiles.Length];
            byte blankPosition = 255;

            // Scan the board once to find where everything is
            for (byte boardPos = 0; boardPos < fullBoard.Length; boardPos++)
            {
                byte tileValue = fullBoard[boardPos];

                if (tileValue == 0) // Assuming 0 represents the empty/blank tile
                {
                    blankPosition = boardPos;
                    continue;
                }

                // Check if this tile is one of the 3 tiles tracked by this database
                int trackedIdx = Array.IndexOf(TrackedTiles, tileValue);
                if (trackedIdx != -1)
                {
                    // Crucial: Store the BOARD POSITION, ordered by the tile's identity
                    positions[trackedIdx] = boardPos;
                }
            }

            // Now it is perfectly safe to call your Codec
            return codec.Encode(positions, blankPosition);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace PDBGenerator
{
    internal class State
    {
        private int[] Positions;

        public State(int boardSize, int patternTileCount)
        {
            Positions = new int[patternTileCount];
            for (int i = 0; i < patternTileCount; i++)
            {
                Positions[i] = i;
            }
        }
    }
}

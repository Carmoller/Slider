using Slider.Common;
using Slider.Common.Interfaces;
using Slider.Solver;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Interfaces
{
    public interface IStateInfoFactory
    {
        void GetAvailableMoves(StateInfo currentState,
            int gridSize,
            IChunkedStructPool<StateInfo> stateInfoPool, 
            IChunkedArrayPool<byte> arrayPool,
            RefAction<StateInfo> processState);
    }
}

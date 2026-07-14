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
        void GetAvailableMoves<TContext>(ref StateInfo currentState,
            int gridSize,
            IChunkedStructPool<StateInfo> stateInfoPool,
            IChunkedArrayPoolUnsafe arrayPool,
            ref TContext context,
            RefAction<StateInfo, TContext> processState) where TContext : struct;
    }
}

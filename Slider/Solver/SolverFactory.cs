using Slider.Common;
using Slider.Common.Interfaces;
using Slider.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Automation;
using System.Windows.Markup;

namespace Slider.Solver
{
    public class SolverFactory : ISolverFactory
    {
        IOptions _options;
        IStateInfoFactory _stateInfoFactory;

        public SolverFactory(IOptions options, IStateInfoFactory stateInfoFactory)
        {
            _options = options;
            _stateInfoFactory = stateInfoFactory;
        }
        public ISolver Create(int gridSize, int heuristic)
        {
            foreach (SolverDescriptor descriptor in _options.SolverSelector)
            {
                if (descriptor.LowHeuristic > heuristic)
                    continue;
                if (descriptor.HighHeuristic < heuristic)
                    continue;

                // descriptor matches all criteria - we've found the one
                return descriptor.Solver;
            }
            throw new InvalidOperationException($"No solver found for gridSize = {gridSize}, heuristic = {heuristic}");
        }

        public ISolver Create(SolverType type)
        {
            switch (type)
            {
                case SolverType.BFSSolver:
                    return new DynamicWeightAStarSolver(_options, _stateInfoFactory);
                default:
                    throw new InvalidOperationException($"Requesting unknown solver type: {type.ToString()}");
            }
        }
    }
}

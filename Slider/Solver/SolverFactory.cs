using Slider.Common;
using Slider.Common.Interfaces;
using Slider.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Automation;

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
                if (descriptor.LowGridSize > gridSize)
                    continue;
                if (descriptor.HighGridSize < gridSize)
                    continue;
                if (descriptor.LowHeuristic > heuristic)
                    continue;
                if (descriptor.HighHeuristic < heuristic)
                    continue;

                // descriptor matches all criteria - we've found the one
                ISolver? solver = Activator.CreateInstance(descriptor.Solver, _options, _stateInfoFactory,  descriptor.SolverParameters) as ISolver;
                if (solver == null)
                    throw new InvalidOperationException($"Could not create solver of type {descriptor.Solver.Name} for gridSize = {gridSize}, heuristic = {heuristic}");

                return solver;
            }
            throw new InvalidOperationException($"No solver found for gridSize = {gridSize}, heuristic = {heuristic}");
        }

        public ISolver Create(SolverType type)
        {
            switch (type)
            {
                case SolverType.WeightedAStar:
                    return new WeightedAStarSolver(_options, _stateInfoFactory);
                case SolverType.BidirectionalAStar:
                    return new BidirectionalAStarSolver(_options, _stateInfoFactory);
                default:
                    throw new InvalidOperationException($"Requesting unknown solver type: {type.ToString()}");
            }
        }
    }
}

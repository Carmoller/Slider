using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Slider.Solver
{
    [DebuggerDisplay("Keys:{Count}, CollisionCount={CollisionCount}, HitCount={HitCount}, MaxLength={MaxLength}")]
    public class SolveStateDictionary : Dictionary<long, List<SolveState>>
    {
        public long CollisionCount { get; private set; }
        public long HitCount { get; private set;  }
        public int MaxLength { get; private set; }
        public SolveStateDictionary() : base(1000000)
        {
        }
        public bool Exists(long hash, SolveState state)
        {
            if (TryGetValue(hash, out List<SolveState>? states))
            {
                foreach (SolveState closedState in states)
                {
                    if (closedState.Equals(state))
                    {
                        HitCount++;
                        return true;
                    }
                }
            }
            return false;
        }

        public void AddState(long hash, SolveState state)
        {
            if (!TryGetValue(hash, out List<SolveState>? list))
            {
                list = new();
                Add(hash, list);
            }
            if (list.Count > 0)
            {
                CollisionCount++;
            }
            list.Add(state);
            if (list.Count > MaxLength)
            {
                MaxLength = base[hash].Count;
            }
        }

        public bool TryGetState(long hash, SolveState queryState, out SolveState? existingState)
        {
            existingState = null;
            if (!TryGetValue(hash, out List<SolveState>? existingStates))
                return false;
            foreach (SolveState state in existingStates)
            {
                if (state.Equals(queryState))
                {
                    existingState = state;
                    return true;
                }
            }
            return false;
        }
    }
}

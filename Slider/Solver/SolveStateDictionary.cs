using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Slider.Solver
{
    [DebuggerDisplay("Keys:{Count}, CollisionCount={CollisionCount}, HitCount={HitCount}, MaxLength={MaxLength}")]
    public class SolveStateDictionary<T> : Dictionary<long, List<T>> where T: struct
    {
        public long CollisionCount { get; private set; }
        public long HitCount { get; private set; }
        public int MaxLength { get; private set; }
        public SolveStateDictionary() : base(1000000)
        {
        }
        public bool Exists(long hash, T state)
        {
            if (TryGetValue(hash, out List<T>? states))
            {
                foreach (T existingState in states)
                {
                    if (existingState.Equals(state))
                    {
                        HitCount++;
                        return true;
                    }
                }
            }
            return false;
        }

        public void AddState(long hash, T state)
        {
            if (!TryGetValue(hash, out List<T>? list))
            {
                list = new();
                Add(hash, list);
            }
            if (list.Count > 0)
            {
                CollisionCount++;
                foreach (T existingState in list)
                {
                    if (existingState.Equals(state))
                        return;
                }
            }
            list.Add(state);
            if (list.Count > MaxLength)
            {
                MaxLength = base[hash].Count;
            }
        }

        public bool TryGetState(long hash, T queryState, out T existingState)
        {
            existingState = default(T);
            if (!TryGetValue(hash, out List<T>? existingStates))
                return false;
            foreach (T state in existingStates)
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

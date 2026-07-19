using Slider.Common;
using Slider.Common.Interfaces;
using Slider.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Solver
{
    public struct StateInfo : IStateInfo, IEquatable<StateInfo>
    {
        public static readonly StateInfo Empty = default;
        public int NodeIndex { get; set; }
        public int ParentIndex { get; set; }
        public PointerToken BoardToken { get; set; }
        public int BoardArrayIndex { get; set; }
        public int BlankPos { get; set; }
        public long Hash { get; set; }
        public int BestG { get; set; }
        public int CurrentG { get; set; }
        public int CurrentH { get; set; }
        public double CurrentF { get; set; }
        public MoveDirection PreviousMove { get; set; }

        public bool Equals(StateInfo other)
        {
            if (BlankPos != other.BlankPos) return false;
            return BoardToken.AsSpan().SequenceEqual(other.BoardToken.AsSpan());
        }

        public override bool Equals(object? obj)
        {
            if (obj == null)
                return false;
            StateInfo other = (StateInfo)obj;
            if (BlankPos != other.BlankPos) return false;
            return BoardToken.AsSpan().SequenceEqual(other.BoardToken.AsSpan());
        }
        public override int GetHashCode()
        {
            return (int)StateHashes.FastHash(BoardToken.AsSpan());
        }

        public override string ToString()
        {
            return $"Nodeindex: {NodeIndex}, ParentIndex: {ParentIndex}, CurrentG: {CurrentG}, CurrentH: {CurrentH},  Board: " + string.Join(',', BoardToken.AsSpan().ToArray());
        }
    }
}

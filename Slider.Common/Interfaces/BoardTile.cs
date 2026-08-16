using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Slider.Common.Interfaces
{
    [DebuggerDisplay("{Value} @ ({Row}, {Column})")]
    public class BoardTile
    {
        public byte Value { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
        public bool IsEmpty { get { return Value == 0; } }
        public bool IsHighlighted { get; set; }
        public int CompareTo(BoardTile? other)
        {
            if (other == null)
                return 1;
            if (Value > other.Value)
                return 1;
            else if (Value < other.Value)
                return -1;
            else
                return 0;
        }

        public BoardTile DeepClone()
        {
            BoardTile newTile = new()
            {
                Value = Value,
                Row = Row,
                Column = Column,
                IsHighlighted = IsHighlighted,
            };
            return newTile;
        }
    }
}

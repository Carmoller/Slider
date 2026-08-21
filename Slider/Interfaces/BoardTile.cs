using Slider.SliderEventArgs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Slider.Interfaces
{
    [DebuggerDisplay("{Value} @ ({Row}, {Column})")]
    public class BoardTile
    {
        public event EventHandler<TilePositionChangedEventArgs>? TilePositionChanged;
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

        public void MoveTo(int row, int column)
        {
            int oldRow = Row;
            int oldColumn = Column;
            Row = row;
            Column = column;
            TilePositionChanged?.Invoke(this, new TilePositionChangedEventArgs { OldRow = oldRow, OldColumn = oldColumn, NewRow = row, NewColumn = column });
        }
    }
}

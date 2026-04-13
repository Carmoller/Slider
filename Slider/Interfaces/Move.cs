using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Slider.Interfaces
{
    [DebuggerDisplay("From: ({FromRow}, {FromColumn}), To: ({ToRow}, {ToColumn})")]
    public class Move
    {
        public int FromRow { get; set; }
        public int FromColumn { get; set; }
        public int ToRow { get; set; }
        public int ToColumn { get; set; }
    }
}

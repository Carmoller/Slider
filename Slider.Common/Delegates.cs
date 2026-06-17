using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Common
{
    public delegate void RefInitializer<T, TState>(ref T target, TState state);
    public delegate void RefAction<T>(ref T arg);
}

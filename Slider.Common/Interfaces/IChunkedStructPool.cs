using Slider.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Common.Interfaces
{
    public interface IChunkedStructPool<T>
    {
        int Get<TState>(TState state, RefInitializer<T, TState> initializer);
        ref T GetRef(int index);
        public void Release(int index, RefAction<T>? Dispose = null);

    }
}

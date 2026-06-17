using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Common.Interfaces
{
    public interface IChunkedArrayPool<T>
    {
        int Get();
        T[] GetArray(int index);
        void Release(int index);
    }
}

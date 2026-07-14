using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Common.Interfaces
{
    public interface IChunkedArrayPoolUnsafe : IDisposable
    {
        int Get();
        PointerToken GetToken();
        void Release(int index);
        void Release(PointerToken token);
    }
}

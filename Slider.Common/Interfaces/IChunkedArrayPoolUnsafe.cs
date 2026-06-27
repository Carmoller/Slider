using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Common.Interfaces
{
    public interface IChunkedArrayPoolUnsafe : IDisposable
    {
        int Get();
        public PointerToken GetToken();
        public void Release(PointerToken token);
    }
}

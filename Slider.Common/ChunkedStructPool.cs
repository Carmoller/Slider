using System;
using System.Collections.Generic;
using System.Text;
using Slider.Common.Interfaces;

namespace Slider.Common
{
    public class ChunkedStructPool<T> : IChunkedStructPool<T>  where T : struct
    {
        public static int NoIndex = -1;
        private int _chunkSize;
        private List<T[]> _chunks = new();
        private Stack<int> _freeIndices = new();
        public ChunkedStructPool(int chunkSize)
        {
            _chunkSize = chunkSize;
            AllocateChunk();
        }

        public int Get<TState>(TState state, RefInitializer<T, TState> initializer)
        {
            if (_freeIndices.Count == 0)
            {
                AllocateChunk();
            }

            int index = _freeIndices.Pop();
            ref T node = ref GetRef(index);
            initializer(ref node, state);
            return index;
        }

        public ref T GetRef(int index)
        {
            int chunkIdx = index / _chunkSize;
            int slotIdx = index % _chunkSize;
            return ref _chunks[chunkIdx][slotIdx];
        }

        private void AllocateChunk()
        {
            T[] chunk = new T[_chunkSize];
            int baseIndex = _chunks.Count * _chunkSize;
            _chunks.Add(chunk);
            _freeIndices.EnsureCapacity(_freeIndices.Count + _chunkSize);
            for (int i = _chunkSize - 1; i >= 0; i--)
            {
                _freeIndices.Push(baseIndex + i);
            }
        }
        public void Release(int index, RefAction<T>? Dispose = null)
        {
            ref T node = ref GetRef(index);
            Dispose?.Invoke(ref node);
            node = default; // Wipe out data/indices back to default values
            _freeIndices.Push(index);
        }
    }
}

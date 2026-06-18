using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Slider.Common.Interfaces;

namespace Slider.Common
{
    public class ChunkedArrayPool<T> : IChunkedArrayPool<T>  
    {
        public static int NoIndex = -1;
        private int _chunkSize;
        private int _arraySize;
        private List<T[][]> _chunks = new();
        private Stack<int> _freeIndices = new();
        public ChunkedArrayPool(int chunkSize, int arraySize)
        {
            _chunkSize = chunkSize;
            _arraySize = arraySize;
            AllocateChunk();
        }

        public int Get()
        {
            if (_freeIndices.Count == 0)
            {
                AllocateChunk();
            }

            int index = _freeIndices.Pop();
            if (index == 0)
            {
                int a = 1;
            }
            return index;
        }

        public T[] GetArray(int index)
        {
            int chunkIdx = index / _chunkSize;
            int slotIdx = index % _chunkSize;
            return _chunks[chunkIdx][slotIdx];
        }

        private void AllocateChunk()
        {
            T[][] chunk = new T[_chunkSize][];
            for (int i = 0; i < _chunkSize; i++)
            {
                chunk[i] = new T[_arraySize];
            }
            int baseIndex = _chunks.Count * _chunkSize;
            _chunks.Add(chunk);
            _freeIndices.EnsureCapacity(_freeIndices.Count + _chunkSize);
            for (int i = _chunkSize - 1; i >= 0; i--)
            {
                _freeIndices.Push(baseIndex + i);
            }
        }
        public void Release(int index)
        {
            if (index == -1)
                return;
#if DEBUG
            if (_freeIndices.Contains(index))
            {
                throw new InvalidOperationException($"Releasing index which is already in queue: {index}");
            }
#endif
            _freeIndices.Push(index);
        }
    }
}

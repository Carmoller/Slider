using Slider.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Slider.Common
{
    public unsafe readonly struct PointerToken
    {
        public byte* Address { get; }
        public int Length { get; }
        public int Index { get; }
        public PointerToken(byte* address, int length, int index)
        {
            Address = address;
            Length = length;
            Index = index;
        }

        // Read/Write directly from the pointer when processing
        public Span<byte> AsSpan() => new Span<byte>(Address, Length);
    }

    public unsafe class ChunkedArrayPoolUnsafe: IDisposable, IChunkedArrayPoolUnsafe
    {
        private struct ChunkDescriptor
        {
            public byte* RootAddress { get; set; }
            public int Index { get; set; }

        }
        private bool _isDisposed;
        public static int NoIndex = -1;
        private int _chunkSize;
        private int _arraySize;
        private List<ChunkDescriptor> _chunks = new();
        private Stack<int> _freeIndices = new();
        public ChunkedArrayPoolUnsafe(int chunkSize, int arraySize)
        {
            _chunkSize = chunkSize;
            _arraySize = arraySize;
            AllocateChunk();
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            foreach (ChunkDescriptor chunk in _chunks)
            {
                NativeMemory.Free(chunk.RootAddress);
            }
            _freeIndices.Clear();
            _isDisposed = true;
        }

        public int Get()
        {
            if (_freeIndices.Count == 0)
            {
                AllocateChunk();
            }

            int index = _freeIndices.Pop();
            return index;
        }

        public unsafe PointerToken GetToken()
        {
            if (_freeIndices.Count == 0)
            {
                AllocateChunk();
            }

            int index = _freeIndices.Pop();
            int chunkIdx = index / _chunkSize;
            int slotIdx = index % _chunkSize;
            ChunkDescriptor chunk = _chunks[chunkIdx];
            return new PointerToken(_chunks[chunkIdx].RootAddress + slotIdx * _arraySize, _arraySize, index);
        }

        private void AllocateChunk()
        {
            ChunkDescriptor chunkDescriptor = new ChunkDescriptor { RootAddress = (byte*)NativeMemory.Alloc((nuint)(_chunkSize * _arraySize)) };

            int baseIndex = _chunks.Count * _chunkSize;
            _chunks.Add(chunkDescriptor);
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
        public void Release(PointerToken token)
        {
            Release(token.Index);
        }
    }
}

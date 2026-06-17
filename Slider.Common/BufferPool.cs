using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.ExceptionServices;

    public class BufferPool
    {
        public readonly struct Slot
        {
            public readonly byte[] Array;
            public readonly int Offset;
            public readonly int Size;
            internal readonly long TrackingId; 

            public Slot(byte[] array, int offset, int size, long trackingId)
            {
                Array = array;
                Offset = offset;
                Size = size;
                TrackingId = trackingId;
            }

            public Memory<byte> Memory => Array.AsMemory(Offset, Size);
        }

        private readonly int _size;
        private readonly Stack<long> _freeSlots = new();
        private readonly byte[][] _buffers;

        private readonly int _indexShift;
        private readonly long _itemsPerBuffer;
        private readonly long _indexMask;

        public BufferPool(long capacity, int size)
        {
            _size = size;

            long targetChunkSize = Math.Min(capacity, 2_000_000_000L / size);

            // Handle edge case where a single item size is larger than 2GB
            if (targetChunkSize == 0)
            {
                throw new ArgumentException($"Item size ({size} bytes) is too large for a single standard array block.");
            }

            // 2. Round down to the nearest power of 2
            _indexShift = 63 - BitOperations.LeadingZeroCount((ulong)targetChunkSize);

            // Ensure minimum shift is 1 (protects against capacity = 1 scenarios)
            if (_indexShift < 1) _indexShift = 1;

            _itemsPerBuffer = 1L << _indexShift;
            _indexMask = _itemsPerBuffer - 1;

            // 3. Setup the buffer layout
            int bufferCount = (int)((capacity + _itemsPerBuffer - 1) / _itemsPerBuffer);
            _buffers = new byte[bufferCount][];

            long remainingItems = capacity;

            for (int i = 0; i < bufferCount; i++)
            {
                long currentBufferItemCount = Math.Min(remainingItems, _itemsPerBuffer);
                _buffers[i] = new byte[currentBufferItemCount * size];

                long globalOffset = (long)i << _indexShift;

                for (long j = currentBufferItemCount - 1; j >= 0; j--)
                {
                    _freeSlots.Push(globalOffset + j);
                }

                remainingItems -= currentBufferItemCount;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Slot Rent()
        {
            if (!_freeSlots.TryPop(out long globalIndex))
            {
                throw new InvalidOperationException("Buffer pool exhausted.");
            }

            // Fast bitwise operations replace costly division and modulo operators
            int bufferKey = (int)(globalIndex >> _indexShift);
            long localItemIndex = globalIndex & _indexMask;
            int byteOffset = (int)(localItemIndex * _size);

            return new Slot(_buffers[bufferKey], byteOffset, _size, globalIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return(in Slot pooledBuffer)
        {
            // Simply return the tracking ID to the stack
            _freeSlots.Push(pooledBuffer.TrackingId);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace PDBGenerator
{
    using System;
    using System.Collections.Concurrent;

    public class BufferPool
    {
        // Inner structure to track where to return the memory
        public readonly struct Slot
        {
            public int Index { get; }
            public byte Size { get; }
            public Slot(int index, byte size) { Index = index; Size = size; }
        }

        private readonly int _size;
        private readonly byte[] _buffer;
        private readonly ConcurrentStack<int> _freeSlots = new();

        public BufferPool(int capacity, int size)
        {
            _size = size;
            _buffer = new byte[capacity*size];
            _freeSlots = new ConcurrentStack<int>();
            for (int i = capacity - 1; i >= 0; i--)
            {
                _freeSlots.Push(i * size);
            }
        }

        public Slot Rent()
        {
            if (_freeSlots.TryPop(out int index))
            {
                return new Slot(index, (byte)_size);
            }
            throw new InvalidOperationException($"Pool is empty!");
        }

        public void Return(Slot slot)
        {
            _freeSlots.Push(slot.Index);
        }

        public Memory<byte> GetMemory(Slot slot)
        {
            return _buffer.AsMemory(slot.Index, slot.Size);
        }
    }
}

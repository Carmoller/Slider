using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
public sealed class MultiMap<T> where T: struct
{
    private long[] _keys;
    private int[] _valueStart;
    private int[] _valueCount;
    private int _mask;
    private T[] _values;
    private int _nextValueIndex;
    public MultiMap(int keyCapacity, int valueCapacity)
    {
        keyCapacity = RoundUpToPowerOf2(keyCapacity);
        _keys = new long[keyCapacity];
        _valueStart = new int[keyCapacity];
        _valueCount = new int[keyCapacity];
        _mask = keyCapacity - 1;
        _values = new T[valueCapacity];
        _nextValueIndex = 0;
    }
    // -------------------------------------------------
    // PUBLIC API
    // -------------------------------------------------
    public void Clear()
    {
    }

    public void AddState(long key, T value)
    {
        if (_nextValueIndex >= _values.Length)
        {
            ResizeValues(2 * _values.Length);
            ResizeKeys(2 * _values.Length);
  //          throw new InvalidOperationException("Value storage full. Call ResizeValues().");
        }
        int slot = ProbeForSlot(key);
        if (_keys[slot] == 0 && _valueCount[slot] == 0)
        {
            // New key
            _keys[slot] = key;
            _valueStart[slot] = _nextValueIndex;
            _values[_nextValueIndex++] = value;
            _valueCount[slot] = 1;
        }
        else
        {
            // Existing key
            int idx = _valueStart[slot] + _valueCount[slot];
            if (idx >= _values.Length)
            {
                ResizeValues(2 * _values.Length);
                ResizeKeys(2 * _values.Length);
//                throw new InvalidOperationException("Value storage full. Call ResizeValues().");
            }
            _values[idx] = value;
            _valueCount[slot]++;
            _nextValueIndex++;
        }
    }
    public ReadOnlySpan<T> Get(long key)
    {
        int slot = ProbeForExisting(key);
        if (slot < 0)
            return ReadOnlySpan<T>.Empty;
        return new ReadOnlySpan<T>(_values, _valueStart[slot], _valueCount[slot]);
    }

    public bool TryGetState(long key, T testValue, ref T existing)
    {
        existing = default;
        ReadOnlySpan<T> existingSpan = Get(key);
        if (existingSpan == ReadOnlySpan<T>.Empty)
            return false;
        for (int i = 0; i < existingSpan.Length; i++)
        {
            if (existingSpan[i].Equals(testValue))
            {
                existing = existingSpan[i];
                return true;
            }
        }
        return false;
    }
    // -------------------------------------------------
    // RESIZING
    // -------------------------------------------------
    public void ResizeKeys(int newCapacity)
    {
        newCapacity = RoundUpToPowerOf2(newCapacity);
        var oldKeys = _keys;
        var oldStart = _valueStart;
        var oldCount = _valueCount;
        _keys = new long[newCapacity];
        _valueStart = new int[newCapacity];
        _valueCount = new int[newCapacity];
        _mask = newCapacity - 1;
        for (int i = 0; i < oldKeys.Length; i++)
        {
            long key = oldKeys[i];
            int count = oldCount[i];
            if (key == 0 && count == 0)
                continue;
            int start = oldStart[i];
            int slot = ProbeForEmpty(key);
            _keys[slot] = key;
            _valueStart[slot] = start;
            _valueCount[slot] = count;
        }
    }
    public void ResizeValues(int newCapacity)
    {
        if (newCapacity <= _values.Length)
            throw new ArgumentException("New capacity must be larger.");
        var newValues = new T[newCapacity];
        Array.Copy(_values, newValues, _nextValueIndex);
        _values = newValues;
    }
    // -------------------------------------------------
    // PROBING (SIMD + fallback)
    // -------------------------------------------------
    private int ProbeForSlot(long key)
    {
        if (Avx2.IsSupported && Vector.IsHardwareAccelerated)
            return ProbeSimd(key);
        else
            return ProbeScalar(key);
    }
    private int ProbeForExisting(long key)
    {
        if (Avx2.IsSupported && Vector.IsHardwareAccelerated)
            return ProbeSimdExisting(key);
        else
            return ProbeScalarExisting(key);
    }
    private int ProbeForEmpty(long key)
    {
        if (Avx2.IsSupported && Vector.IsHardwareAccelerated)
            return ProbeSimdEmpty(key);
        else
            return ProbeScalarEmpty(key);
    }
    // ----------------- Scalar versions -----------------
    private int ProbeScalar(long key)
    {
        int i = (int)key & _mask;
        while (true)
        {
            long k = _keys[i];
            if (k == 0 && _valueCount[i] == 0)
                return i;          // empty slot
            if (k == key)
                return i;          // existing key
            i = (i + 1) & _mask;
        }
    }
    private int ProbeScalarExisting(long key)
    {
        int i = (int)key & _mask;
        while (true)
        {
            long k = _keys[i];
            if (k == key)
                return i;
            if (k == 0 && _valueCount[i] == 0)
                return -1;         // not found
            i = (i + 1) & _mask;
        }
    }
    private int ProbeScalarEmpty(long key)
    {
        int i = (int)key & _mask;
        while (true)
        {
            long k = _keys[i];
            if (k == 0 && _valueCount[i] == 0)
                return i;
            i = (i + 1) & _mask;
        }
    }
    // ----------------- SIMD versions (AVX2) -----------------
    private int ProbeSimd(long key)
    {
        int i = (int)key & _mask;
        var keyVec = Vector256.Create(key);
        var zeroVec = Vector256<long>.Zero;
        while (true)
        {
            ref long start = ref _keys[i];
            var block = LoadVector256(ref start);
            var cmpEq = Avx2.CompareEqual(block, keyVec);
            int maskEq = Avx2.MoveMask(cmpEq.AsByte());
            if (maskEq != 0)
            {
                int offset = BitOperations.TrailingZeroCount(maskEq) / 8;
                return (i + offset) & _mask;
            }
            var cmpZero = Avx2.CompareEqual(block, zeroVec);
            int maskZero = Avx2.MoveMask(cmpZero.AsByte());
            if (maskZero != 0)
            {
                int offset = BitOperations.TrailingZeroCount(maskZero) / 8;
                return (i + offset) & _mask;
            }
            i = (i + 4) & _mask;
        }
    }
    private int ProbeSimdExisting(long key)
    {
        int i = (int)key & _mask;
        var keyVec = Vector256.Create(key);
        var zeroVec = Vector256<long>.Zero;
        while (true)
        {
            ref long start = ref _keys[i];
            var block = LoadVector256(ref start);
            var cmpEq = Avx2.CompareEqual(block, keyVec);
            int maskEq = Avx2.MoveMask(cmpEq.AsByte());
            if (maskEq != 0)
            {
                int offset = BitOperations.TrailingZeroCount(maskEq) / 8;
                return (i + offset) & _mask;
            }
            var cmpZero = Avx2.CompareEqual(block, zeroVec);
            int maskZero = Avx2.MoveMask(cmpZero.AsByte());
            if (maskZero != 0)
            {
                return -1; // hit empty → not found
            }
            i = (i + 4) & _mask;
        }
    }
    private int ProbeSimdEmpty(long key)
    {
        int i = (int)key & _mask;
        var zeroVec = Vector256<long>.Zero;
        while (true)
        {
            ref long start = ref _keys[i];
            var block = LoadVector256(ref start);
            var cmpZero = Avx2.CompareEqual(block, zeroVec);
            int maskZero = Avx2.MoveMask(cmpZero.AsByte());
            if (maskZero != 0)
            {
                int offset = BitOperations.TrailingZeroCount(maskZero) / 8;
                return (i + offset) & _mask;
            }
            i = (i + 4) & _mask;
        }
    }
    // -------------------------------------------------
    // Helpers
    // -------------------------------------------------
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int RoundUpToPowerOf2(int x)
    {
        if (x < 2) return 2;
        x--;
        x |= x >> 1;
        x |= x >> 2;
        x |= x >> 4;
        x |= x >> 8;
        x |= x >> 16;
        x++;
        return x;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<long> LoadVector256(ref long start)
    {
        unsafe
        {
            fixed (long* p = &start)
            {
                return Avx.LoadVector256(p);
            }
        }
    }
}
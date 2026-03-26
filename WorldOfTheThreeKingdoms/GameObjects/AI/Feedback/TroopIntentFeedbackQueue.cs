using System;
using System.Collections.Generic;

namespace GameObjects.AI;

public sealed class TroopIntentFeedbackQueue
{
    private readonly TroopIntentFailedEvent[] _buffer;
    private int _head;
    private int _count;

    public int Count => _count;
    public int Capacity => _buffer.Length;

    public TroopIntentFeedbackQueue(int capacity = 4096)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _buffer = new TroopIntentFailedEvent[capacity];
        _head = 0;
        _count = 0;
    }

    public bool Publish(in TroopIntentFailedEvent eventData)
    {
        if (_count < _buffer.Length)
        {
            int insertIndex = (_head + _count) % _buffer.Length;
            _buffer[insertIndex] = eventData;
            _count++;
            return true;
        }

        _buffer[_head] = eventData;
        _head = (_head + 1) % _buffer.Length;
        return false;
    }

    public int Drain(Span<TroopIntentFailedEvent> destination)
    {
        if (destination.Length == 0 || _count == 0) return 0;

        int drainCount = Math.Min(destination.Length, _count);
        for (int i = 0; i < drainCount; i++)
        {
            destination[i] = _buffer[(_head + i) % _buffer.Length];
        }

        _head = (_head + drainCount) % _buffer.Length;
        _count -= drainCount;
        return drainCount;
    }

    public int DrainAll(List<TroopIntentFailedEvent> destination)
    {
        if (destination == null) throw new ArgumentNullException(nameof(destination));
        if (_count == 0) return 0;

        int drained = _count;
        for (int i = 0; i < drained; i++)
        {
            destination.Add(_buffer[(_head + i) % _buffer.Length]);
        }

        _head = 0;
        _count = 0;
        return drained;
    }

    public void Clear()
    {
        _head = 0;
        _count = 0;
    }
}

using System;
using System.Collections.Generic;

namespace GameObjects.AI;

public sealed class TroopIntentRegistry
{
    private readonly Dictionary<int, TroopIntent> _currentByTroop = new();

    public int Count => _currentByTroop.Count;

    public bool TryGetCurrentIntent(int troopId, out TroopIntent intent)
    {
        return _currentByTroop.TryGetValue(troopId, out intent);
    }

    public TroopIntent GetCurrentIntent(int troopId)
    {
        if (_currentByTroop.TryGetValue(troopId, out TroopIntent intent))
        {
            return intent;
        }

        throw new KeyNotFoundException($"No current TroopIntent for troopId={troopId}.");
    }

    public void SetCurrentIntent(TroopIntent intent)
    {
        if (!intent.Id.IsValid)
        {
            throw new ArgumentException("Intent id must be valid.", nameof(intent));
        }

        _currentByTroop[intent.Id.TroopId] = intent;
    }

    public void Remove(int troopId)
    {
        _currentByTroop.Remove(troopId);
    }

    public int ClearFaction(int factionId)
    {
        if (factionId < 0 || _currentByTroop.Count == 0) return 0;

        List<int> removeKeys = [];
        foreach (var entry in _currentByTroop)
        {
            if (entry.Value.SourceFactionId == factionId)
            {
                removeKeys.Add(entry.Key);
            }
        }

        for (int i = 0; i < removeKeys.Count; i++)
        {
            _currentByTroop.Remove(removeKeys[i]);
        }

        return removeKeys.Count;
    }

    public void Clear()
    {
        _currentByTroop.Clear();
    }
}

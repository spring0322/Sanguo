using System;
using System.Collections.Generic;

namespace WorldOfTheThreeKingdoms.GameManager
{
    public static class DeterministicRNG
    {
        private static Dictionary<string, float> _cache = new Dictionary<string, float>();
        private static int _currentTurn = 0;

        public static void SetTurn(int turn)
        {
            if (turn != _currentTurn)
            {
                _cache.Clear();
                _currentTurn = turn;
            }
        }

        public static int GetCurrentTurn()
        {
            return _currentTurn;
        }

        public static float GetValue(int turn, int factionId, string key)
        {
            string compositeKey = $"{turn}_{factionId}_{key}";
            if (_cache.ContainsKey(compositeKey))
            {
                return _cache[compositeKey];
            }

            int seed = compositeKey.GetHashCode();
            Random rand = new Random(seed);
            float value = (float)rand.NextDouble();
            _cache[compositeKey] = value;
            return value;
        }
    }
}

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace WorldOfTheThreeKingdoms.GameManager
{
    // AI记忆地图 - 简化版本
    public class AIMemoryMap
    {
        private Dictionary<string, GhostUnit> memoryMap;

        public AIMemoryMap()
        {
            memoryMap = new Dictionary<string, GhostUnit>();
        }

        /// <summary>
        /// 获取内部字典，用于兼容原有代码中的 MemoryMap.Values.Values 访问模式
        /// </summary>
        public Dictionary<string, GhostUnit> Values => memoryMap;

        public void AddOrUpdateGhost(string key, GhostUnit ghost)
        {
            memoryMap[key] = ghost;
        }

        public GhostUnit GetGhost(string key)
        {
            return memoryMap.ContainsKey(key) ? memoryMap[key] : null;
        }

        public void RemoveGhost(string key)
        {
            memoryMap.Remove(key);
        }

        public void Clear()
        {
            memoryMap.Clear();
        }

        public Dictionary<string, GhostUnit> GetAllGhosts()
        {
            return new Dictionary<string, GhostUnit>(memoryMap);
        }

        /// <summary>
        /// 清理过期的记忆
        /// </summary>
        public void CleanExpiredMemories(int currentDay = 0)
        {
            var keysToRemove = new List<string>();
            
            foreach (var kvp in memoryMap)
            {
                if (kvp.Value.IsExpired())
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                memoryMap.Remove(key);
            }
        }
    }
}
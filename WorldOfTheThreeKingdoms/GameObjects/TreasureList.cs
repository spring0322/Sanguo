using System;
using System.Runtime.Serialization;

namespace GameObjects
{
    // 🔥 2026-02-12 根本修复：移除 [DataContract]，添加 [JsonConverter]
    [System.Text.Json.Serialization.JsonConverter(typeof(WorldOfTheThreeKingdoms.Serialization.SystemTextJson.GameObjectListConverter))]
    public class TreasureList : GameObjectList
    {
        public void AddTreasure(Treasure treasure)
        {
            base.GameObjects.Add(treasure);
        }
    }
}


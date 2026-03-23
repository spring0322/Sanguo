using System;
using System.Runtime.Serialization;

namespace GameObjects
{
    // 🔥 2026-02-12 根本修复：移除 [DataContract]，添加 [JsonConverter]
    [System.Text.Json.Serialization.JsonConverter(typeof(WorldOfTheThreeKingdoms.Serialization.SystemTextJson.GameObjectListConverter))]
    public class LegionList : GameObjectList
    {
        public void AddLegionWithEvent(Legion legion)
        {
            base.Add(legion);
        }

        public void RemoveLegion(Legion legion)
        {
            base.Remove(legion);
        }
    }
}


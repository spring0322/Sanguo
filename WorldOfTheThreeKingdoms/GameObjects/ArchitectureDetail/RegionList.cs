using GameObjects;
using System.Runtime.Serialization;

namespace GameObjects.ArchitectureDetail
{
    // 🔥 2026-02-12 根本修复：移除 [DataContract]，添加 [JsonConverter]
    [System.Text.Json.Serialization.JsonConverter(typeof(WorldOfTheThreeKingdoms.Serialization.SystemTextJson.GameObjectListConverter))]
    public class RegionList : GameObjectList
    {
    }
}


using GameObjects;
using System.Runtime.Serialization;

namespace GameObjects.TroopDetail
{
    // 🔥 2026-02-12 根本修复：移除 [DataContract] 特性，添加 [JsonConverter]
    // 问题：[DataContract] 与 System.Text.Json 源生成器冲突
    // 解决：使用 GameObjectListConverter 处理序列化
    [System.Text.Json.Serialization.JsonConverter(typeof(WorldOfTheThreeKingdoms.Serialization.SystemTextJson.GameObjectListConverter))]
    public class MilitaryKindList : GameObjectList
    {
    }
}


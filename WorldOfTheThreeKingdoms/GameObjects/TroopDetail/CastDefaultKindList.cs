using GameObjects;
using System;
using System.Runtime.Serialization;

namespace GameObjects.TroopDetail
{
    // 🔥 2026-02-12 根本修复:移除 [DataContract] 特性，添加 [JsonConverter]
    // 问题：[DataContract] 与 System.Text.Json 源生成器冲突
    // 解决：使用 GameObjectListConverter 处理序列化
    [System.Text.Json.Serialization.JsonConverter(typeof(WorldOfTheThreeKingdoms.Serialization.SystemTextJson.GameObjectListConverter))]
    public class CastDefaultKindList : GameObjectList
    {
        public CastDefaultKindList GetSelectedList(TroopCastDefaultKind kind)
        {
            CastDefaultKindList list = new CastDefaultKindList();
            
            // 🔥 AOT 修复：显式类型转换，避免隐式转换失败
            // 日期：2026-03-21
            // 原因：AOT 环境下 foreach (CastDefaultKind in List<GameObject>) 隐式转换失败
            // 解决：使用 for 循环 + as 类型转换 + Fail Fast
            for (int i = 0; i < base.GameObjects.Count; i++)
            {
                CastDefaultKind kind2 = base.GameObjects[i] as CastDefaultKind;
                if (kind2 == null)
                {
                    throw new InvalidOperationException($"CastDefaultKindList 中存在非 CastDefaultKind 类型的对象：{base.GameObjects[i]?.GetType().Name ?? "null"}");
                }
                
                if (kind2.ID ==(int) kind)
                {
                    list.Add(kind2);
                }
            }
            return list;
        }
    }
}


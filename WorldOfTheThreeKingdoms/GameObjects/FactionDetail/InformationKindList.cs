using GameObjects;
using System;
using System.Runtime.Serialization;

namespace GameObjects.FactionDetail
{
    // 🔥 2026-02-12 根本修复：移除 [DataContract] 特性，添加 [JsonConverter]
    // 问题：[DataContract] 与 System.Text.Json 源生成器冲突
    // 解决：使用 GameObjectListConverter 处理序列化
    [System.Text.Json.Serialization.JsonConverter(typeof(WorldOfTheThreeKingdoms.Serialization.SystemTextJson.GameObjectListConverter))]
    public class InformationKindList : GameObjectList
    {
        public InformationKindList GetAvailList(Architecture a)
        {
            InformationKindList list = new InformationKindList();
            // System.Diagnostics.Debug.WriteLine($\"[InformationKindList.GetAvailList] 开始处理 {a.Name} - 总数: {base.GameObjects.Count}\");
            
            foreach (GameObject obj in base.GameObjects)
            {
                InformationKind kind = (obj is InformationKind ? (InformationKind)obj : null);
                if (kind == null)
                {
                    // System.Diagnostics.Debug.WriteLine($\"[InformationKindList.GetAvailList] ❌ 类型转换失败: 对象类型为 {obj?.GetType()?.Name ?? \"null\"}, ID={obj?.ID ?? -1}\");
                    continue;
                }
                
                if (kind.Avail(a))
                {
                    list.Add(kind);
                    // System.Diagnostics.Debug.WriteLine($\"[InformationKindList.GetAvailList] ✅ 添加可用情报类型: {kind.ID} ({kind.Name})\");
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine($\"[InformationKindList.GetAvailList] ⚠️ 情报类型不可用: {kind.ID} ({kind.Name}) - 资金不足或其他条件\");
                }
            }
            
            // System.Diagnostics.Debug.WriteLine($\"[InformationKindList.GetAvailList] 完成处理 {a.Name} - 可用数: {list.Count}\");
            return list;
        }

        public bool HasAvailItem(Architecture a)
        {
            // System.Diagnostics.Debug.WriteLine($\"[HasAvailItem] 检查 {a.Name} - 情报类型总数: {base.GameObjects.Count}\");
            
            int availCount = 0;
            foreach (GameObject obj in base.GameObjects)
            {
                InformationKind kind = (obj is InformationKind ? (InformationKind)obj : null);
                if (kind == null)
                {
                    // System.Diagnostics.Debug.WriteLine($\"[HasAvailItem] ❌ 类型转换失败: 对象类型为 {obj?.GetType()?.Name ?? \"null\"}, ID={obj?.ID ?? -1}\");
                    continue;
                }
                
                bool avail = kind.Avail(a);
                if (avail)
                {
                    availCount++;
                    // System.Diagnostics.Debug.WriteLine($\"[HasAvailItem] {a.Name} - 情报类型 {kind.ID} ({kind.Name}) 可用: Cost={kind.CostFund}, Fund={a.Fund}\");
                }
            }
            
            // System.Diagnostics.Debug.WriteLine($\"[HasAvailItem] {a.Name} - 可用情报类型数: {availCount}\");
            return availCount > 0;
        }
    }
}



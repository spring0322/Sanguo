using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GameObjects.ArchitectureDetail
{
    /// <summary>
    /// 建筑类型表
    /// </summary>
    [DataContract]
    public class ArchitectureKindTable
    {
        // 🔥 关键修复：JSON 中的字典键是字符串（"1", "2"），需要转换为 int
        // 日期：2026-03-20
        [DataMember]
        [System.Text.Json.Serialization.JsonConverter(typeof(WorldOfTheThreeKingdoms.Serialization.SystemTextJson.LegacyDictionaryConverter<int, ArchitectureKind>))]
        public Dictionary<int, ArchitectureKind> ArchitectureKinds = [];

        /// <summary>
        /// 建筑类型字典添加类型
        /// </summary>
        /// <param name="architectureKind"></param>
        /// <returns></returns>
        public bool AddArchitectureKind(ArchitectureKind architectureKind)
        {
            if (ArchitectureKinds.ContainsKey(architectureKind.ID))
                return false;

            ArchitectureKinds.Add(architectureKind.ID, architectureKind);

            return true;
        }

        /// <summary>
        /// 清空建筑类型字典
        /// </summary>
        public void Clear()
        {
            ArchitectureKinds.Clear();
        }

        /// <summary>
        /// 获取建筑类型
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ArchitectureKind GetArchitectureKind(int id)
        {
            // 🔥 诊断：检查字典状态
            if (ArchitectureKinds == null)
            {
                return null;
            }
            
            if (ArchitectureKinds.Count == 0)
            {
                return null;
            }
            
            ArchitectureKinds.TryGetValue(id, out var architectureKind);
            
            if (architectureKind == null)
            {
            }

            return architectureKind;
        }

        /// <summary>
        /// 获取建筑类型列表
        /// </summary>
        /// <returns></returns>
        public GameObjectList GetArchitectureKindList()
        {
            GameObjectList list = new GameObjectList();

            foreach (ArchitectureKind kind in ArchitectureKinds.Values)
            {
                list.Add(kind);
            }

            return list;
        }
    }
}
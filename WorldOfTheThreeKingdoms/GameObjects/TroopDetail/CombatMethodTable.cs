using GameObjects;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace GameObjects.TroopDetail
{
    [DataContract]
    public class CombatMethodTable
    {
        // 🔥 关键修复：CommonData.json 使用字符串键，需要转换为 int 键
        // 日期：2026-03-20
        [DataMember]
        [System.Text.Json.Serialization.JsonConverter(typeof(WorldOfTheThreeKingdoms.Serialization.SystemTextJson.LegacyDictionaryConverter<int, CombatMethod>))]
        public Dictionary<int, CombatMethod> CombatMethods = [];

        public bool AddCombatMethod(CombatMethod combatMethod)
        {
            if (this.CombatMethods.ContainsKey(combatMethod.ID))
            {
                return false;
            }
            this.CombatMethods.Add(combatMethod.ID, combatMethod);
            return true;
        }

        public bool RemoveCombatMethod(CombatMethod combatMethod)
        {
            if (!this.CombatMethods.ContainsKey(combatMethod.ID))
            {
                return false;
            }
            this.CombatMethods.Remove(combatMethod.ID);
            return true;
        }

        public void Clear()
        {
            this.CombatMethods.Clear();
        }

        public CombatMethod GetCombatMethod(int combatMethodID)
        {
            CombatMethod method = null;
            this.CombatMethods.TryGetValue(combatMethodID, out method);
            return method;
        }

        public GameObjectList GetCombatMethodList()
        {
            GameObjectList list = new GameObjectList();
            foreach (CombatMethod method in this.CombatMethods.Values)
            {
                list.Add(method);
            }
            return list;
        }

        public int Count
        {
            get
            {
                return this.CombatMethods.Count;
            }
        }

        public string SaveToString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (var obj2 in this.CombatMethods)
            {
                builder.Append(obj2.Key + " ");
            }
            return builder.ToString();
        }

    }
}


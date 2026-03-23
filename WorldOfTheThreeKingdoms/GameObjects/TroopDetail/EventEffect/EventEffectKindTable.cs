using GameObjects;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace GameObjects.TroopDetail.EventEffect
{
    // 🔥 2026-02-12 AOT 根本修复：移除 DataContract 特性
    // 问题：DataContract/DataMember 与 System.Text.Json 源生成器冲突，导致源生成器静默失败
    // 解决：使用 System.Text.Json 的特性，让 AOT 源生成器正常工作
    public class EventEffectKindTable
    {
        // 🔥 关键修复：CommonData.json 使用字符串键，需要转换为 int 键
        // 日期：2026-03-20
        [JsonInclude]
        [System.Text.Json.Serialization.JsonConverter(typeof(WorldOfTheThreeKingdoms.Serialization.SystemTextJson.LegacyDictionaryConverter<int, EventEffectKind>))]
        public Dictionary<int, EventEffectKind> EventEffectKinds = [];

        public bool AddEventEffectKind(EventEffectKind e)
        {
            if (this.EventEffectKinds.ContainsKey(e.ID))
            {
                return false;
            }
            this.EventEffectKinds.Add(e.ID, e);
            return true;
        }

        public void Clear()
        {
            this.EventEffectKinds.Clear();
        }

        public EventEffectKind GetEventEffectKind(int id)
        {
            EventEffectKind kind = null;
            this.EventEffectKinds.TryGetValue(id, out kind);
            return kind;
        }

        public GameObjectList GetEventEffectKindList()
        {
            GameObjectList list = new GameObjectList();
            foreach (EventEffectKind kind in this.EventEffectKinds.Values)
            {
                list.Add(kind);
            }
            return list;
        }

        public bool HasEventEffectKind(int id)
        {
            return this.EventEffectKinds.ContainsKey(id);
        }

        public void LoadFromString(EventEffectKindTable allEventEffectKinds, string influenceIDs)
        {
            // 🔥 防止 STJ 反序列化后的 null 导致崩溃
            if (string.IsNullOrEmpty(influenceIDs)) return;
            
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = influenceIDs.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            EventEffectKind kind = null;
            for (int i = 0; i < strArray.Length; i++)
            {
                if (allEventEffectKinds.EventEffectKinds.TryGetValue(int.Parse(strArray[i]), out kind))
                {
                    this.AddEventEffectKind(kind);
                }
            }
        }

        public string SaveToString()
        {
            string str = "";
            foreach (EventEffectKind kind in this.EventEffectKinds.Values)
            {
                str = str + kind.ID.ToString() + " ";
            }
            return str;
        }

        public int Count
        {
            get
            {
                return this.EventEffectKinds.Count;
            }
        }
    }
}


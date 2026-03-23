using GameObjects;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace GameObjects.ArchitectureDetail.EventEffect
{
    // 🔥 2026-02-12 AOT 根本修复：移除 DataContract 特性
    // 问题：DataContract/DataMember 与 System.Text.Json 源生成器冲突，导致源生成器静默失败
    // 解决：使用 System.Text.Json 的特性，让 AOT 源生成器正常工作
    public class EventEffectTable
    {
        // 🔥 关键修复：CommonData.json 使用字符串键，需要转换为 int 键
        // 日期：2026-03-20
        [JsonInclude]
        [System.Text.Json.Serialization.JsonConverter(typeof(WorldOfTheThreeKingdoms.Serialization.SystemTextJson.LegacyDictionaryConverter<int, EventEffect>))]
        public Dictionary<int, EventEffect> EventEffects = [];

        public bool AddEventEffect(EventEffect e)
        {
            if (this.EventEffects.ContainsKey(e.ID))
            {
                return false;
            }
            this.EventEffects.Add(e.ID, e);
            return true;
        }

        public void Clear()
        {
            this.EventEffects.Clear();
        }

        public EventEffect GetEventEffect(int id)
        {
            EventEffect effect = null;
            this.EventEffects.TryGetValue(id, out effect);
            return effect;
        }

        public GameObjectList GetEventEffectList()
        {
            GameObjectList list = new GameObjectList();
            foreach (EventEffect effect in this.EventEffects.Values)
            {
                list.Add(effect);
            }
            return list;
        }

        public bool HasEventEffect(int id)
        {
            return this.EventEffects.ContainsKey(id);
        }

        public void LoadFromString(EventEffectTable allEventEffects, string influenceIDs)
        {
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = influenceIDs.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            EventEffect effect = null;
            for (int i = 0; i < strArray.Length; i++)
            {
                if (allEventEffects.EventEffects.TryGetValue(int.Parse(strArray[i]), out effect))
                {
                    this.AddEventEffect(effect);
                }
            }
        }

        public string SaveToString()
        {
            string str = "";
            foreach (EventEffect effect in this.EventEffects.Values)
            {
                str = str + effect.ID.ToString() + " ";
            }
            return str;
        }

        public int Count
        {
            get
            {
                return this.EventEffects.Count;
            }
        }
    }
}


using GameObjects;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GameObjects.PersonDetail
{
    [DataContract]
    public class StuntTable
    {
        // 🔥 关键修复：CommonData.json 使用字符串键，需要转换为 int 键
        // 日期：2026-03-20
        [DataMember]
        [System.Text.Json.Serialization.JsonConverter(typeof(WorldOfTheThreeKingdoms.Serialization.SystemTextJson.LegacyDictionaryConverter<int, Stunt>))]
        public Dictionary<int, Stunt> Stunts = [];

        public bool AddStunt(Stunt stunt)
        {
            if (this.Stunts.ContainsKey(stunt.ID))
            {
                return false;
            }
            this.Stunts.Add(stunt.ID, stunt);
            return true;
        }

        public bool RemoveStunt(Stunt stunt)
        {
            if (!this.Stunts.ContainsKey(stunt.ID))
            {
                return false;
            }
            this.Stunts.Remove(stunt.ID);
            return true;
        }

        public void Clear()
        {
            this.Stunts.Clear();
        }

        public Stunt GetStunt(int stuntID)
        {
            Stunt stunt = null;
            this.Stunts.TryGetValue(stuntID, out stunt);
            return stunt;
        }

        public GameObjectList GetStuntList()
        {
            GameObjectList list = new GameObjectList();
            foreach (Stunt stunt in this.Stunts.Values)
            {
                list.Add(stunt);
            }
            return list;
        }

        public void LoadFromString(StuntTable allStunts, string stuntIDs)
        {
            // 🔥 防止 STJ 反序列化后的 null 导致崩溃
            if (string.IsNullOrEmpty(stuntIDs)) return;
            
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = stuntIDs.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            Stunt stunt = null;
            for (int i = 0; i < strArray.Length; i++)
            {
                if (allStunts.Stunts.TryGetValue(int.Parse(strArray[i]), out stunt))
                {
                    this.AddStunt(stunt);
                }
            }
        }

        public string SaveToString()
        {
            string str = "";
            foreach (Stunt stunt in this.Stunts.Values)
            {
                str = str + stunt.ID.ToString() + " ";
            }
            return str;
        }

        public int Count
        {
            get
            {
                return this.Stunts.Count;
            }
        }
    }
}


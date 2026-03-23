using GameObjects;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace GameObjects.PersonDetail
{
    [DataContract]
    public class TitleTable : System.Text.Json.Serialization.IJsonOnDeserialized
    {
        // 🔥 关键修复：CommonData.json 使用字符串键，需要转换为 int 键
        // 日期：2026-03-20
        [DataMember]
        [System.Text.Json.Serialization.JsonConverter(typeof(WorldOfTheThreeKingdoms.Serialization.SystemTextJson.LegacyDictionaryConverter<int, Title>))]
        public Dictionary<int, Title> Titles = [];

        // 🔥 STJ 反序列化后的安全保障
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context) => Titles ??= [];

        /// <summary>
        /// IJsonOnDeserialized 接口实现 —— STJ AOT 模式下的反序列化回调。
        /// </summary>
        void System.Text.Json.Serialization.IJsonOnDeserialized.OnDeserialized() => Titles ??= [];

        public bool AddTitle(Title title)
        {
            if (this.Titles.ContainsKey(title.ID))
            {
                return false;
            }
            this.Titles.Add(title.ID, title);
            return true;
        }

        public void Clear()
        {
            this.Titles.Clear();
        }

        public Title GetTitle(int titleID)
        {
            Title title = null;
            this.Titles.TryGetValue(titleID, out title);
            return title;
        }

        public GameObjectList GetTitleList()
        {
            GameObjectList list = new GameObjectList();
            foreach (Title title in this.Titles.Values)
            {
                list.Add(title);
            }
            return list;
        }

        public List<string> LoadFromString(TitleTable allTitles, string titleIDs)
        {
            List<string> errorMsg = new List<string>();

            // 🔥 防止 STJ 反序列化后的 null 导致崩溃
            if (string.IsNullOrEmpty(titleIDs)) return errorMsg;

            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = titleIDs.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            Title title = null;
            try
            {
                for (int i = 0; i < strArray.Length; i++)
                {
                    if (allTitles.Titles.TryGetValue(int.Parse(strArray[i]), out title))
                    {
                        this.AddTitle(title);
                    }
                    else
                    {
                        errorMsg.Add("称号ID" + int.Parse(strArray[i]) + "不存在");
                    }
                }
            }
            catch
            {
                errorMsg.Add("兵种一栏应为半型空格分隔的影响ID");
            }

            return errorMsg;
        }

        public string SaveToString()
        {
            string str = "";
            foreach (Title title in this.Titles.Values)
            {
                str = str + title.ID.ToString() + " ";
            }
            return str;
        }

        public int Count
        {
            get
            {
                return this.Titles.Count;
            }
        }
    }
}


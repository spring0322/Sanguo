using GameManager;
using GameObjects;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GameObjects.TroopDetail
{
    [DataContract]
    public class MilitaryKindTable
    {
        private Dictionary<int, MilitaryKind> _militaryKinds;
        
        // 🔥 关键修复：CommonData.json 使用字符串键，需要转换为 int 键
        // 日期：2026-03-20
        [DataMember]
        [System.Text.Json.Serialization.JsonConverter(typeof(WorldOfTheThreeKingdoms.Serialization.SystemTextJson.LegacyDictionaryConverter<int, MilitaryKind>))]
        public Dictionary<int, MilitaryKind> MilitaryKinds 
        { 
            get => _militaryKinds ??= []; 
            set => _militaryKinds = value ?? []; 
        }

        public bool AddMilitaryKind(MilitaryKind militaryKind)
        {
            if (this.MilitaryKinds.ContainsKey(militaryKind.ID))
            {
                return false;
            }
            this.MilitaryKinds.Add(militaryKind.ID, militaryKind);
            return true;
        }

        public bool AddMilitaryKind(int kind)
        {
            if (this.MilitaryKinds.ContainsKey(kind))
            {
                return false;
            }
            MilitaryKind militaryKind = Session.Current.Scenario.GameCommonData.AllMilitaryKinds.GetMilitaryKind(kind);
            if (militaryKind != null)
            {
                this.MilitaryKinds.Add(kind, militaryKind);
            }
            return true;
        }

        public bool RemoveMilitaryKind(int kind)
        {
            if (!this.MilitaryKinds.ContainsKey(kind))
            {
                return false;
            }
            MilitaryKind militaryKind = Session.Current.Scenario.GameCommonData.AllMilitaryKinds.GetMilitaryKind(kind);
            if (militaryKind != null)
            {
                this.MilitaryKinds.Remove(militaryKind.ID);
            }
            return true;
        }

        public void Clear()
        {
            this.MilitaryKinds.Clear();
        }

        public MilitaryKind GetMilitaryKind(int militaryKindID)
        {
            MilitaryKind kind = null;
            this.MilitaryKinds.TryGetValue(militaryKindID, out kind);
            return kind;
        }

        public GameObjectList GetMilitaryKindList()
        {
            GameObjectList list = new GameObjectList();
            foreach (MilitaryKind kind in this.MilitaryKinds.Values)
            {
                list.Add(kind);
            }
            return list;
        }

        public void AddBasicMilitaryKinds()
        {
            // 🔥 技术性修复：避免ArgumentNullException和IndexOutOfRangeException
            var militaryKindList = Session.Current.Scenario?.GameCommonData?.AllMilitaryKinds?.GetMilitaryKindList();
            if (militaryKindList != null)
            {
                var mk0 = militaryKindList.GetGameObject(0) is MilitaryKind ? (MilitaryKind)militaryKindList.GetGameObject(0) : null;
                if (mk0 != null) this.AddMilitaryKind(mk0);
                
                var mk1 = militaryKindList.GetGameObject(1) is MilitaryKind ? (MilitaryKind)militaryKindList.GetGameObject(1) : null;
                if (mk1 != null) this.AddMilitaryKind(mk1);
                
                var mk2 = militaryKindList.GetGameObject(2) is MilitaryKind ? (MilitaryKind)militaryKindList.GetGameObject(2) : null;
                if (mk2 != null) this.AddMilitaryKind(mk2);
                
                var mk30 = militaryKindList.GetGameObject(30) is MilitaryKind ? (MilitaryKind)militaryKindList.GetGameObject(30) : null;
                if (mk30 != null) this.AddMilitaryKind(mk30);
            }
        }

        public List<string> LoadFromString(MilitaryKindTable allMilitaryKinds, string militaryKindIDs)
        {
            List<string> errorMsg = [];

            // 🔥 防止 STJ 反序列化后的 null 导致崩溃
            if (string.IsNullOrEmpty(militaryKindIDs)) return errorMsg;

            char[] separator = [' ', '\n', '\r', '\t'];
            string[] strArray = militaryKindIDs.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            MilitaryKind kind = null;
            try
            {
                for (int i = 0; i < strArray.Length; i++)
                {
                    int militaryKindID = int.Parse(strArray[i]);
                    
                    if (allMilitaryKinds.MilitaryKinds.TryGetValue(militaryKindID, out kind))
                    {
                        this.AddMilitaryKind(kind);
                    }
                    else
                    {
                        string error = $"兵种ID {militaryKindID} 不存在";
                        errorMsg.Add(error);
                    }
                }
            }
            catch (Exception ex)
            {
                string error = "兵种一栏应为半型空格分隔的兵种ID";
                errorMsg.Add(error);
            }

            return errorMsg;
        }

        public string SaveToString()
        {
            string str = "";
            foreach (MilitaryKind kind in this.MilitaryKinds.Values)
            {
                str = str + kind.ID.ToString() + " ";
            }
            return str;
        }
    }
}



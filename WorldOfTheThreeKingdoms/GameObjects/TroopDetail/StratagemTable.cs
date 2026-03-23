using GameObjects;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GameObjects.TroopDetail
{
    [DataContract]
    public class StratagemTable
    {
        // 🔥 关键修复：CommonData.json 使用字符串键，需要转换为 int 键
        // 日期：2026-03-20
        [DataMember]
        [System.Text.Json.Serialization.JsonConverter(typeof(WorldOfTheThreeKingdoms.Serialization.SystemTextJson.LegacyDictionaryConverter<int, Stratagem>))]
        public Dictionary<int, Stratagem> Stratagems = [];

        public bool AddStratagem(Stratagem Stratagem)
        {
            if (this.Stratagems.ContainsKey(Stratagem.ID))
            {
                return false;
            }
            this.Stratagems.Add(Stratagem.ID, Stratagem);
            return true;
        }

        public void Clear()
        {
            this.Stratagems.Clear();
        }

        public Stratagem GetStratagem(int StratagemID)
        {
            Stratagem stratagem = null;
            this.Stratagems.TryGetValue(StratagemID, out stratagem);
            return stratagem;
        }

        public GameObjectList GetStratagemList()
        {
            GameObjectList list = new GameObjectList();
            foreach (Stratagem stratagem in this.Stratagems.Values)
            {
                list.Add(stratagem);
            }
            return list;
        }
    }
}


using WorldOfTheThreeKingdoms.GameGlobal;  // 🔥 用于 GenerateUIAccessor 特性
using GameObjects;
using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace GameObjects.PersonDetail
{
    [DataContract]
    [GenerateUIAccessor]  // 🔥 2026-03-03 修复：添加源生成器特性，支持 UI 列表显示
    public class IdealTendencyKind : GameObject
    {
        private int offset;

        public override string ToString()
        {
            return (base.Name + " " + this.Offset.ToString());
        }
        
        [DataMember]
        [JsonPropertyName("Offset")]
        [JsonInclude]
        public int Offset
        {
            get
            {
                return this.offset;
            }
            set
            {
                this.offset = value;
            }
        }
    }
}


using WorldOfTheThreeKingdoms.GameGlobal;  // 🔥 用于 GenerateUIAccessor 特性
using GameManager;
using GameObjects;
using System;


namespace GameObjects.FactionDetail
{
    // 🔥 2026-03-03 修复：添加源生成器特性，让 UI 能访问 FactionName 属性
    [GenerateUIAccessor]
    public class DiplomaticRelationDisplay : GameObject
    {
        private string factionName;
        private DiplomaticRelation LinkedDiplomaticRelation;

        public DiplomaticRelationDisplay(DiplomaticRelation linked, string displayName)
        {
            this.LinkedDiplomaticRelation = linked;
            this.factionName = displayName;
            // 🔥 2026-03-03 修复：同步设置 GameObject.Name 以支持通用显示
            base.name = displayName;
        }

        // 🔥 C# 12: 使用表达式体属性
        public string FactionName => this.factionName;

        public Faction LinkedFaction1 => this.LinkedDiplomaticRelation.RelationFaction1;

        public Faction LinkedFaction2 => this.LinkedDiplomaticRelation.RelationFaction2;

        public int Relation
        {
            get => this.LinkedDiplomaticRelation.Relation;
            set => this.LinkedDiplomaticRelation.Relation = value;
        }

        public int Truce
        {
            get => this.LinkedDiplomaticRelation.Truce;
            set => this.LinkedDiplomaticRelation.Truce = value;
        }

        public int TruceDays => Truce * Session.Current.Scenario.Parameters.DayInTurn;

        public override bool Equals(object obj)
        {
            if (!(obj is DiplomaticRelationDisplay)) return false;
            return this.LinkedDiplomaticRelation.Equals(((DiplomaticRelationDisplay)obj).LinkedDiplomaticRelation);
        }

        public override int GetHashCode()
        {
            return this.LinkedDiplomaticRelation.GetHashCode();
        }
    }
}


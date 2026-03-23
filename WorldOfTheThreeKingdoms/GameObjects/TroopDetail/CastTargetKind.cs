using WorldOfTheThreeKingdoms.GameGlobal;  // 🔥 用于 GenerateUIAccessor 特性
using GameObjects;
using System;
using System.Runtime.Serialization;

namespace GameObjects.TroopDetail
{
    [DataContract]
    [GenerateUIAccessor]  // 🔥 2026-03-03 修复：添加源生成器特性，支持 UI 列表显示
    public class CastTargetKind : GameObject
    {
        public void Apply(Troop troop)
        {
            troop.CastTargetKind = (TroopCastTargetKind) base.ID;
        }
    }
}


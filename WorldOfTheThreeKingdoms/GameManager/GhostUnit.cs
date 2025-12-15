using System;
using Microsoft.Xna.Framework;
using GameObjects;

namespace WorldOfTheThreeKingdoms.GameManager
{
    // 幽灵单位 - 用于AI记忆系统
    public class GhostUnit
    {
        public int TroopId { get; set; }
        public int FactionId { get; set; }
        public Point Position { get; set; }
        public DateTime LastSeenTime { get; set; }
        public float Strength { get; set; }
        public int PersonCount { get; set; }

        /// <summary>
        /// 最后已知位置 - 兼容原有代码
        /// </summary>
        public Point LastPosition 
        { 
            get => Position; 
            set => Position = value; 
        }

        /// <summary>
        /// 真实单位ID - 兼容原有代码
        /// </summary>
        public int RealUnitID 
        { 
            get => TroopId; 
            set => TroopId = value; 
        }

        public GhostUnit(int troopId, int factionId, Point position, float strength, int personCount)
        {
            TroopId = troopId;
            FactionId = factionId;
            Position = position;
            Strength = strength;
            PersonCount = personCount;
            LastSeenTime = DateTime.Now;
        }

        // 从Troop对象创建GhostUnit的构造函数
        public GhostUnit(Troop troop, int currentDay)
        {
            TroopId = troop.ID;
            FactionId = troop.BelongedFaction.ID;
            Position = troop.Position;
            Strength = troop.Quantity;
            PersonCount = troop.PersonCount;
            LastSeenTime = DateTime.Now;
        }

        // 检查是否过期 (10天)
        public bool IsExpired()
        {
            return (DateTime.Now - LastSeenTime).TotalDays > 10;
        }

        // 获取当前强度 (线性衰减)
        public float GetCurrentStrength()
        {
            double daysPassed = (DateTime.Now - LastSeenTime).TotalDays;
            if (daysPassed >= 10) return 0f;
            
            return Strength * (1f - (float)(daysPassed / 10.0));
        }

        /// <summary>
        /// 更新幽灵单位信息 - 兼容原有代码
        /// </summary>
        public void Update(Point newPosition, float newStrength)
        {
            Position = newPosition;
            LastPosition = newPosition;
            Strength = newStrength;
            LastSeenTime = DateTime.Now;
        }
    }
}
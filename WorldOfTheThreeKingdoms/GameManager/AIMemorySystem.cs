using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameObjects;
using WorldOfTheThreeKingdoms.GameManager;

namespace WorldOfTheThreeKingdoms.GameManager
{
    
    /// <summary>
    /// 影响力配置
    /// </summary>
    public class InfluenceConfig
    {
        public float Range { get; set; } = 3.0f;      // 影响范围
        public float Strength { get; set; } = 1.0f;   // 基础强度
        public float Decay { get; set; } = 0.8f;      // 衰减系数
    }
}
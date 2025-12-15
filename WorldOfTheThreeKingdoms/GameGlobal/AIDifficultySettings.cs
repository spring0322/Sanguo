using System;
using System.Collections.Generic;
using GameObjects;

namespace GameGlobal
{
    public class AIDifficultySettings
    {
        public float ErrorRate { get; set; }        // 犯错概率 (0.0 - 1.0)
        public bool EnableSiegeCoordination { get; set; } // 是否启用围城协同
        public bool EnableUnitDrafting { get; set; }      // 是否启用选秀组队
        public int PredictionDepth { get; set; }    // 战术预测步数
        public bool UseCheatingVision { get; set; } // 是否开天眼
        
        public static AIDifficultySettings Get(AIDifficulty level)
        {
            switch (level)
            {
                case AIDifficulty.Easy:
                    return new AIDifficultySettings { 
                        ErrorRate = 0.4f, 
                        EnableSiegeCoordination = false, 
                        EnableUnitDrafting = false, // 只有主将带队，不配副将
                        PredictionDepth = 0 
                    };
                case AIDifficulty.Normal:
                    return new AIDifficultySettings { 
                        ErrorRate = 0.1f, 
                        EnableSiegeCoordination = true, 
                        EnableUnitDrafting = true, 
                        PredictionDepth = 1 
                    };
                case AIDifficulty.Hard:
                    return new AIDifficultySettings { 
                        ErrorRate = 0.0f, 
                        EnableSiegeCoordination = true, 
                        EnableUnitDrafting = true, 
                        PredictionDepth = 2 // 多看一步
                    };
                case AIDifficulty.Nightmare:
                    return new AIDifficultySettings { 
                        ErrorRate = 0.0f, 
                        EnableSiegeCoordination = true, 
                        EnableUnitDrafting = true, 
                        PredictionDepth = 3,
                        UseCheatingVision = true
                    };
                default: // Default to Normal
                    return new AIDifficultySettings { 
                        ErrorRate = 0.1f, 
                        EnableSiegeCoordination = true, 
                        EnableUnitDrafting = true, 
                        PredictionDepth = 1 
                    };
            }
        }
    }
}

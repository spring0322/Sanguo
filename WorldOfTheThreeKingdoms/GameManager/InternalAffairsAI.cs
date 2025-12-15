using System;
using System.Collections.Generic;
using GameGlobal;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 内政AI - 基于投资回报计算的城市发展决策
    /// </summary>
    public class InternalAffairsAI
    {
        // 默认参数（当 CommonData 中没有对应值时使用）
        private const float DefaultAgricultureDevelopmentCost = 100f;
        private const float DefaultCommercialDevelopmentCost = 100f;
        private const float DefaultRecruitmentCost = 50f;
        private const float DefaultFoodPrice = 0.1f;
        private const float DefaultFoodConsumptionPerTroop = 0.5f;
        private const float DefaultMinCityFund = 500f;
        private const float DefaultMaxRecruitRatio = 0.1f;

        public static InternalAffairsAI Instance { get; private set; }

        public InternalAffairsAI()
        {
            Instance = this;
        }

        /// <summary>
        /// 处理势力每回合的内政决策
        /// </summary>
        public void ProcessTurn(Faction faction)
        {
            if (faction == null || !faction.IsAlive || faction.ArchitectureCount == 0) return;

            foreach (Architecture city in faction.Architectures)
            {
                if (city == null) continue;
                ProcessCityDevelopment(faction, city);
            }
        }

        /// <summary>
        /// 处理单个城市的发展决策
        /// </summary>
        private void ProcessCityDevelopment(Faction faction, Architecture city)
        {
            try
            {
                // 1. 紧急状态检查：如果粮草不够维持下个季度，强制种田
                float dailyFoodConsumption = city.ArmyQuantity * DefaultFoodConsumptionPerTroop;
                float safetyFood = dailyFoodConsumption * 90; // 3个月存量
                
                if (city.Food < safetyFood)
                {
                    DevelopAgriculture(city);
                    return;
                }

                // 2. 计算投资回报率 (ROI)
                // 农业每点提升带来 0.5 粮/季，粮价 0.1 金
                float agricultureROI = (0.5f * DefaultFoodPrice) / Math.Max(1, DefaultAgricultureDevelopmentCost);
                
                // 商业每点提升带来 0.2 金/季
                float commerceROI = 0.2f / Math.Max(1, DefaultCommercialDevelopmentCost);

                // 3. 决策逻辑
                if (city.Fund < DefaultMinCityFund) // 资金低于安全线
                {
                    DevelopCommerce(city);
                }
                else if (city.ArmyQuantity < city.Population * DefaultMaxRecruitRatio) // 兵役人口未满
                {
                    // 只有在资金充裕时才征兵
                    if (city.Fund > city.ArmyQuantity * DefaultRecruitmentCost)
                    {
                        Recruit(city);
                    }
                    else
                    {
                        DevelopCommerce(city); // 没钱征兵就去搞钱
                    }
                }
                else
                {
                    // 比较 ROI 决定发展方向
                    if (agricultureROI > commerceROI && city.Agriculture < city.AgricultureCeiling)
                    {
                        DevelopAgriculture(city);
                    }
                    else if (city.Commerce < city.CommerceCeiling)
                    {
                        DevelopCommerce(city);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[InternalAffairsAI] ProcessCityDevelopment 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 发展农业
        /// </summary>
        private void DevelopAgriculture(Architecture city)
        {
            // 示例：分配空闲武将进行农业工作
            // 实际实现需要调用游戏API
            System.Diagnostics.Debug.WriteLine($"[InternalAffairsAI] {city.Name} 优先发展农业");
        }

        /// <summary>
        /// 发展商业
        /// </summary>
        private void DevelopCommerce(Architecture city)
        {
            // 示例：分配空闲武将进行商业工作
            System.Diagnostics.Debug.WriteLine($"[InternalAffairsAI] {city.Name} 优先发展商业");
        }

        /// <summary>
        /// 征兵
        /// </summary>
        private void Recruit(Architecture city)
        {
            // 示例：分配空闲武将进行征兵
            System.Diagnostics.Debug.WriteLine($"[InternalAffairsAI] {city.Name} 优先征兵");
        }

        /// <summary>
        /// 获取推荐的发展方向
        /// </summary>
        public string GetRecommendedDevelopment(Architecture city)
        {
            if (city == null) return "无";

            float dailyFoodConsumption = city.ArmyQuantity * DefaultFoodConsumptionPerTroop;
            float safetyFood = dailyFoodConsumption * 90;

            if (city.Food < safetyFood)
                return "农业 (粮草不足)";
            
            if (city.Fund < DefaultMinCityFund)
                return "商业 (资金不足)";

            if (city.ArmyQuantity < city.Population * DefaultMaxRecruitRatio && 
                city.Fund > city.ArmyQuantity * DefaultRecruitmentCost)
                return "征兵";

            if (city.Agriculture < city.AgricultureCeiling)
                return "农业";

            if (city.Commerce < city.CommerceCeiling)
                return "商业";

            return "已达发展上限";
        }

        /// <summary>
        /// 获取城市发展状态摘要
        /// </summary>
        public string GetCityDevelopmentSummary(Architecture city)
        {
            if (city == null) return "";

            return $"城市: {city.Name}\n" +
                   $"  资金: {city.Fund}, 粮草: {city.Food}\n" +
                   $"  农业: {city.Agriculture}/{city.AgricultureCeiling}\n" +
                   $"  商业: {city.Commerce}/{city.CommerceCeiling}\n" +
                   $"  推荐: {GetRecommendedDevelopment(city)}";
        }
    }
}

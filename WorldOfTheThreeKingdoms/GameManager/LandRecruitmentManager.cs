using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameObjects.TroopDetail;
using GameObjects.PersonDetail;

namespace GameManager
{
    /// <summary>
    /// 陆军智能编队管理器 - 负责步、骑、弓、器械的自动适配
    /// </summary>
    public class LandRecruitmentManager
    {
        // 攻城模式开关：如果是攻城战，优先选器械
        public MilitaryKind SelectBestUnit(Person leader, Architecture city, bool isSiegeMode = false)
        {
            // 1. 获取城市所有可编制的兵种
            if (city.Militaries == null) return GetBasicInfantry();

            var availableUnits = new List<Military>();
            foreach (GameObject obj in city.Militaries.GetList())
            {
                if (obj is Military m && m.Kind != null)
                {
                    availableUnits.Add(m);
                }
            }
            
            if (availableUnits.Count == 0) return GetBasicInfantry();

            MilitaryKind bestUnit = null;
            float bestScore = -9999f;

            foreach (var military in availableUnits)
            {
                MilitaryKind unit = military.Kind;
                MilitaryType unitType = unit.Type;
                
                // --- 过滤器 ---

                // A. 排除水军 (由 NavalManager 处理)
                if (unitType == MilitaryType.水军) continue;

                // B. 攻城模式过滤
                if (isSiegeMode)
                {
                    // 攻城模式下，只看器械 或 强力步兵
                    if (unitType != MilitaryType.器械 && unitType != MilitaryType.步兵) continue;
                }
                else
                {
                    // 野战模式下，排除龟速的器械
                    if (unitType == MilitaryType.器械) continue;
                }

                // C. 经济检查 (买得起最小编制才看)
                // 按最小编制算，如果连最小编制的钱都不够，那确实不能带
                if (city.Fund < unit.CreateCost * unit.MinScale) continue;

                // --- 评分器 (包含统率软门槛逻辑) ---
                float score = MilitaryCapabilityEvaluator.CalculateCapability(leader, unit);

                // 如果评分更高，则替换
                if (score > bestScore)
                {
                    bestScore = score;
                    bestUnit = unit;
                }
            }

            return bestUnit ?? GetBasicInfantry();
        }

        private MilitaryKind GetBasicInfantry()
        {
            // 获取 ID 为 0 的基础步兵作为兜底
            if (Session.Current?.Scenario?.GameCommonData?.AllMilitaryKinds != null)
            {
                var mk = Session.Current.Scenario.GameCommonData.AllMilitaryKinds.GetMilitaryKind(0);
                if (mk != null) return mk;
                
                // Or first Infantry type
                foreach (MilitaryKind k in Session.Current.Scenario.GameCommonData.AllMilitaryKinds.MilitaryKinds.Values)
                {
                    if (k.Type == MilitaryType.步兵) return k;
                }
            }
            return null;
        }
    }
}

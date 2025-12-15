using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects.Influences;

namespace GameObjects
{
    /// <summary>
    /// 建筑类型枚举
    /// </summary>
    public enum BuildingType
    {
        None,           // 无建筑
        Market,         // 市场 - 增加收入
        Farm,           // 农田 - 增加粮食产量
        Barracks,       // 兵营 - 训练士兵
        Wall,           // 城墙 - 增加防御
        Academy,        // 学院 - 提升科技
        Temple,         // 寺庙 - 提升民心
        Workshop,       // 工坊 - 生产装备
        Granary,        // 粮仓 - 储存粮食
        Fortress        // 要塞 - 军事防御
    }

    /// <summary>
    /// 性格特质枚举
    /// </summary>
    public enum Trait
    {
        Rash,           // 莽撞 - 偏好军事建筑
        Fame,           // 名声 - 偏好声望建筑
        Greedy,         // 贪婪 - 偏好经济建筑
        Cautious,       // 谨慎 - 偏好防御建筑
        Scholar,        // 学者 - 偏好文化建筑
        Pragmatic,      // 实用主义 - 平衡发展
        Ambitious,      // 野心勃勃 - 快速扩张
        Conservative,   // 保守 - 稳健发展
        Loyal,          // 忠诚 - 高服从度
        Rebellious      // 叛逆 - 低服从度
    }

    /// <summary>
    /// 数学工具类 - 提供常用数学函数
    /// </summary>
    public static class Mathf
    {
        /// <summary>
        /// 将值限制在指定范围内
        /// </summary>
        public static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>
        /// 线性插值
        /// </summary>
        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Clamp(t, 0f, 1f);
        }
    }

    /// <summary>
    /// 势力状态枚举
    /// </summary>
    public enum FactionState
    {
        Prosperous,     // 繁荣
        Stable,         // 稳定
        EconomicCrisis, // 经济危机
        WarTime,        // 战时
        Expansion,      // 扩张期
        Defensive       // 防守期
    }

    /// <summary>
    /// 城市建设AI系统 - 基于太守性格的智能建设决策
    /// </summary>
    public class CityConstructionAI
    {
        /// <summary>
        /// 更新城市建设决策 - 增强版：包含服从度和性格冲突机制
        /// </summary>
        public void UpdateCityConstruction(City city)
        {
            if (city.FreeSlots <= 0 || city.IsBuilding) return;

            var factionState = city.Owner.State;
            var prefect = city.Prefect;

            // 1. 获取基础得分 (AI整体意志)
            Dictionary<BuildingType, float> scores = GetBaseFactionScores(city, factionState);

            // 2. 如果有太守，应用太守的性格偏差
            if (prefect != null)
            {
                // 计算服从度：义理越高越听话，野心越高越叛逆
                // 范围 0.2 (完全捣乱) ~ 1.0 (完全执行)
                float compliance = (prefect.Righteousness / 100f) - (prefect.Ambition / 200f);
                
                // 忠诚特质加成
                if (prefect.Traits.Contains(Trait.Loyal)) 
                    compliance += 0.2f;
                
                // 叛逆特质惩罚
                if (prefect.Traits.Contains(Trait.Rebellious)) 
                    compliance -= 0.3f;
                
                compliance = Mathf.Clamp(compliance, 0.2f, 1.0f);

                System.Diagnostics.Debug.WriteLine($"[CityAI] {prefect.Name} 服从度: {compliance:F2} (义理:{prefect.Righteousness}, 野心:{prefect.Ambition})");

                // 应用偏差
                var buildingTypes = new List<BuildingType>(scores.Keys);
                foreach (var type in buildingTypes)
                {
                    float factionWill = scores[type];           // 势力意志
                    float prefectWill = GetPrefectPreference(prefect, type); // 太守个人偏好

                    // 最终决策融合：服从度高时更听从势力意志，服从度低时更按个人喜好
                    scores[type] = Mathf.Lerp(prefectWill, factionWill, compliance);
                    
                    System.Diagnostics.Debug.WriteLine($"[CityAI] {type}: 势力意志={factionWill:F1}, 太守偏好={prefectWill:F1}, 最终={scores[type]:F1}");
                }
            }

            // 3. 选出最高分并执行
            var best = scores.OrderByDescending(x => x.Value).FirstOrDefault();
            if (best.Value > 0 && city.Owner.Gold >= GetBuildingCost(best.Key))
            {
                city.StartConstruction(best.Key);
                
                string decisionMaker = prefect != null ? $"太守 {prefect.Name}" : "君主直辖";
                System.Diagnostics.Debug.WriteLine($"[CityAI] {city.Name} 由 {decisionMaker} 决定建造 {best.Key} (评分:{best.Value:F1})");
            }
            else if (best.Value <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"[CityAI] {city.Name} 所有建筑评分都不合格，暂不建设");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[CityAI] {city.Name} 资金不足，无法建造 {best.Key} (需要:{GetBuildingCost(best.Key)}, 拥有:{city.Owner.Gold})");
            }
        }

        /// <summary>
        /// 获取势力基础评分 - 代表君主和国家的整体意志
        /// </summary>
        private Dictionary<BuildingType, float> GetBaseFactionScores(City city, FactionState factionState)
        {
            Dictionary<BuildingType, float> scores = new Dictionary<BuildingType, float>();

            // === 基础需求评分 ===
            CalculateBaseScores(city, scores);

            // === 战略地图修正 ===
            ApplyStrategicModifiers(city, scores);

            // === 势力状态修正 ===
            ApplyFactionStateModifiers(factionState, scores);

            return scores;
        }

        /// <summary>
        /// 获取太守个人偏好 - 代表太守的个人意志和性格倾向
        /// </summary>
        private float GetPrefectPreference(Officer prefect, BuildingType type)
        {
            float score = 50; // 基础中性分数

            // === 性格特质强烈影响 ===
            foreach (var trait in prefect.Traits)
            {
                switch (trait)
                {
                    case Trait.Rash: // 莽撞人想要兵营
                        if (type == BuildingType.Barracks) score = 400;
                        if (type == BuildingType.Market) score = 10; // 极度厌恶经济建筑
                        if (type == BuildingType.Academy) score = 5;  // 不喜欢文化
                        break;

                    case Trait.Greedy: // 贪婪人想要钱
                        if (type == BuildingType.Market) score = 500;
                        if (type == BuildingType.Farm) score = 15;    // 不关心粮食
                        if (type == BuildingType.Temple) score = 5;   // 不关心民心
                        break;

                    case Trait.Scholar: // 学者偏好文化建筑
                        if (type == BuildingType.Academy) score = 450;
                        if (type == BuildingType.Temple) score = 300;
                        if (type == BuildingType.Barracks) score = 20; // 相对不喜欢纯军事
                        break;

                    case Trait.Cautious: // 谨慎偏好防御
                        if (type == BuildingType.Wall) score = 380;
                        if (type == BuildingType.Granary) score = 350;
                        if (type == BuildingType.Fortress) score = 320;
                        break;

                    case Trait.Fame: // 名声偏好声望建筑
                        if (type == BuildingType.Temple) score = 400;
                        if (type == BuildingType.Academy) score = 350;
                        if (type == BuildingType.Wall) score = 300; // 城墙也有面子
                        break;

                    case Trait.Pragmatic: // 实用主义根据实际需要
                        // 会根据城市状况调整，这里给个平衡分数
                        score = 100;
                        break;

                    case Trait.Ambitious: // 野心勃勃快速扩张
                        if (type == BuildingType.Barracks) score = 380;
                        if (type == BuildingType.Market) score = 320;
                        if (type == BuildingType.Academy) score = 30; // 急功近利
                        break;

                    case Trait.Conservative: // 保守稳健发展
                        if (type == BuildingType.Farm) score = 350;
                        if (type == BuildingType.Wall) score = 300;
                        if (type == BuildingType.Temple) score = 280;
                        break;
                }
            }

            // === 能力值影响个人偏好 ===
            // 文官不喜欢打仗
            if (prefect.War < 30 && type == BuildingType.Barracks) 
                score -= 100;

            // 武官不喜欢搞文化
            if (prefect.Politics < 30 && type == BuildingType.Academy) 
                score -= 80;

            // 政治高的喜欢经济建筑
            if (prefect.Politics > 80 && (type == BuildingType.Market || type == BuildingType.Farm))
                score += 50;

            // 统率高的喜欢军事建筑
            if (prefect.Leadership > 80 && (type == BuildingType.Barracks || type == BuildingType.Fortress))
                score += 50;

            // === 个人经历影响 ===
            if (prefect.HasExperience("SiegeDefense") && type == BuildingType.Wall)
                score += 100; // 守城经验让他重视城墙

            if (prefect.HasExperience("Famine") && type == BuildingType.Farm)
                score += 120; // 饥荒经验让他重视农业

            if (prefect.HasExperience("Rebellion") && type == BuildingType.Temple)
                score += 100; // 民变经验让他重视民心

            // === 年龄影响 ===
            if (prefect.Age > 60) // 老人更保守
            {
                if (type == BuildingType.Wall || type == BuildingType.Temple)
                    score += 30;
                if (type == BuildingType.Barracks)
                    score -= 20;
            }

            if (prefect.Age < 30) // 年轻人更激进
            {
                if (type == BuildingType.Barracks || type == BuildingType.Academy)
                    score += 30;
            }

            return Math.Max(score, 0); // 确保不为负数
        }

        /// <summary>
        /// 计算基础需求评分
        /// </summary>
        private void CalculateBaseScores(City city, Dictionary<BuildingType, float> scores)
        {
            // 经济需求
            float incomeScore = (1.0f - city.Owner.FiscalHealth) * 200;
            scores.Add(BuildingType.Market, incomeScore);

            // 粮食需求
            float foodScore = (city.Owner.Food < city.Owner.TotalTroops * 12) ? 150 : 50;
            scores.Add(BuildingType.Farm, foodScore);

            // 人口和民心需求
            float populationPressure = city.Population / (float)city.MaxPopulation;
            scores.Add(BuildingType.Temple, populationPressure * 100);

            // 科技需求
            float techScore = city.Owner.TechLevel < 50 ? 80 : 30;
            scores.Add(BuildingType.Academy, techScore);

            // 军事需求基础分
            scores.Add(BuildingType.Barracks, 60);
            scores.Add(BuildingType.Wall, 40);
            scores.Add(BuildingType.Workshop, 45);
            scores.Add(BuildingType.Granary, 35);
            scores.Add(BuildingType.Fortress, 25);
        }

        /// <summary>
        /// 应用战略修正
        /// </summary>
        private void ApplyStrategicModifiers(City city, Dictionary<BuildingType, float> scores)
        {
            float threatLevel = GetThreatLevel(city);
            
            if (threatLevel > 0.6f) // 高威胁区域
            {
                // 经济建筑大幅降权
                scores[BuildingType.Market] *= 0.2f;
                scores[BuildingType.Farm] *= 0.5f;
                scores[BuildingType.Academy] *= 0.3f;
                
                // 军事建筑大幅提权
                scores[BuildingType.Barracks] = 300;
                scores[BuildingType.Wall] = 250;
                scores[BuildingType.Fortress] = 200;
            }
            else if (threatLevel < 0.3f) // 安全区域
            {
                // 经济建筑提权
                scores[BuildingType.Market] *= 1.5f;
                scores[BuildingType.Farm] *= 1.3f;
                scores[BuildingType.Academy] *= 1.4f;
                
                // 军事建筑降权
                scores[BuildingType.Barracks] *= 0.5f;
                scores[BuildingType.Wall] *= 0.3f;
                scores[BuildingType.Fortress] *= 0.2f;
            }

            // 地理位置修正
            if (city.IsCapital)
            {
                scores[BuildingType.Academy] *= 1.5f; // 首都重视科技
                scores[BuildingType.Temple] *= 1.3f;  // 首都重视民心
            }

            if (city.IsPortCity)
            {
                scores[BuildingType.Market] *= 1.4f; // 港口城市重视贸易
            }

            if (city.IsBorderCity)
            {
                scores[BuildingType.Wall] *= 1.6f;     // 边境城市重视防御
                scores[BuildingType.Fortress] *= 1.5f;
            }
        }

        /// <summary>
        /// 应用太守个人偏好修正 - 核心人性化逻辑
        /// </summary>
        private void ApplyPrefectBias(Officer prefect, Dictionary<BuildingType, float> scores, float threatLevel)
        {
            // 1. 能力值影响 (Ability Influence)
            ApplyAbilityModifiers(prefect, scores);

            // 2. 性格标签影响 (Personality Tags)
            ApplyPersonalityModifiers(prefect, scores, threatLevel);

            // 3. 个人经历影响 (Experience Influence)
            ApplyExperienceModifiers(prefect, scores);

            // 4. 年龄和健康影响
            ApplyAgeAndHealthModifiers(prefect, scores);
        }

        /// <summary>
        /// 应用能力值修正
        /// </summary>
        private void ApplyAbilityModifiers(Officer prefect, Dictionary<BuildingType, float> scores)
        {
            float polFactor = prefect.Politics / 100f;   // 0.1 ~ 1.0
            float leadFactor = prefect.Leadership / 100f; // 0.1 ~ 1.0
            float intFactor = prefect.Intelligence / 100f; // 0.1 ~ 1.0

            // 政治高的太守，会放大经济建筑的权重（因为他们擅长这个）
            if (scores.ContainsKey(BuildingType.Market)) 
                scores[BuildingType.Market] *= (1.0f + polFactor);
            if (scores.ContainsKey(BuildingType.Farm)) 
                scores[BuildingType.Farm] *= (1.0f + polFactor * 0.8f);

            // 统率高的太守，会放大军事建筑的权重
            if (scores.ContainsKey(BuildingType.Barracks)) 
                scores[BuildingType.Barracks] *= (1.0f + leadFactor);
            if (scores.ContainsKey(BuildingType.Fortress)) 
                scores[BuildingType.Fortress] *= (1.0f + leadFactor * 0.9f);

            // 智力高的太守，重视科技和文化建筑
            if (scores.ContainsKey(BuildingType.Academy)) 
                scores[BuildingType.Academy] *= (1.0f + intFactor);
            if (scores.ContainsKey(BuildingType.Temple)) 
                scores[BuildingType.Temple] *= (1.0f + intFactor * 0.7f);
        }

        /// <summary>
        /// 应用性格特质修正
        /// </summary>
        private void ApplyPersonalityModifiers(Officer prefect, Dictionary<BuildingType, float> scores, float threatLevel)
        {
            foreach (var trait in prefect.Traits)
            {
                switch (trait)
                {
                    case Trait.Rash: // 莽撞 (如吕布、张飞)
                        // 极度厌恶造市场，哪怕没钱也要造兵营
                        if (scores.ContainsKey(BuildingType.Market)) 
                            scores[BuildingType.Market] *= 0.5f;
                        if (scores.ContainsKey(BuildingType.Barracks)) 
                            scores[BuildingType.Barracks] *= 1.8f;
                        if (scores.ContainsKey(BuildingType.Academy)) 
                            scores[BuildingType.Academy] *= 0.6f; // 不重视文化
                        break;

                    case Trait.Fame: // 名声 (如刘备、袁绍)
                        // 喜欢造城墙或者治安设施，为了好看和稳固
                        if (scores.ContainsKey(BuildingType.Wall)) 
                            scores[BuildingType.Wall] *= 1.5f;
                        if (scores.ContainsKey(BuildingType.Temple)) 
                            scores[BuildingType.Temple] *= 1.4f;
                        if (scores.ContainsKey(BuildingType.Academy)) 
                            scores[BuildingType.Academy] *= 1.3f;
                        break;

                    case Trait.Greedy: // 贪婪 (如董卓)
                        // 只造钱，不造粮
                        if (scores.ContainsKey(BuildingType.Market)) 
                            scores[BuildingType.Market] *= 2.5f;
                        if (scores.ContainsKey(BuildingType.Farm)) 
                            scores[BuildingType.Farm] *= 0.3f;
                        if (scores.ContainsKey(BuildingType.Temple)) 
                            scores[BuildingType.Temple] *= 0.2f; // 不关心民心
                        break;

                    case Trait.Cautious: // 谨慎 (如司马懿)
                        // 重视防御和储备
                        if (scores.ContainsKey(BuildingType.Wall)) 
                            scores[BuildingType.Wall] *= 1.6f;
                        if (scores.ContainsKey(BuildingType.Granary)) 
                            scores[BuildingType.Granary] *= 1.8f;
                        if (scores.ContainsKey(BuildingType.Fortress)) 
                            scores[BuildingType.Fortress] *= 1.4f;
                        break;

                    case Trait.Scholar: // 学者 (如诸葛亮、庞统)
                        // 重视科技和文化
                        if (scores.ContainsKey(BuildingType.Academy)) 
                            scores[BuildingType.Academy] *= 2.0f;
                        if (scores.ContainsKey(BuildingType.Temple)) 
                            scores[BuildingType.Temple] *= 1.5f;
                        if (scores.ContainsKey(BuildingType.Barracks)) 
                            scores[BuildingType.Barracks] *= 0.8f; // 相对不重视纯军事
                        break;

                    case Trait.Pragmatic: // 实用主义 (如曹操)
                        // 根据实际需要调整，不会有极端偏好
                        // 在威胁高时重军事，威胁低时重经济
                        if (threatLevel > 0.5f)
                        {
                            if (scores.ContainsKey(BuildingType.Barracks)) 
                                scores[BuildingType.Barracks] *= 1.3f;
                        }
                        else
                        {
                            if (scores.ContainsKey(BuildingType.Market)) 
                                scores[BuildingType.Market] *= 1.3f;
                        }
                        break;

                    case Trait.Ambitious: // 野心勃勃 (如袁术、刘表)
                        // 快速扩张，重视军事和经济
                        if (scores.ContainsKey(BuildingType.Barracks)) 
                            scores[BuildingType.Barracks] *= 1.4f;
                        if (scores.ContainsKey(BuildingType.Market)) 
                            scores[BuildingType.Market] *= 1.3f;
                        if (scores.ContainsKey(BuildingType.Academy)) 
                            scores[BuildingType.Academy] *= 0.7f; // 急功近利
                        break;

                    case Trait.Conservative: // 保守 (如刘璋)
                        // 稳健发展，重视防御和民生
                        if (scores.ContainsKey(BuildingType.Wall)) 
                            scores[BuildingType.Wall] *= 1.3f;
                        if (scores.ContainsKey(BuildingType.Farm)) 
                            scores[BuildingType.Farm] *= 1.4f;
                        if (scores.ContainsKey(BuildingType.Temple)) 
                            scores[BuildingType.Temple] *= 1.2f;
                        break;
                }
            }
        }

        /// <summary>
        /// 应用个人经历修正
        /// </summary>
        private void ApplyExperienceModifiers(Officer prefect, Dictionary<BuildingType, float> scores)
        {
            // 如果太守曾经历过围城战
            if (prefect.HasExperience("SiegeDefense"))
            {
                if (scores.ContainsKey(BuildingType.Wall)) 
                    scores[BuildingType.Wall] *= 1.5f;
                if (scores.ContainsKey(BuildingType.Granary)) 
                    scores[BuildingType.Granary] *= 1.3f;
            }

            // 如果太守曾经历过饥荒
            if (prefect.HasExperience("Famine"))
            {
                if (scores.ContainsKey(BuildingType.Farm)) 
                    scores[BuildingType.Farm] *= 1.6f;
                if (scores.ContainsKey(BuildingType.Granary)) 
                    scores[BuildingType.Granary] *= 1.4f;
            }

            // 如果太守曾经历过民变
            if (prefect.HasExperience("Rebellion"))
            {
                if (scores.ContainsKey(BuildingType.Temple)) 
                    scores[BuildingType.Temple] *= 1.7f;
                if (scores.ContainsKey(BuildingType.Market)) 
                    scores[BuildingType.Market] *= 0.8f; // 认为过度商业化导致民变
            }
        }

        /// <summary>
        /// 应用年龄和健康修正
        /// </summary>
        private void ApplyAgeAndHealthModifiers(Officer prefect, Dictionary<BuildingType, float> scores)
        {
            // 年老的太守更保守
            if (prefect.Age > 60)
            {
                if (scores.ContainsKey(BuildingType.Wall)) 
                    scores[BuildingType.Wall] *= 1.2f;
                if (scores.ContainsKey(BuildingType.Temple)) 
                    scores[BuildingType.Temple] *= 1.3f;
                if (scores.ContainsKey(BuildingType.Barracks)) 
                    scores[BuildingType.Barracks] *= 0.9f;
            }

            // 年轻的太守更激进
            if (prefect.Age < 30)
            {
                if (scores.ContainsKey(BuildingType.Barracks)) 
                    scores[BuildingType.Barracks] *= 1.2f;
                if (scores.ContainsKey(BuildingType.Academy)) 
                    scores[BuildingType.Academy] *= 1.1f;
            }

            // 健康状况影响决策
            if (prefect.Health < 50)
            {
                // 健康不佳的太守决策更保守
                foreach (var key in scores.Keys.ToList())
                {
                    if (key == BuildingType.Wall || key == BuildingType.Temple)
                        scores[key] *= 1.1f;
                    else
                        scores[key] *= 0.95f;
                }
            }
        }

        /// <summary>
        /// 应用势力状态修正
        /// </summary>
        private void ApplyFactionStateModifiers(FactionState factionState, Dictionary<BuildingType, float> scores)
        {
            switch (factionState)
            {
                case FactionState.EconomicCrisis:
                    // 经济危机时，停止军事建设
                    scores[BuildingType.Barracks] = -1;
                    scores[BuildingType.Wall] = -1;
                    scores[BuildingType.Fortress] = -1;
                    // 专注经济恢复
                    scores[BuildingType.Market] *= 2.0f;
                    scores[BuildingType.Farm] *= 1.5f;
                    break;

                case FactionState.WarTime:
                    // 战时优先军事
                    scores[BuildingType.Barracks] *= 2.0f;
                    scores[BuildingType.Wall] *= 1.8f;
                    scores[BuildingType.Fortress] *= 1.6f;
                    // 经济建筑降权
                    scores[BuildingType.Market] *= 0.5f;
                    scores[BuildingType.Academy] *= 0.3f;
                    break;

                case FactionState.Expansion:
                    // 扩张期平衡发展
                    scores[BuildingType.Barracks] *= 1.3f;
                    scores[BuildingType.Market] *= 1.2f;
                    break;

                case FactionState.Defensive:
                    // 防守期重视防御
                    scores[BuildingType.Wall] *= 1.8f;
                    scores[BuildingType.Fortress] *= 1.6f;
                    scores[BuildingType.Granary] *= 1.4f;
                    break;

                case FactionState.Prosperous:
                    // 繁荣期重视文化和科技
                    scores[BuildingType.Academy] *= 1.5f;
                    scores[BuildingType.Temple] *= 1.3f;
                    break;
            }
        }

        /// <summary>
        /// 执行建设决策
        /// </summary>
        private void ExecuteBuildingDecision(City city, Dictionary<BuildingType, float> scores, Officer prefect)
        {
            var bestBuilding = scores.OrderByDescending(x => x.Value).FirstOrDefault();

            // 增加一个判定：太守的政治力决定了是否能准确执行（低政治太守可能判断失误或偷懒）
            if (bestBuilding.Key != BuildingType.None && bestBuilding.Value > 0)
            {
                // 政治能力影响执行准确性
                float executionAccuracy = 1.0f;
                if (prefect != null)
                {
                    executionAccuracy = 0.7f + (prefect.Politics / 100f) * 0.3f; // 0.7 ~ 1.0
                    
                    // 低政治太守可能做出错误决策
                    if (prefect.Politics < 40 && UnityEngine.Random.Range(0f, 1f) > executionAccuracy)
                    {
                        // 随机选择一个次优选项
                        var alternatives = scores.OrderByDescending(x => x.Value).Skip(1).Take(3).ToList();
                        if (alternatives.Count > 0)
                        {
                            bestBuilding = alternatives[UnityEngine.Random.Range(0, alternatives.Count)];
                            System.Diagnostics.Debug.WriteLine($"[CityAI] {prefect.Name} 政治能力不足，做出了次优决策：{bestBuilding.Key}");
                        }
                    }
                }

                // 简单判定：只要钱够就造
                if (city.Owner.Gold >= GetBuildingCost(bestBuilding.Key))
                {
                    city.StartConstruction(bestBuilding.Key);
                    System.Diagnostics.Debug.WriteLine($"[CityAI] {city.Name} 开始建造 {bestBuilding.Key}，评分：{bestBuilding.Value:F1}");
                    
                    if (prefect != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CityAI] 太守 {prefect.Name} 的决策，性格特质：{string.Join(",", prefect.Traits)}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[CityAI] {city.Name} 资金不足，无法建造 {bestBuilding.Key}");
                }
            }
        }

        /// <summary>
        /// 获取威胁等级
        /// </summary>
        private float GetThreatLevel(City city)
        {
            // 这里需要根据实际的战略地图系统来实现
            // 简化实现：基于城市位置和周围敌军情况
            float threat = 0.0f;

            // 边境城市威胁更高
            if (city.IsBorderCity) threat += 0.4f;

            // 根据周围敌军数量
            var nearbyEnemies = GetNearbyEnemyTroops(city);
            threat += Math.Min(nearbyEnemies * 0.1f, 0.5f);

            // 根据最近的战争记录
            if (city.HasRecentBattle) threat += 0.3f;

            return Math.Min(threat, 1.0f);
        }

        /// <summary>
        /// 获取建筑成本
        /// </summary>
        private int GetBuildingCost(BuildingType buildingType)
        {
            switch (buildingType)
            {
                case BuildingType.Market: return 1000;
                case BuildingType.Farm: return 800;
                case BuildingType.Barracks: return 1200;
                case BuildingType.Wall: return 1500;
                case BuildingType.Academy: return 2000;
                case BuildingType.Temple: return 900;
                case BuildingType.Workshop: return 1100;
                case BuildingType.Granary: return 700;
                case BuildingType.Fortress: return 2500;
                default: return 1000;
            }
        }

        /// <summary>
        /// 获取附近敌军数量
        /// </summary>
        private int GetNearbyEnemyTroops(City city)
        {
            // 简化实现，实际需要连接到游戏的军队系统
            return UnityEngine.Random.Range(0, 5);
        }
    }
}
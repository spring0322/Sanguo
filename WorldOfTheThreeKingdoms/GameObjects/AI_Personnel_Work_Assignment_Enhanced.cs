/// <summary>
/// AI人员工作分配系统 - 整合版
/// 基于武将能力和城市需求的智能工作分配
/// 整合了用户提供的逻辑和现有代码结构
/// </summary>

using WorldOfTheThreeKingdoms.GameGlobal;
using System;
using System.Linq;
using GameManager;
using GameObjects.ArchitectureDetail;
using GameObjects.PersonDetail;
using GameObjects.TroopDetail;

namespace GameObjects
{
    public partial class Architecture
    {
        /// <summary>
        /// 获取武将在特定工作上的效率
        /// 基于武将属性计算工作效率，包含低能力惩罚机制
        /// </summary>
        /// <param name="p">武将</param>
        /// <param name="kind">工作类型</param>
        /// <returns>工作效率 (0.0-2.0)</returns>
        private float GetPersonWorkEfficiency(Person p, ArchitectureWorkKind kind)
        {
            // 定义低智商/低政治的惩罚阈值
            const int STAT_THRESHOLD = 60;
            
            switch (kind)
            {
                case ArchitectureWorkKind.农业: // 农业吃政治
                case ArchitectureWorkKind.商业: // 商业吃政治
                    if (p.Politics < STAT_THRESHOLD) 
                        return (p.GetWorkAbility(kind) / 100.0f) * 0.6f; // 政治低给予惩罚，但允许工作
                    return Math.Min(1.25f, p.GetWorkAbility(kind) / 100.0f);
                
                case ArchitectureWorkKind.技术: // 技术吃智力
                    if (p.Intelligence < STAT_THRESHOLD) 
                        return (p.GetWorkAbility(kind) / 100.0f) * 0.6f; // 智力低给予惩罚，但允许工作
                    return Math.Min(1.25f, p.GetWorkAbility(kind) / 100.0f);
                
                case ArchitectureWorkKind.统治: // 统治吃魅力/统率
                case ArchitectureWorkKind.民心: // 民心吃魅力
                    // 猛将也可以去巡查威慑
                    return Math.Min(1.5f, p.GetWorkAbility(kind) / 200.0f);
                
                case ArchitectureWorkKind.耐久: // 修补吃统率/武力 (搬砖)
                    return Math.Min(1.5f, p.GetWorkAbility(kind) / 200.0f);
                
                case ArchitectureWorkKind.训练: // 训练吃武力/统率
                    // 猛将的最爱，给予额外加成
                    float trainScore = Math.Min(1.5f, p.GetWorkAbility(kind) / 200.0f);
                    if (p.Strength > 80) 
                        trainScore *= 1.2f; // 猛将训练权重翻倍
                    return trainScore;
                
                case ArchitectureWorkKind.补充:
                    return Math.Min(1.5f, p.GetWorkAbility(kind) / 200.0f);

                case ArchitectureWorkKind.赈灾:
                    return Math.Min(1.5f, p.GetWorkAbility(kind) / 200.0f);

                default:
                    return 1.0f;
            }
        }

        /// <summary>
        /// 智能工作分配系统 - 整合版
        /// 结合城市需求和武将能力进行最优分配
        /// </summary>
        /// <param name="p">要分配工作的武将</param>
        private void AIWorkSmart(Person p)
        {
            // 1. 获取城市的基础需求权重 (即原来的逻辑)
            float[] cityNeeds = this.GetCityWorkNeeds();
            
            // 2. 结合武将能力进行修正
            float[] finalWeights = new float[cityNeeds.Length];
            bool hasValidWork = false;
            
            for (int i = 0; i < cityNeeds.Length; i++)
            {
                ArchitectureWorkKind workKind = (ArchitectureWorkKind)(i + 1);
                
                // 计算适性：城市需求 * 武将效率
                float aptitude = GetPersonWorkEfficiency(p, workKind);
                
                // 最终权重
                finalWeights[i] = cityNeeds[i] * aptitude;
                if (finalWeights[i] > 0) hasValidWork = true;
            }
            
            // 3. 兜底逻辑：如果猛将因为智力低，所有内政都被过滤成 0 了
            // 必须给他找点事做 (比如强行安排去训练/补充治安，或者征兵)
            if (!hasValidWork)
            {
                // 如果他是猛将 (武力>70)，强制去训练或修补
                if (p.Strength > 70)
                {
                    finalWeights[6] = 100.0f; // 6 is Training
                    finalWeights[5] = 50.0f;  // 5 is Endurance
                }
                else
                {
                    // 废柴文官，去搞搞民心吧
                    finalWeights[4] = 50.0f; // 4 is Morale
                }
            }
            
            // 4. ⚔️ 猛将特别通道：征兵检查
            // 如果 城市缺兵 AND 资金充足 AND 武将是猛将
            if (this.MilitaryCount < this.Population / 5 && this.Fund > 2000)
            {
                // 如果武将武力高，或者魅力高(招募快)
                if (p.Strength > 75 || p.Glamour > 75)
                {
                    // 直接跳过内政，去执行招募 (假设你有 recruit 逻辑)
                    // 这里可以调用征兵相关的方法
                    // this.Recruit(p);
                    // return;
                    
                    // 暂时提高训练权重，模拟征兵优先
                    finalWeights[6] = 200.0f; // 6 is Training
                }
            }
            
            // 5. 执行原有的随机分配，但是传入修正后的 finalWeights
            this.AssignWorkByWeights(p, finalWeights);
            
            if (SectionAIHelper.EnableDebugOutput)
            {
                System.Diagnostics.Debug.WriteLine($"[AIWorkSmart Enhanced] {p.Name} 在 {this.Name} 被分配工作: {p.WorkKind} " +
                    $"(武力:{p.Strength} 智力:{p.Intelligence} 政治:{p.Politics} 魅力:{p.Glamour})");
            }
        }

        internal void RefreshIdleWorkAfterDailyDevelop()
        {
            this.RefreshIdleWorkForAI();
        }

        internal void RefreshIdleWorkForAI()
        {
            if (!this.ShouldRunLocalDailyWorkRefresh())
            {
                return;
            }

            bool hasIdleOfficer = false;
            for (int i = 0; i < this.Persons.Count; i++)
            {
                Person person = this.Persons[i] as Person;
                if (this.CanUseOfficerForDailyWorkRefresh(person) && person.WorkKind == ArchitectureWorkKind.无)
                {
                    hasIdleOfficer = true;
                    break;
                }
            }

            if (!hasIdleOfficer)
            {
                return;
            }

            this.EnsureMilitaryWorkTargetsForAI();

            for (int i = 0; i < this.Persons.Count; i++)
            {
                Person person = this.Persons[i] as Person;
                if (!this.CanUseOfficerForDailyWorkRefresh(person) || person.WorkKind != ArchitectureWorkKind.无)
                {
                    continue;
                }

                this.AIWorkSmart(person);
                if (person.WorkKind == ArchitectureWorkKind.无)
                {
                    this.AssignDefaultWork(person);
                }
            }
        }

        private bool ShouldRunLocalDailyWorkRefresh()
        {
            if (this.BelongedFaction == null || !this.HasPerson())
            {
                return false;
            }

            if (!Session.Current.Scenario.IsPlayer(this.BelongedFaction))
            {
                return true;
            }

            return this.BelongedSection?.AIDetail?.AutoRun == true;
        }

        private bool CanUseOfficerForDailyWorkRefresh(Person p)
        {
            return p != null &&
                   p.Alive &&
                   !p.IsCaptive &&
                   p.BelongedCaptive == null &&
                   p.Status == PersonStatus.Normal &&
                   p.LocationArchitecture == this;
        }

        /// <summary>
        /// 获取城市工作需求权重数组
        /// 基于现有的AIWorkSmart逻辑提取城市需求
        /// </summary>
        /// <returns>工作需求权重数组 [农业,商业,技术,统治,民心,耐久,训练]</returns>
        private float[] GetCityWorkNeeds()
        {
            // 获取动态阈值
            float limit = this.InternalAffairSaturationThreshold;
            
            // 定义工作权重数组
            // [0]=农业, [1]=商业, [2]=技术, [3]=统治, [4]=民心, [5]=耐久, [6]=训练
            float[] workWeights = new float[9];
            
            // --- 农业检查 ---
            if (_architectureKind.HasAgriculture && this.Agriculture < this.AgricultureCeiling)
            {
                if (this.Agriculture < this.AgricultureCeiling * limit)
                {
                    // 没达标：正常权重 (优先干)
                    workWeights[0] = 100.0f;
                }
                else
                {
                    // 达标了但没满：填缝权重 (没事干才干这个)
                    workWeights[0] = 1.0f;
                }
            }
            
            // --- 商业检查 ---
            if (_architectureKind.HasCommerce && this.Commerce < this.CommerceCeiling)
            {
                if (this.Commerce < this.CommerceCeiling * limit)
                {
                    workWeights[1] = 100.0f;
                }
                else
                {
                    workWeights[1] = 1.0f;
                }
            }
            
            // --- 技术检查 ---
            if (_architectureKind.HasTechnology && this.Technology < this.TechnologyCeiling)
            {
                if (this.Technology < this.TechnologyCeiling * limit)
                {
                    workWeights[2] = 100.0f;
                }
                else
                {
                    workWeights[2] = 1.0f;
                }
            }
            
            // --- 统治检查 ---
            if (_architectureKind.HasDomination && this.Domination < this.DominationCeiling)
            {
                if (this.Domination < this.DominationCeiling * limit)
                {
                    workWeights[3] = 100.0f;
                }
                else
                {
                    workWeights[3] = 1.0f;
                }
            }
            
            // --- 民心检查 ---
            if (_architectureKind.HasMorale && this.Morale < this.MoraleCeiling)
            {
                if (this.Morale < this.MoraleCeiling * limit)
                {
                    workWeights[4] = 100.0f;
                }
                else
                {
                    workWeights[4] = 1.0f;
                }
            }
            
            // --- 耐久检查 ---
            if (_architectureKind.HasEndurance && this.Endurance < this.EnduranceCeiling)
            {
                if (this.Endurance < this.EnduranceCeiling * limit)
                {
                    workWeights[5] = 100.0f;
                }
                else
                {
                    workWeights[5] = 1.0f;
                }
            }
            
            // --- 训练检查 ---
            MilitaryList trainingMilitaryList = this.GetTrainingMilitaryList();
            if (trainingMilitaryList.Count > 0)
            {
                // 训练总是高优先级（军事需求）
                workWeights[6] = 150.0f;
                
                // 检查是否只需要一人训练
                if (trainingMilitaryList.Count == 1)
                {
                    Military m = trainingMilitaryList[0] as Military;
                    if (m.Morale >= m.MoraleCeiling - 3 && m.Combativity >= m.CombativityCeiling - 3)
                    {
                        workWeights[6] = 50.0f; // 降低权重，只需要一人训练
                    }
                }
            }
            
            // --- 特殊情况调整 ---
            
            // 最近被攻击：优先耐久修复
            if (this.CanExecuteAIRecruitment(false) && this.RecruitmentAvail())
            {
                MilitaryList recruitmentMilitaryList = this.GetRecruitmentMilitaryList();
                if (recruitmentMilitaryList.Count > 0)
                {
                    workWeights[7] = this.CalculateRecruitmentWorkWeight(recruitmentMilitaryList);
                }
            }

            if (this.kezhenzai())
            {
                workWeights[8] = Math.Max(workWeights[8], 260.0f);
            }

            if (this.RecentlyAttacked > 0)
            {
                if (this.Endurance < this.EnduranceCeiling)
                {
                    workWeights[5] = 200.0f; // 耐久最高优先级
                }
                // 降低其他内政权重
                workWeights[0] = Math.Min(workWeights[0], 10.0f);
                workWeights[1] = Math.Min(workWeights[1], 10.0f);
                workWeights[2] = Math.Min(workWeights[2], 10.0f);
            }
            
            // 资金不足：优先商业
            if (!this.IsFundEnough)
            {
                workWeights[1] *= 2.0f;
            }
            
            // 粮食不足：优先农业
            if (!this.IsFoodEnough)
            {
                workWeights[0] *= 2.0f;
            }
            
            // [新增逻辑 2] 日常备战逻辑：只要有部队士气未满 100，提升训练权重
            // 不禁止其他工作（如补充资金粮草），仅单纯提高训练优先级
            bool anyNeedMorale = false;
            if (this.Militaries != null)
            {
                anyNeedMorale = this.Militaries.GetList().Cast<Military>()
                    .Any(m => m.BelongedArchitecture == this && m.Morale < 100);
            }

            if (anyNeedMorale)
            {
                // 普通内政满额权重通常是 100
                // 这里设定为 175，既高于普通内政，又给"紧急整军"(200+)留出空间
                if (workWeights[6] < 175.0f)
                {
                    workWeights[6] = 175.0f;
                }
            }

            // [新增逻辑] 紧急整军逻辑：根据要求的动态权重算法
            // 只有当城内编队 >= 3 且最低士气不足 60 时触发
            if (this.Militaries != null)
            {
                var localMilitaryList = this.Militaries.GetList().Cast<Military>()
                    .Where(m => m.BelongedArchitecture == this)
                    .OrderBy(m => m.Morale) // 升序排列，第一个就是最低
                    .ToList();

                if (localMilitaryList.Count >= 3)
                {
                    // 获取士气最低的三支部队
                    var targetTroops = localMilitaryList.Take(3).ToList();
                    
                    float minMorale = targetTroops[0].Morale;               // 最低士气
                    float maxMorale = targetTroops[targetTroops.Count - 1].Morale; // 三支中最高的士气
                    float avgMorale = (float)targetTroops.Average(m => m.Morale);  // 平均士气

                    // 核心判断：只要最低军队士气没有达到60，训练权重都要比较高
                    if (minMorale < 60)
                    {
                        float dynamicBonus = (60 - minMorale) * 4.0f + 
                                             (60 - avgMorale) * 2.0f + 
                                             (60 - maxMorale) * 1.0f;

                        float finalTrainWeight = 200.0f + Math.Max(0, dynamicBonus);
                        workWeights[6] = finalTrainWeight;

                        // "补充工作变为最低"
                        for (int k = 0; k < 6; k++)
                        {
                            workWeights[k] *= 0.01f; // 降为原来的 1%
                        }

                        if (SectionAIHelper.EnableDebugOutput)
                        {
                            System.Diagnostics.Debug.WriteLine($"[AI整军] {this.Name} 触发: 最低{minMorale} 平均{avgMorale:F0} 最高{maxMorale} -> 训练权重:{finalTrainWeight}");
                        }
                    }
                }
            }
            
            this.ApplyOperationalStateWorkBias(workWeights);

            return workWeights;
        }

        /// <summary>
        /// 基于权重分配工作 - 增强版
        /// 整合了武将能力评估和智能决策
        /// </summary>
        /// <param name="p">武将</param>
        /// <param name="workWeights">工作权重数组</param>
        private void AssignWorkByWeights(Person p, float[] workWeights)
        {
            // 计算总权重（基于武将能力）
            float totalWeight = 0f;
            float[] adjustedWeights = new float[workWeights.Length];
            
            for (int i = 0; i < workWeights.Length; i++)
            {
                if (workWeights[i] > 0)
                {
                    ArchitectureWorkKind workKind = (ArchitectureWorkKind)(i + 1);
                    if (!this.CanAssignWorkForAI(p, workKind))
                    {
                        continue;
                    }

                    // 使用现有的能力获取方法
                    float abilityWeight = GetPersonAbilityForWork(p, i);
                    adjustedWeights[i] = workWeights[i] * abilityWeight;
                    totalWeight += adjustedWeights[i];
                }
            }
            
            if (totalWeight == 0)
            {
                if (this.TryAssignMaintenanceWork(p))
                {
                    return;
                }
                p.WorkKind = ArchitectureWorkKind.无;
                return;
            }
            
            // 随机选择工作（基于权重）
            float randomValue = (float)(GameObject.Random(10000) / 10000.0) * totalWeight;
            float currentWeight = 0f;
            
            for (int i = 0; i < adjustedWeights.Length; i++)
            {
                if (adjustedWeights[i] > 0)
                {
                    currentWeight += adjustedWeights[i];
                    if (randomValue <= currentWeight)
                    {
                        if (this.AssignSpecificWork(p, i))
                        {
                            return;
                        }
                    }
                }
            }

            
            // 兜底：如果没有分配到工作，分配第一个可用的
            for (int i = 0; i < adjustedWeights.Length; i++)
            {
                if (adjustedWeights[i] > 0)
                {
                    if (this.AssignSpecificWork(p, i))
                    {
                        return;
                    }
                }
            }
            
            if (this.TryAssignMaintenanceWork(p))
            {
                return;
            }

            p.WorkKind = ArchitectureWorkKind.无;
        }

        /// <summary>
        /// 分配默认工作 (兜底)
        /// </summary>
        private void AssignDefaultWork(Person p)
        {
            if (this.TryAssignMaintenanceWork(p))
            {
                if (SectionAIHelper.EnableDebugOutput)
                {
                    System.Diagnostics.Debug.WriteLine($"[AssignDefaultWork] {this.Name} - {p.Name} 使用默认工作: {p.WorkKind}");
                }
                return;
            }

            if (this.TryAssignGenericIdleWork(p))
            {
                if (SectionAIHelper.EnableDebugOutput)
                {
                    System.Diagnostics.Debug.WriteLine($"[AssignDefaultWork] {this.Name} - {p.Name} 浣跨敤閫氱敤鍏滃簳宸ヤ綔: {p.WorkKind}");
                }
                return;
            }

            if (this.GetAICityOperationalState() == AICityOperationalState.Recovery)
            {
                if (_architectureKind.HasDomination)
                {
                    p.WorkKind = ArchitectureWorkKind.统治;
                }
                else if (_architectureKind.HasMorale)
                {
                    p.WorkKind = ArchitectureWorkKind.民心;
                }
                else if (this.GetTrainingMilitaryList().Count > 0)
                {
                    p.WorkKind = ArchitectureWorkKind.训练;
                }
                else
                {
                    p.WorkKind = ArchitectureWorkKind.无;
                }
            }
            else
            {
                p.WorkKind = ArchitectureWorkKind.无;
            }
            
            if (SectionAIHelper.EnableDebugOutput)
            {
                System.Diagnostics.Debug.WriteLine($"[AssignDefaultWork] {this.Name} - {p.Name} 使用默认工作: {p.WorkKind}");
            }
        }

        private bool TryAssignGenericIdleWork(Person p)
        {
            if (p == null)
            {
                return false;
            }

/*
                p.WorkKind = ArchitectureWorkKind.璁粌;
                return true;
            }

            bool canUsePaidInternal = p.InternalNoFundNeeded ||
                                      this.CountAssignedPaidInternalWorkersForAI() < this.GetPaidInternalWorkSlotsForAI();
            ArchitectureWorkKind bestWorkKind = ArchitectureWorkKind.鏃?;
            int bestAbility = int.MinValue;

            this.ConsiderGenericIdleWork(p, ArchitectureWorkKind.缁熸不, _architectureKind.HasDomination, canUsePaidInternal, ref bestWorkKind, ref bestAbility);
            this.ConsiderGenericIdleWork(p, ArchitectureWorkKind.姘戝績, _architectureKind.HasMorale, canUsePaidInternal, ref bestWorkKind, ref bestAbility);
            this.ConsiderGenericIdleWork(p, ArchitectureWorkKind.鑰愪箙, _architectureKind.HasEndurance, canUsePaidInternal, ref bestWorkKind, ref bestAbility);
            this.ConsiderGenericIdleWork(p, ArchitectureWorkKind.鍐滀笟, _architectureKind.HasAgriculture, canUsePaidInternal, ref bestWorkKind, ref bestAbility);
            this.ConsiderGenericIdleWork(p, ArchitectureWorkKind.鍟嗕笟, _architectureKind.HasCommerce, canUsePaidInternal, ref bestWorkKind, ref bestAbility);
            this.ConsiderGenericIdleWork(p, ArchitectureWorkKind.鎶€鏈? _architectureKind.HasTechnology, canUsePaidInternal, ref bestWorkKind, ref bestAbility);

            if (bestWorkKind == ArchitectureWorkKind.鏃?)
            {
                return false;
            }

            p.WorkKind = bestWorkKind;
            return true;
*/
            if (this.TryAssignRecruitmentWork(p))
            {
                return true;
            }

            if (this.GetTrainingMilitaryList().Count > 0)
            {
                p.WorkKind = (ArchitectureWorkKind)7;
                return true;
            }

            bool canUsePaidInternal = p.InternalNoFundNeeded ||
                                      this.CountAssignedPaidInternalWorkersForAI() < this.GetPaidInternalWorkSlotsForAI();
            ArchitectureWorkKind bestWorkKind = (ArchitectureWorkKind)0;
            int bestAbility = int.MinValue;

            this.ConsiderGenericIdleWork(p, (ArchitectureWorkKind)4, _architectureKind.HasDomination, canUsePaidInternal, ref bestWorkKind, ref bestAbility);
            this.ConsiderGenericIdleWork(p, (ArchitectureWorkKind)5, _architectureKind.HasMorale, canUsePaidInternal, ref bestWorkKind, ref bestAbility);
            this.ConsiderGenericIdleWork(p, (ArchitectureWorkKind)6, _architectureKind.HasEndurance, canUsePaidInternal, ref bestWorkKind, ref bestAbility);
            this.ConsiderGenericIdleWork(p, (ArchitectureWorkKind)1, _architectureKind.HasAgriculture, canUsePaidInternal, ref bestWorkKind, ref bestAbility);
            this.ConsiderGenericIdleWork(p, (ArchitectureWorkKind)2, _architectureKind.HasCommerce, canUsePaidInternal, ref bestWorkKind, ref bestAbility);
            this.ConsiderGenericIdleWork(p, (ArchitectureWorkKind)3, _architectureKind.HasTechnology, canUsePaidInternal, ref bestWorkKind, ref bestAbility);

            if (bestWorkKind == (ArchitectureWorkKind)0)
            {
                return false;
            }

            p.WorkKind = bestWorkKind;
            return true;
        }

        private void ConsiderGenericIdleWork(Person p, ArchitectureWorkKind kind, bool isAvailable, bool canUsePaidInternal, ref ArchitectureWorkKind bestWorkKind, ref int bestAbility)
        {
            if (!isAvailable)
            {
                return;
            }

            if (this.IsFundedInternalWorkForAI(kind) && !canUsePaidInternal)
            {
                return;
            }

            int ability = p.GetWorkAbility(kind);
            if (ability > bestAbility)
            {
                bestAbility = ability;
                bestWorkKind = kind;
            }
        }

        private float CalculateRecruitmentWorkWeight(MilitaryList recruitmentMilitaryList)
        {
            if (recruitmentMilitaryList == null || recruitmentMilitaryList.Count <= 0)
            {
                return 0f;
            }

            float gapRatioSum = 0f;
            for (int i = 0; i < recruitmentMilitaryList.Count; i++)
            {
                Military military = recruitmentMilitaryList[i] as Military;
                if (military == null || military.Kind.MaxScale <= 0)
                {
                    continue;
                }

                gapRatioSum += (float)(military.Kind.MaxScale - military.Quantity) / military.Kind.MaxScale;
            }

            float averageGapRatio = gapRatioSum / recruitmentMilitaryList.Count;
            float weight = 60f + averageGapRatio * 120f + this.CalculateRecruitmentReadinessScoreForAI() * 8f;

            switch (this.GetAICityOperationalState())
            {
                case AICityOperationalState.Balanced:
                    weight = Math.Min(weight, 135f);
                    break;

                case AICityOperationalState.MilitaryBuildUp:
                    weight = Math.Max(weight, 165f);
                    break;

                case AICityOperationalState.WartimeOverdraft:
                    weight = Math.Max(weight, 190f);
                    break;
            }

            return weight;
        }

        private float CalculateRecruitmentPriorityForAI(Military military)
        {
            if (military == null || military.Kind.MaxScale <= 0)
            {
                return 0f;
            }

            float gapRatio = (float)(military.Kind.MaxScale - military.Quantity) / military.Kind.MaxScale;
            float scaleWeight = 10000f / Math.Max(military.Kind.MaxScale, 10000);
            float meritWeight = 1.0f + (military.Merit / 1000f) * 0.01f;
            return gapRatio * scaleWeight * meritWeight;
        }

        private int GetOperationalReserveFundForAI()
        {
            int reserve = this.ExpectedSalary +
                          this.FacilityMaintenanceCost * 10 +
                          this.RoutewayActiveCost * 10 +
                          this.InformationDayCost * 5;
            reserve = Math.Max(this.ExpectedSalary, reserve);

            int reserveCap = Math.Max(this.ExpectedSalary, (int)(this.EnoughFund * 0.55f));
            return Math.Min(reserve, reserveCap);
        }

        private int GetPaidInternalWorkSlotsForAI()
        {
            int surplusFund = this.Fund - this.GetOperationalReserveFundForAI();
            if (surplusFund <= 0)
            {
                return 0;
            }

            return surplusFund / Math.Max(1, this.InternalFundCost);
        }

        private int CountAssignedPaidInternalWorkersForAI()
        {
            int count = 0;
            for (int i = 0; i < this.Persons.Count; i++)
            {
                Person person = this.Persons[i] as Person;
                if (person == null || person.InternalNoFundNeeded)
                {
                    continue;
                }

                if (this.IsFundedInternalWorkForAI(person.WorkKind))
                {
                    count++;
                }
            }

            return count;
        }

        private int GetDynamicWorkDemandCountForAI()
        {
            int demand = 0;
            float limit = this.InternalAffairSaturationThreshold;

            if (_architectureKind.HasAgriculture && this.Agriculture < this.AgricultureCeiling * limit) demand++;
            if (_architectureKind.HasCommerce && this.Commerce < this.CommerceCeiling * limit) demand++;
            if (_architectureKind.HasTechnology && this.Technology < this.TechnologyCeiling * limit) demand++;
            if (_architectureKind.HasDomination && this.Domination < this.DominationCeiling * limit) demand++;
            if (_architectureKind.HasMorale && this.Morale < this.MoraleCeiling * limit) demand++;
            if (_architectureKind.HasEndurance && this.Endurance < this.EnduranceCeiling) demand++;
            if (this.GetTrainingMilitaryList().Count > 0) demand++;
            if (this.CanExecuteAIRecruitment(false) && this.RecruitmentAvail() && this.GetRecruitmentMilitaryList().Count > 0) demand++;
            if (this.ShouldBootstrapMilitaryWorkForAI()) demand++;
            if (this.kezhenzai()) demand++;

            return Math.Max(1, demand);
        }

        private bool HasImmediateMilitaryWorkTargetsForAI()
        {
            if (this.GetTrainingMilitaryList().Count > 0)
            {
                return true;
            }

            return this.CanExecuteAIRecruitment(false) &&
                   this.RecruitmentAvail() &&
                   this.GetRecruitmentMilitaryList().Count > 0;
        }

        private bool ShouldBootstrapMilitaryWorkForAI()
        {
            if (!this.HasPerson() || this.HasImmediateMilitaryWorkTargetsForAI())
            {
                return false;
            }

            if (!_architectureKind.HasPopulation || !this.NewMilitaryAvail() || !this.CanExecuteAIRecruitment(true))
            {
                return false;
            }

            if (this.CountLocalActiveOfficersForAI() < 3)
            {
                return false;
            }

            return this.Fund > Math.Max(this.ExpectedSalary, this.GetOperationalReserveFundForAI());
        }

        private void EnsureMilitaryWorkTargetsForAI()
        {
            if (!this.ShouldBootstrapMilitaryWorkForAI())
            {
                return;
            }

            this.AIRecruitMilitary();
        }

        private bool HasSevereWorkforceShortageForAI()
        {
            return this.CountLocalActiveOfficersForAI() <= Math.Max(2, this.GetDynamicWorkDemandCountForAI());
        }

        private bool HasWorkforceSurplusForAI()
        {
            return this.CountLocalActiveOfficersForAI() >= Math.Max(4, this.GetDynamicWorkDemandCountForAI() * 2);
        }

        private int GetDynamicAbilityFloorForAI(ArchitectureWorkKind kind)
        {
            int floor = kind switch
            {
                ArchitectureWorkKind.农业 => 80,
                ArchitectureWorkKind.商业 => 80,
                ArchitectureWorkKind.技术 => 85,
                ArchitectureWorkKind.统治 => 65,
                ArchitectureWorkKind.民心 => 65,
                ArchitectureWorkKind.耐久 => 60,
                ArchitectureWorkKind.补充 => 70,
                ArchitectureWorkKind.赈灾 => 55,
                _ => 0
            };

            if (this.HasSevereWorkforceShortageForAI())
            {
                floor -= 30;
            }
            else if (this.HasWorkforceSurplusForAI())
            {
                floor += 10;
            }

            if (this.IsFundedInternalWorkForAI(kind) && this.GetPaidInternalWorkSlotsForAI() * 2 < Math.Max(1, this.CountLocalActiveOfficersForAI()))
            {
                floor += 10;
            }

            return Math.Max(0, floor);
        }

        private bool IsFundedInternalWorkForAI(ArchitectureWorkKind kind)
        {
            return kind == ArchitectureWorkKind.农业 ||
                   kind == ArchitectureWorkKind.商业 ||
                   kind == ArchitectureWorkKind.技术 ||
                   kind == ArchitectureWorkKind.统治 ||
                   kind == ArchitectureWorkKind.民心 ||
                   kind == ArchitectureWorkKind.耐久 ||
                   kind == ArchitectureWorkKind.赈灾;
        }

        private bool CanAssignWorkForAI(Person p, ArchitectureWorkKind kind)
        {
            if (p == null || kind == ArchitectureWorkKind.无)
            {
                return false;
            }

            if (kind == ArchitectureWorkKind.训练)
            {
                return this.GetTrainingMilitaryList().Count > 0;
            }

            if (kind == ArchitectureWorkKind.补充)
            {
                if (!this.CanExecuteAIRecruitment(false) || !this.RecruitmentAvail() || this.GetRecruitmentMilitaryList().Count <= 0)
                {
                    return false;
                }

                return this.GetAICityOperationalState() == AICityOperationalState.WartimeOverdraft ||
                       this.Fund >= this.GetOperationalReserveFundForAI();
            }

            if (kind == ArchitectureWorkKind.赈灾 && !this.kezhenzai())
            {
                return false;
            }

            if (!this.IsFundedInternalWorkForAI(kind))
            {
                return true;
            }

            if (p.InternalNoFundNeeded)
            {
                return true;
            }

            if (this.CountAssignedPaidInternalWorkersForAI() >= this.GetPaidInternalWorkSlotsForAI())
            {
                return false;
            }

            int ability = p.GetWorkAbility(kind);
            int preferredFloor = this.GetDynamicAbilityFloorForAI(kind);
            if (ability >= preferredFloor)
            {
                return true;
            }

            return this.CanFallbackToLowSkillInternalWorkForAI(kind, ability, preferredFloor);
        }

        private bool CanFallbackToLowSkillInternalWorkForAI(ArchitectureWorkKind kind, int ability, int preferredFloor)
        {
            if (!this.IsFundedInternalWorkForAI(kind) || this.HasImmediateMilitaryWorkTargetsForAI())
            {
                return false;
            }

            int minimumFloor = kind switch
            {
                ArchitectureWorkKind.农业 => 45,
                ArchitectureWorkKind.商业 => 45,
                ArchitectureWorkKind.技术 => 50,
                ArchitectureWorkKind.赈灾 => 40,
                _ => 30
            };

            int fallbackFloor = kind switch
            {
                ArchitectureWorkKind.统治 => preferredFloor - 35,
                ArchitectureWorkKind.民心 => preferredFloor - 35,
                ArchitectureWorkKind.耐久 => preferredFloor - 35,
                ArchitectureWorkKind.赈灾 => preferredFloor - 20,
                _ => preferredFloor - 10
            };

            if (this.HasSevereWorkforceShortageForAI())
            {
                fallbackFloor -= 10;
            }

            return ability >= Math.Max(minimumFloor, fallbackFloor);
        }

        private bool TryAssignRecruitmentWork(Person p)
        {
            if (p == null || !this.CanAssignWorkForAI(p, ArchitectureWorkKind.补充))
            {
                return false;
            }

            MilitaryList recruitmentMilitaryList = this.GetRecruitmentMilitaryList();
            Military bestMilitary = null;
            float bestPriority = float.MinValue;

            for (int i = 0; i < recruitmentMilitaryList.Count; i++)
            {
                Military military = recruitmentMilitaryList[i] as Military;
                if (military == null || military.RecruitmentPerson != null)
                {
                    continue;
                }

                float priority = this.CalculateRecruitmentPriorityForAI(military);
                if (priority > bestPriority)
                {
                    bestPriority = priority;
                    bestMilitary = military;
                }
            }

            if (bestMilitary == null)
            {
                return false;
            }

            p.RecruitMilitary(bestMilitary);
            return true;
        }

        private bool TryAssignMaintenanceWork(Person p)
        {
            if (p == null)
            {
                return false;
            }

            if (this.kezhenzai() && this.CanAssignWorkForAI(p, ArchitectureWorkKind.赈灾))
            {
                p.WorkKind = ArchitectureWorkKind.赈灾;
                return true;
            }

            if (this.TryAssignRecruitmentWork(p))
            {
                return true;
            }

            ArchitectureWorkKind bestWorkKind = ArchitectureWorkKind.无;
            float bestScore = float.MinValue;

            this.ConsiderMaintenanceWork(p, ArchitectureWorkKind.统治, _architectureKind.HasDomination ? 1.30f : 0f, ref bestWorkKind, ref bestScore);
            this.ConsiderMaintenanceWork(p, ArchitectureWorkKind.民心, _architectureKind.HasMorale ? 1.25f : 0f, ref bestWorkKind, ref bestScore);
            this.ConsiderMaintenanceWork(p, ArchitectureWorkKind.耐久, _architectureKind.HasEndurance ? (this.RecentlyAttacked > 0 ? 1.30f : 1.10f) : 0f, ref bestWorkKind, ref bestScore);
            this.ConsiderMaintenanceWork(p, ArchitectureWorkKind.农业, _architectureKind.HasAgriculture ? 1.00f : 0f, ref bestWorkKind, ref bestScore);
            this.ConsiderMaintenanceWork(p, ArchitectureWorkKind.商业, _architectureKind.HasCommerce ? 1.00f : 0f, ref bestWorkKind, ref bestScore);
            this.ConsiderMaintenanceWork(p, ArchitectureWorkKind.技术, _architectureKind.HasTechnology ? 0.95f : 0f, ref bestWorkKind, ref bestScore);

            if (this.GetTrainingMilitaryList().Count > 0)
            {
                this.ConsiderMaintenanceWork(p, ArchitectureWorkKind.训练, 1.20f, ref bestWorkKind, ref bestScore);
            }

            if (bestWorkKind == ArchitectureWorkKind.无)
            {
                return false;
            }

            p.WorkKind = bestWorkKind;
            return true;
        }

        private void ConsiderMaintenanceWork(Person p, ArchitectureWorkKind kind, float bias, ref ArchitectureWorkKind bestWorkKind, ref float bestScore)
        {
            if (bias <= 0f || !this.CanAssignWorkForAI(p, kind))
            {
                return;
            }

            float score = this.GetPersonWorkEfficiency(p, kind) * bias;
            if (this.HasWorkforceSurplusForAI())
            {
                score *= 0.75f + Math.Min(1.25f, p.GetWorkAbility(kind) / 100.0f);
            }
            else if (this.HasSevereWorkforceShortageForAI())
            {
                score *= 1.10f;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestWorkKind = kind;
            }
        }
    }
}

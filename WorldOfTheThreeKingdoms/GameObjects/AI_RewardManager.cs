using System;
using System.Collections.Generic;
using GameManager;
using GameObjects.PersonDetail;

namespace GameObjects
{
    /// <summary>
    /// AI褒赏管理器：负责AI势力的自动褒赏逻辑
    /// </summary>
    public class AI_RewardManager
    {
        // 用于缓存 AI 打分结果的结构体，避免 GC 分配
        private struct RewardCandidate : IComparable<RewardCandidate>
        {
            public Person Target;
            public int Score;

            public int CompareTo(RewardCandidate other)
            {
                // 降序排序：分数高的排前面
                return other.Score.CompareTo(this.Score);
            }
        }

        // 预分配的列表，每月复用，实现零分配
        private readonly List<RewardCandidate> _candidatePool = new(500);

        /// <summary>
        /// AI 势力执行每月的褒赏指令
        /// </summary>
        public void ExecuteMonthlyRewards(Faction aiFaction)
        {
            // 只有AI势力才执行
            if (Session.Current.Scenario.IsPlayer(aiFaction)) return;

            // 1. 确定预算：AI 不能把钱花光，比如最多拿出国库的 30% 来发奖金
            int budget = aiFaction.Fund * 30 / 100;
            int costPerPerson = Session.Parameters.RewardPersonCost;
            
            if (budget < costPerPerson) return; // 穷得连一个人都赏不起，直接退出

            _candidatePool.Clear();

            // 2. 遍历势力下的武将，进行海选打分
            for (int i = 0; i < aiFaction.Persons.Count; i++)
            {
                // PersonList 保证类型安全，直接转换
                Person person = (Person)aiFaction.Persons[i];

                // 排除：已经赏赐过的、忠诚度 >= 100 的、不在正常状态的
                if (person.RewardFinished || person.Loyalty >= 100 || person.Status != PersonStatus.Normal)
                {
                    continue;
                }

                int score = CalculateRewardScore(person);
                if (score > 0)
                {
                    _candidatePool.Add(new RewardCandidate { Target = person, Score = score });
                }
            }

            // 3. 原地排序（极其高效）
            _candidatePool.Sort();

            // 4. 按优先级发钱，直到预算耗尽
            for (int i = 0; i < _candidatePool.Count; i++)
            {
                if (budget < costPerPerson) break; // 预算花光了

                RewardCandidate candidate = _candidatePool[i];
                
                // 扣钱并执行赏赐
                if (candidate.Target.BelongedArchitecture != null)
                {
                    candidate.Target.BelongedArchitecture.DecreaseFund(costPerPerson);
                }
                else if (aiFaction.Capital != null)
                {
                    aiFaction.Capital.DecreaseFund(costPerPerson);
                }
                budget -= costPerPerson;
                candidate.Target.ReceiveReward(costPerPerson);
            }
        }

        /// <summary>
        /// 计算 AI 眼中的"褒赏投资回报率 (ROI)"得分
        /// </summary>
        private int CalculateRewardScore(Person person)
        {
            int loyalty = person.Loyalty;
            int statValue = person.Command + person.Intelligence + person.Strength; // 核心三维总和

            // 基础价值得分 (能力越强，底分越高)
            int score = statValue;

            // --- 维度 1：忠诚度分诊乘数 (整数运算) ---
            int urgencyMultiplier = loyalty switch
            {
                >= 90 => 1,  // 锦上添花区：优先级最低（90-99）
                >= 75 => 15, // 黄金救援区：极高优先级。只要稍微推一把就能解除能力打折，ROI 最高
                >= 50 => 10, // 危险动荡区：正常救援
                _     => 3   // 无底洞区 (<50)：基本放弃。除非此人核心三维突破 280 (如吕布)，才勉强和普通武将平起平坐
            };

            // --- 维度 2：义理（对金钱敏感度）乘数 ---
            // 义理越低，金钱效果越好，AI 越倾向于给钱
            int greedMultiplier = (int)person.PersonalLoyalty switch
            {
                <= 0 => 12, // 见钱眼开：给钱就狂涨忠诚，AI 狂喜
                1    => 10,
                2    => 8,
                3    => 6,
                >= 4 => 4   // 视金钱如粪土：给钱效果差，AI 觉得划不来
            };

            // 最终得分 = 能力基础 * 紧急程度 * 贪财程度
            return score * urgencyMultiplier * greedMultiplier;
        }

        /// <summary>
        /// 玩家势力的建筑自动褒赏（委任褒奖）- 使用AI智能打分逻辑
        /// </summary>
        public void ExecuteArchitectureAutoReward(Architecture architecture)
        {
            // 检查是否开启自动褒赏
            if (!architecture.AutoRewarding) return;

            // 检查是否是玩家势力
            if (!Session.Current.Scenario.IsPlayer(architecture.BelongedFaction)) return;

            // 检查资金是否充裕（至少保留3个月的开销）
            int monthlyCost = EstimateMonthlyCost(architecture.BelongedFaction);
            int reserveFund = monthlyCost * 3;
            
            if (architecture.BelongedFaction.Fund < reserveFund) return;

            _candidatePool.Clear();

            // 使用AI智能打分逻辑筛选武将
            for (int i = 0; i < architecture.Persons.Count; i++)
            {
                // PersonList 保证类型安全，直接转换
                Person person = (Person)architecture.Persons[i];

                // 排除：已经赏赐过的、忠诚度 >= 100 的、不在正常状态的、君主
                if (person.RewardFinished || 
                    person.LoyaltyDisplay >= 100 || 
                    person.Status != PersonStatus.Normal ||
                    person == architecture.BelongedFaction.Leader)
                {
                    continue;
                }

                int score = CalculateRewardScore(person);
                if (score > 0)
                {
                    _candidatePool.Add(new RewardCandidate { Target = person, Score = score });
                }
            }

            // 按AI打分排序
            _candidatePool.Sort();

            // 褒赏前N个武将（根据资金情况）
            int rewardCost = Session.Parameters.RewardPersonCost;
            int maxRewardCount = Math.Min(
                _candidatePool.Count,
                (architecture.BelongedFaction.Fund - reserveFund) / rewardCost
            );

            for (int i = 0; i < maxRewardCount; i++)
            {
                RewardCandidate candidate = _candidatePool[i];
                architecture.DecreaseFund(rewardCost);
                candidate.Target.ReceiveReward(rewardCost);
            }
        }

        /// <summary>
        /// 估算势力的月度开销
        /// </summary>
        private int EstimateMonthlyCost(Faction faction)
        {
            // 简化估算：每个武将平均月薪 + 每个建筑维护费
            int personCost = faction.Persons.Count * 50; // 假设平均月薪50
            int architectureCost = faction.Architectures.Count * 100; // 假设平均维护费100
            return personCost + architectureCost;
        }

        /// <summary>
        /// 为UI获取按AI打分排序的褒赏候选人列表
        /// </summary>
        public PersonList GetRewardCandidatesForUI(Architecture architecture)
        {
            _candidatePool.Clear();

            // 🔍 调试信息：记录筛选过程
            int totalCount = architecture.Persons.Count;
            int excludedByRewardFinished = 0;
            int excludedByLoyalty = 0;
            int excludedByStatus = 0;
            int excludedByLeader = 0;
            int excludedByScore = 0;

            // 使用AI智能打分逻辑筛选武将
            for (int i = 0; i < architecture.Persons.Count; i++)
            {
                // PersonList 保证类型安全，直接转换
                Person person = (Person)architecture.Persons[i];

                // 排除：已经赏赐过的
                if (person.RewardFinished)
                {
                    excludedByRewardFinished++;
                    continue;
                }

                // 排除：显示忠诚度 >= 100 的
                if (person.LoyaltyDisplay >= 100)
                {
                    excludedByLoyalty++;
                    continue;
                }

                // 排除：不在正常状态的
                if (person.Status != PersonStatus.Normal)
                {
                    excludedByStatus++;
                    System.Diagnostics.Debug.WriteLine($"[褒赏筛选] 武将 {person.Name} 被排除：状态={person.Status}");
                    continue;
                }

                // 排除：君主
                if (person == architecture.BelongedFaction.Leader)
                {
                    excludedByLeader++;
                    continue;
                }

                int score = CalculateRewardScore(person);
                if (score > 0)
                {
                    _candidatePool.Add(new RewardCandidate { Target = person, Score = score });
                }
                else
                {
                    excludedByScore++;
                    System.Diagnostics.Debug.WriteLine($"[褒赏筛选] 武将 {person.Name} 被排除：得分={score}, 忠诚度={person.Loyalty}, 显示忠诚度={person.LoyaltyDisplay}");
                }
            }

            // 🔍 输出调试统计
            System.Diagnostics.Debug.WriteLine($"[褒赏筛选统计] 总武将数={totalCount}, 候选人数={_candidatePool.Count}");
            System.Diagnostics.Debug.WriteLine($"  排除原因: 已褒赏={excludedByRewardFinished}, 忠诚度>=100={excludedByLoyalty}, 状态异常={excludedByStatus}, 君主={excludedByLeader}, 得分<=0={excludedByScore}");

            // 按AI打分排序（分数高的在前）
            _candidatePool.Sort();

            // 转换为PersonList
            PersonList result = [];
            for (int i = 0; i < _candidatePool.Count; i++)
            {
                result.Add(_candidatePool[i].Target);
            }

            return result;
        }
    }
}

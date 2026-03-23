using GameObjects;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.DataIntegrity
{
    /// <summary>
    /// Troop.BelongedLegion关系重建策略
    /// </summary>
    public class TroopBelongedLegionRebuildStrategy : RelationshipRebuildStrategy
    {
        public override string StrategyName => "TroopBelongedLegionRebuild";
        public override string Description => "重建Troop.BelongedLegion关系";

        public override bool AppliesTo(GameObject gameObject)
        {
            return gameObject is Troop;
        }

        public override async Task<RelationshipRepairResult> RebuildAsync(GameObject gameObject)
        {
            if (!(gameObject is Troop troop))
            {
                return CreateResult(gameObject, false, "对象不是Troop类型");
            }

            // 模拟异步数据重建过程
            await Task.Delay(5).ConfigureAwait(false);

            var result = CreateResult(troop, true);

            try
            {
                // 情况1: BelongedLegion不为null但军团的部队列表中没有该部队
                if (troop.BelongedLegion != null && 
                    troop.BelongedLegion.Troops != null &&
                    !troop.BelongedLegion.Troops.HasGameObject(troop.ID))
                {
                    Debug.WriteLine($"[TroopBelongedLegionRebuildStrategy] 将Troop[{troop.ID}] 添加到军团部队列表");
                    
                    troop.BelongedLegion.Troops.Add(troop);
                    result.AddRepairedRelationship($"将部队添加到军团 {troop.BelongedLegion.Name} 的部队列表");
                }

                // 情况2: BelongedLegion不为null但军团属于不同的势力
                if (troop.BelongedLegion != null && 
                    troop.BelongedFaction != null &&
                    troop.BelongedLegion.BelongedFaction != troop.BelongedFaction)
                {
                    Debug.WriteLine($"[TroopBelongedLegionRebuildStrategy] Troop[{troop.ID}] 的势力与军团势力不匹配");
                    
                    // 这种情况需要谨慎处理，可能需要：
                    // 1. 将部队从当前军团移除
                    // 2. 修正部队的势力归属
                    // 3. 或者修正军团的势力归属
                    
                    // 暂时采用将部队从军团中移除的策略
                    if (troop.BelongedLegion.Troops != null && troop.BelongedLegion.Troops.HasGameObject(troop.ID))
                    {
                        troop.BelongedLegion.Troops.Remove(troop);
                    }
                    troop.BelongedLegion = null;
                    result.AddRepairedRelationship($"移除部队与不匹配军团的关系");
                }

                // 情况3: 验证双向关系的一致性
                if (troop.BelongedLegion != null && 
                    troop.BelongedLegion.Troops != null &&
                    troop.BelongedLegion.Troops.HasGameObject(troop.ID) &&
                    troop.BelongedLegion.BelongedFaction == troop.BelongedFaction)
                {
                    // 关系正常，无需修复
                    Debug.WriteLine($"[TroopBelongedLegionRebuildStrategy] Troop[{troop.ID}] 与军团 {troop.BelongedLegion.Name} 的关系正常");
                }
            }
            catch (System.Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"重建异常: {ex.Message}";
                Debug.WriteLine($"[TroopBelongedLegionRebuildStrategy] 重建异常: {ex.Message}");
            }

            return result;
        }

        public override async Task<bool> ValidateAsync(GameObject gameObject)
        {
            if (!(gameObject is Troop troop))
            {
                return true; // 不适用的对象认为验证通过
            }

            // 模拟异步验证过程
            await Task.Delay(2).ConfigureAwait(false);

            // 如果部队不属于任何军团，这是正常的
            if (troop.BelongedLegion == null)
                return true;

            // 验证部队在军团的部队列表中
            if (troop.BelongedLegion.Troops != null &&
                !troop.BelongedLegion.Troops.HasGameObject(troop.ID))
                return false;

            // 验证部队和军团属于同一势力
            if (troop.BelongedFaction != null &&
                troop.BelongedLegion.BelongedFaction != troop.BelongedFaction)
                return false;

            return true;
        }
    }
}
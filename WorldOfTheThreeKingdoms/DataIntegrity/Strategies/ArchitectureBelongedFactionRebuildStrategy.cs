using GameObjects;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.DataIntegrity
{
    /// <summary>
    /// Architecture.BelongedFaction关系重建策略
    /// </summary>
    public class ArchitectureBelongedFactionRebuildStrategy : RelationshipRebuildStrategy
    {
        public override string StrategyName => "ArchitectureBelongedFactionRebuild";
        public override string Description => "重建Architecture.BelongedFaction关系";

        public override bool AppliesTo(GameObject gameObject)
        {
            return gameObject is Architecture;
        }

        public override async Task<RelationshipRepairResult> RebuildAsync(GameObject gameObject)
        {
            if (!(gameObject is Architecture architecture))
            {
                return CreateResult(gameObject, false, "对象不是Architecture类型");
            }

            // 模拟异步建筑势力关系重建过程
            await Task.Delay(7).ConfigureAwait(false);

            var result = CreateResult(architecture, true);

            try
            {
                // 情况1: BelongedFaction为null
                if (architecture.BelongedFaction == null)
                {
                    Debug.WriteLine($"[ArchitectureBelongedFactionRebuildStrategy] Architecture[{architecture.ID}] {architecture.Name} 的BelongedFaction为null");
                    
                    // 这种情况比较复杂，可能需要根据建筑的位置、历史数据或其他信息来确定所属势力
                    // 暂时只记录问题，不自动修复
                    result.AddFailedRelationship("BelongedFaction为null，需要手动分配所属势力");
                }

                // 情况2: BelongedFaction存在但建筑不在势力的建筑列表中
                if (architecture.BelongedFaction != null && 
                    architecture.BelongedFaction.Architectures != null &&
                    !architecture.BelongedFaction.Architectures.HasGameObject(architecture.ID))
                {
                    Debug.WriteLine($"[ArchitectureBelongedFactionRebuildStrategy] 将Architecture[{architecture.ID}] {architecture.Name} 添加到势力建筑列表");
                    
                    architecture.BelongedFaction.Architectures.Add(architecture);
                    result.AddRepairedRelationship($"将建筑添加到势力 {architecture.BelongedFaction.Name} 的建筑列表");
                }

                // 情况3: 验证双向关系的一致性
                if (architecture.BelongedFaction != null && 
                    architecture.BelongedFaction.Architectures != null &&
                    architecture.BelongedFaction.Architectures.HasGameObject(architecture.ID))
                {
                    // 关系正常，无需修复
                    Debug.WriteLine($"[ArchitectureBelongedFactionRebuildStrategy] Architecture[{architecture.ID}] {architecture.Name} 与势力 {architecture.BelongedFaction.Name} 的关系正常");
                }
            }
            catch (System.Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"重建异常: {ex.Message}";
                Debug.WriteLine($"[ArchitectureBelongedFactionRebuildStrategy] 重建异常: {ex.Message}");
            }

            return result;
        }

        public override async Task<bool> ValidateAsync(GameObject gameObject)
        {
            if (!(gameObject is Architecture architecture))
            {
                return true; // 不适用的对象认为验证通过
            }

            // 模拟异步验证过程
            await Task.Delay(4).ConfigureAwait(false);

            // 验证BelongedFaction不为null
            if (architecture.BelongedFaction == null)
                return false;

            // 验证建筑在势力的建筑列表中
            if (architecture.BelongedFaction.Architectures != null &&
                !architecture.BelongedFaction.Architectures.HasGameObject(architecture.ID))
                return false;

            return true;
        }
    }
}
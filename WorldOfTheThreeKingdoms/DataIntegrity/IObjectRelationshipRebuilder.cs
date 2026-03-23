using GameObjects;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.DataIntegrity
{
    /// <summary>
    /// 对象关系重建器接口
    /// 用于修复AOT环境下断裂的对象关系
    /// </summary>
    public interface IObjectRelationshipRebuilder
    {
        /// <summary>
        /// 重建游戏对象的关系
        /// </summary>
        /// <param name="gameObject">要重建关系的游戏对象</param>
        /// <returns>重建是否成功</returns>
        Task<bool> RebuildRelationshipsAsync(GameObject gameObject);

        /// <summary>
        /// 验证游戏对象的关系完整性
        /// </summary>
        /// <param name="gameObject">要验证的游戏对象</param>
        /// <returns>关系是否完整</returns>
        Task<bool> ValidateRelationshipsAsync(GameObject gameObject);

        /// <summary>
        /// 修复断裂的关系
        /// </summary>
        /// <param name="gameObject">要修复的游戏对象</param>
        /// <returns>修复结果报告</returns>
        Task<RelationshipRepairResult> RepairBrokenRelationshipsAsync(GameObject gameObject);

        /// <summary>
        /// 重建所有游戏对象的关系
        /// </summary>
        /// <returns>重建结果报告</returns>
        Task<RelationshipRebuildReport> RebuildAllRelationshipsAsync();

        /// <summary>
        /// 添加关系重建策略
        /// </summary>
        /// <param name="strategy">重建策略</param>
        void AddRebuildStrategy(IRelationshipRebuildStrategy strategy);

        /// <summary>
        /// 移除关系重建策略
        /// </summary>
        /// <param name="strategyName">策略名称</param>
        void RemoveRebuildStrategy(string strategyName);
    }
}
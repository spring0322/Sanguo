using GameObjects;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.DataIntegrity
{
    /// <summary>
    /// 关系重建策略接口
    /// </summary>
    public interface IRelationshipRebuildStrategy
    {
        /// <summary>
        /// 策略名称
        /// </summary>
        string StrategyName { get; }

        /// <summary>
        /// 策略描述
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 是否启用此策略
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// 检查此策略是否适用于指定的游戏对象
        /// </summary>
        /// <param name="gameObject">游戏对象</param>
        /// <returns>是否适用</returns>
        bool AppliesTo(GameObject gameObject);

        /// <summary>
        /// 重建对象关系
        /// </summary>
        /// <param name="gameObject">要重建关系的游戏对象</param>
        /// <returns>重建结果</returns>
        Task<RelationshipRepairResult> RebuildAsync(GameObject gameObject);

        /// <summary>
        /// 验证对象关系
        /// </summary>
        /// <param name="gameObject">要验证的游戏对象</param>
        /// <returns>验证是否通过</returns>
        Task<bool> ValidateAsync(GameObject gameObject);
    }
}
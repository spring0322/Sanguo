using GameObjects;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.DataIntegrity
{
    /// <summary>
    /// 关系重建策略抽象基类
    /// </summary>
    public abstract class RelationshipRebuildStrategy : IRelationshipRebuildStrategy
    {
        public abstract string StrategyName { get; }
        public abstract string Description { get; }
        public bool IsEnabled { get; set; } = true;

        public abstract bool AppliesTo(GameObject gameObject);
        public abstract Task<RelationshipRepairResult> RebuildAsync(GameObject gameObject);
        public abstract Task<bool> ValidateAsync(GameObject gameObject);

        /// <summary>
        /// 创建修复结果的辅助方法
        /// </summary>
        /// <param name="gameObject">游戏对象</param>
        /// <param name="success">是否成功</param>
        /// <param name="errorMessage">错误消息</param>
        /// <returns>修复结果</returns>
        protected RelationshipRepairResult CreateResult(GameObject gameObject, bool success, string errorMessage = "")
        {
            return new RelationshipRepairResult
            {
                Success = success,
                ObjectType = gameObject?.GetType().Name ?? "Unknown",
                ObjectId = gameObject?.ID ?? -1,
                ErrorMessage = errorMessage
            };
        }
    }
}
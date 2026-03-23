using GameObjects;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.DataIntegrity
{
    /// <summary>
    /// 数据完整性检查规则抽象基类
    /// </summary>
    public abstract class IntegrityRule : IIntegrityRule
    {
        public abstract string RuleName { get; }
        public abstract string Description { get; }
        public bool IsEnabled { get; set; } = true;

        public abstract Task<ValidationResult> ValidateAsync(GameObject gameObject);

        public virtual bool AppliesTo(GameObject gameObject)
        {
            // 默认实现：检查对象是否为null
            return gameObject != null;
        }

        /// <summary>
        /// 创建验证失败结果的辅助方法
        /// </summary>
        /// <param name="gameObject">游戏对象</param>
        /// <param name="propertyName">属性名</param>
        /// <param name="issueType">问题类型</param>
        /// <param name="message">错误消息</param>
        /// <param name="suggestedFix">建议修复方案</param>
        /// <param name="severity">严重程度</param>
        /// <returns>验证结果</returns>
        protected ValidationResult CreateFailedResult(GameObject gameObject, string propertyName, 
            string issueType, string message, string suggestedFix = "", IssueSeverity severity = IssueSeverity.Warning)
        {
            return ValidationResult.Failed(
                gameObject?.GetType().Name ?? "Unknown",
                gameObject?.ID ?? -1,
                propertyName,
                issueType,
                message,
                suggestedFix,
                severity
            );
        }
    }
}
using GameObjects;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.DataIntegrity
{
    /// <summary>
    /// 数据完整性检查规则接口
    /// </summary>
    public interface IIntegrityRule
    {
        /// <summary>
        /// 规则名称
        /// </summary>
        string RuleName { get; }

        /// <summary>
        /// 规则描述
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 是否启用此规则
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// 验证游戏对象
        /// </summary>
        /// <param name="gameObject">要验证的游戏对象</param>
        /// <returns>验证结果</returns>
        Task<ValidationResult> ValidateAsync(GameObject gameObject);

        /// <summary>
        /// 检查此规则是否适用于指定的游戏对象类型
        /// </summary>
        /// <param name="gameObject">游戏对象</param>
        /// <returns>是否适用</returns>
        bool AppliesTo(GameObject gameObject);
    }
}
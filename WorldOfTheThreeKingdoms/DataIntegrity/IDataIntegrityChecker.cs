using GameObjects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.DataIntegrity
{
    /// <summary>
    /// 数据完整性检查器接口
    /// 用于检测和诊断AOT环境下的数据转换问题
    /// </summary>
    public interface IDataIntegrityChecker
    {
        /// <summary>
        /// 检查单个游戏对象的数据完整性
        /// </summary>
        /// <param name="gameObject">要检查的游戏对象</param>
        /// <returns>完整性报告</returns>
        Task<IntegrityReport> CheckAsync(GameObject gameObject);

        /// <summary>
        /// 检查所有游戏对象的数据完整性
        /// </summary>
        /// <returns>完整性报告</returns>
        Task<IntegrityReport> CheckAllAsync();

        /// <summary>
        /// 验证游戏对象的关系完整性
        /// </summary>
        /// <param name="gameObject">要验证的游戏对象</param>
        /// <returns>验证结果</returns>
        Task<bool> ValidateRelationshipsAsync(GameObject gameObject);

        /// <summary>
        /// 添加完整性检查规则
        /// </summary>
        /// <param name="rule">检查规则</param>
        void AddRule(IIntegrityRule rule);

        /// <summary>
        /// 移除完整性检查规则
        /// </summary>
        /// <param name="ruleName">规则名称</param>
        void RemoveRule(string ruleName);

        /// <summary>
        /// 获取所有已注册的规则
        /// </summary>
        /// <returns>规则列表</returns>
        IReadOnlyList<IIntegrityRule> GetRules();
    }
}
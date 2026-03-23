using System;
using System.Collections.Generic;

namespace WorldOfTheThreeKingdoms.DataIntegrity
{
    /// <summary>
    /// 关系修复结果
    /// </summary>
    public class RelationshipRepairResult
    {
        public bool Success { get; set; }
        public string ObjectType { get; set; } = string.Empty;
        public int ObjectId { get; set; }
        public List<string> RepairedRelationships { get; set; } = new List<string>();
        public List<string> FailedRelationships { get; set; } = new List<string>();
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime RepairTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 添加成功修复的关系
        /// </summary>
        /// <param name="relationshipName">关系名称</param>
        public void AddRepairedRelationship(string relationshipName)
        {
            if (!string.IsNullOrEmpty(relationshipName))
            {
                RepairedRelationships.Add(relationshipName);
            }
        }

        /// <summary>
        /// 添加修复失败的关系
        /// </summary>
        /// <param name="relationshipName">关系名称</param>
        public void AddFailedRelationship(string relationshipName)
        {
            if (!string.IsNullOrEmpty(relationshipName))
            {
                FailedRelationships.Add(relationshipName);
            }
        }

        /// <summary>
        /// 获取修复摘要
        /// </summary>
        /// <returns>修复摘要字符串</returns>
        public string GetSummary()
        {
            return $"{ObjectType}[{ObjectId}]: 成功修复{RepairedRelationships.Count}个关系，失败{FailedRelationships.Count}个关系";
        }
    }

    /// <summary>
    /// 关系重建报告
    /// </summary>
    public class RelationshipRebuildReport
    {
        public List<RelationshipRepairResult> Results { get; set; } = new List<RelationshipRepairResult>();
        public DateTime RebuildTime { get; set; } = DateTime.Now;
        public int TotalObjectsProcessed { get; set; }
        public int SuccessfulRebuilds { get; set; }
        public int FailedRebuilds { get; set; }

        /// <summary>
        /// 添加修复结果
        /// </summary>
        /// <param name="result">修复结果</param>
        public void AddResult(RelationshipRepairResult result)
        {
            Results.Add(result);
            TotalObjectsProcessed++;
            
            if (result.Success)
            {
                SuccessfulRebuilds++;
            }
            else
            {
                FailedRebuilds++;
            }
        }

        /// <summary>
        /// 获取报告摘要
        /// </summary>
        /// <returns>报告摘要字符串</returns>
        public string GetSummary()
        {
            var summary = $"关系重建报告 - {RebuildTime:yyyy-MM-dd HH:mm:ss}\n";
            summary += $"处理对象总数: {TotalObjectsProcessed}\n";
            summary += $"成功重建: {SuccessfulRebuilds}\n";
            summary += $"失败重建: {FailedRebuilds}\n";
            
            int totalRepairedRelationships = 0;
            int totalFailedRelationships = 0;
            
            foreach (var result in Results)
            {
                totalRepairedRelationships += result.RepairedRelationships.Count;
                totalFailedRelationships += result.FailedRelationships.Count;
            }
            
            summary += $"总修复关系数: {totalRepairedRelationships}\n";
            summary += $"总失败关系数: {totalFailedRelationships}\n";
            
            return summary;
        }
    }
}
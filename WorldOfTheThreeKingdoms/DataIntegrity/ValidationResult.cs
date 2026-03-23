namespace WorldOfTheThreeKingdoms.DataIntegrity
{
    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string SuggestedFix { get; set; } = string.Empty;
        public string ObjectType { get; set; } = string.Empty;
        public int ObjectId { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public string IssueType { get; set; } = string.Empty;
        public IssueSeverity Severity { get; set; } = IssueSeverity.Warning;

        /// <summary>
        /// 创建成功的验证结果
        /// </summary>
        /// <returns>成功的验证结果</returns>
        public static ValidationResult Success()
        {
            return new ValidationResult { IsValid = true };
        }

        /// <summary>
        /// 创建失败的验证结果
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="suggestedFix">建议修复方案</param>
        /// <param name="severity">严重程度</param>
        /// <returns>失败的验证结果</returns>
        public static ValidationResult Failed(string message, string suggestedFix = "", IssueSeverity severity = IssueSeverity.Warning)
        {
            return new ValidationResult
            {
                IsValid = false,
                Message = message,
                SuggestedFix = suggestedFix,
                Severity = severity
            };
        }

        /// <summary>
        /// 创建失败的验证结果（包含详细信息）
        /// </summary>
        /// <param name="objectType">对象类型</param>
        /// <param name="objectId">对象ID</param>
        /// <param name="propertyName">属性名</param>
        /// <param name="issueType">问题类型</param>
        /// <param name="message">错误消息</param>
        /// <param name="suggestedFix">建议修复方案</param>
        /// <param name="severity">严重程度</param>
        /// <returns>失败的验证结果</returns>
        public static ValidationResult Failed(string objectType, int objectId, string propertyName, 
            string issueType, string message, string suggestedFix = "", IssueSeverity severity = IssueSeverity.Warning)
        {
            return new ValidationResult
            {
                IsValid = false,
                ObjectType = objectType,
                ObjectId = objectId,
                PropertyName = propertyName,
                IssueType = issueType,
                Message = message,
                SuggestedFix = suggestedFix,
                Severity = severity
            };
        }
    }
}
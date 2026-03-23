using System.Text.Json;
using System.Text.Json.Serialization;
#if !DEBUG
using WorldOfTheThreeKingdoms.Serialization;
#endif

namespace Tools
{
    /// <summary>
    /// JSON序列化帮助类，提供统一的JsonSerializerOptions配置
    /// 修复JSON反序列化崩溃问题的关键配置：
    /// - IncludeFields = true (必须开启才能读到数据)
    /// - PropertyNameCaseInsensitive = true (忽略大小写)
    /// - AllowTrailingCommas = true (允许尾随逗号)
    /// - ReadCommentHandling = JsonCommentHandling.Skip (跳过注释)
    /// - UnsafeRelaxedJsonEscaping (允许中文不被转义，防止乱码)
    /// </summary>
    public static class JsonHelper
    {
        private static JsonSerializerOptions _options;
        public static JsonSerializerOptions Options 
        { 
            get
            {
                if (_options == null)
                {
                    _options = CreateOptions();
                }
                return _options;
            }
        }

        // 别名，更明确地表示这是宽松的选项配置
        public static JsonSerializerOptions LooseOptions => Options;

        private static JsonSerializerOptions CreateOptions()
        {
            // Use the simplified approach without JsonSerializerContext
            return new JsonSerializerOptions 
            { 
                IncludeFields = true,                  // <--- 关键！必须开这个才能读到数据
                PropertyNameCaseInsensitive = true,    // 忽略大小写
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                // 允许中文不被转义，防止乱码
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
        }
    }
}

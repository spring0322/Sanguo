using System.Text.Json;
using System.Text.Encodings.Web;

namespace WorldOfTheThreeKingdoms.Helpers
{
    public static class JsonOptionsHelper
    {
        // 这个选项专门用来读老代码的 JSON
        public static readonly JsonSerializerOptions LooseOptions = new JsonSerializerOptions
        {
            // 🔥【最关键】允许读取 public 字段 (老代码全是字段)
            IncludeFields = true,
            
            // 忽略大小写 (PersonID vs personId)
            PropertyNameCaseInsensitive = true,
            
            // 允许尾部逗号 (防止 JSON 格式不严谨报错)
            AllowTrailingCommas = true,
            
            // 跳过注释
            ReadCommentHandling = JsonCommentHandling.Skip,
            
            // 解决中文乱码问题
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }
}
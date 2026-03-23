using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace AnalyzeSerializationIssues
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("=== 序列化字段完整性分析工具 ===\n");

            // 定义要检查的核心类型对
            List<(string EntityFile, string DtoFile)> typePairs =
            [
                ("WorldOfTheThreeKingdoms/GameObjects/Person.cs", "WorldOfTheThreeKingdoms/Serialization/DTOs/PersonDTO.cs"),
                ("WorldOfTheThreeKingdoms/GameObjects/Architecture.cs", "WorldOfTheThreeKingdoms/Serialization/DTOs/ArchitectureDTO.cs"),
                ("WorldOfTheThreeKingdoms/GameObjects/Faction.cs", "WorldOfTheThreeKingdoms/Serialization/DTOs/FactionDTO.cs"),
                ("WorldOfTheThreeKingdoms/GameObjects/Troop.cs", "WorldOfTheThreeKingdoms/Serialization/DTOs/TroopDTO.cs"),
                ("WorldOfTheThreeKingdoms/GameObjects/Treasure.cs", "WorldOfTheThreeKingdoms/Serialization/DTOs/TreasureDTO.cs")
            ];

            Dictionary<string, List<string>> allMissingFields = [];

            foreach (var (entityFile, dtoFile) in typePairs)
            {
                var typeName = Path.GetFileNameWithoutExtension(entityFile);
                Console.WriteLine($"\n{'='} 检查 {typeName} {'='}\n");
                var missing = AnalyzeTypePair(entityFile, dtoFile);
                allMissingFields[typeName] = missing;
            }

            Console.WriteLine("\n\n" + new string('=', 80));
            Console.WriteLine("=== 字段分类分析 ===");
            Console.WriteLine(new string('=', 80));

            foreach (var (typeName, missingFields) in allMissingFields)
            {
                if (missingFields.Count > 0)
                {
                    FieldClassifier.ClassifyMissingFields(typeName, missingFields);
                }
            }

            Console.WriteLine("\n=== 分析完成 ===");
        }

        static List<string> AnalyzeTypePair(string entityPath, string dtoPath)
        {
            if (!File.Exists(entityPath))
            {
                Console.WriteLine($"❌ 实体文件不存在: {entityPath}");
                return [];
            }

            if (!File.Exists(dtoPath))
            {
                Console.WriteLine($"❌ DTO文件不存在: {dtoPath}");
                return [];
            }

            var entityContent = File.ReadAllText(entityPath);
            var dtoContent = File.ReadAllText(dtoPath);

            // 提取实体类中带 [DataMember] 的字段和属性
            var entityFields = ExtractDataMemberFields(entityContent);
            
            // 提取 DTO 中的所有公共属性
            var dtoProperties = ExtractPublicProperties(dtoContent);

            // 对比分析
            List<string> missing = [];
            List<string> found = [];

            foreach (var field in entityFields)
            {
                // 尝试匹配（忽略大小写，因为可能有命名转换）
                var matched = dtoProperties.Any(p => 
                    string.Equals(p, field, StringComparison.OrdinalIgnoreCase));

                if (matched)
                {
                    found.Add(field);
                }
                else
                {
                    missing.Add(field);
                }
            }

            // 输出结果
            Console.WriteLine($"✅ 实体类中有 [DataMember] 的字段: {entityFields.Count}");
            Console.WriteLine($"✅ DTO 中的公共属性: {dtoProperties.Count}");
            Console.WriteLine($"✅ 已匹配: {found.Count}");
            
            if (missing.Count > 0)
            {
                Console.WriteLine($"\n⚠️  缺失的字段 ({missing.Count}):");
                foreach (var field in missing.OrderBy(f => f))
                {
                    Console.WriteLine($"   - {field}");
                }
            }
            else
            {
                Console.WriteLine("\n✅ 所有 [DataMember] 字段都已在 DTO 中定义");
            }
            
            return missing;
        }

        static List<string> ExtractDataMemberFields(string content)
        {
            List<string> fields = [];
            
            // 匹配模式：[DataMember] 后面跟着属性或字段定义
            // 支持多行，支持 [JsonInclude] 等其他特性
            var pattern = @"\[DataMember\][\s\S]*?(?:public|private|internal|protected)\s+(?:static\s+)?(?:readonly\s+)?[\w<>,\[\]\?]+\s+(\w+)\s*(?:\{|;|=)";
            
            var matches = Regex.Matches(content, pattern, RegexOptions.Multiline);
            
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    var fieldName = match.Groups[1].Value;
                    // 过滤掉明显的私有字段（小写开头）
                    if (!string.IsNullOrEmpty(fieldName) && char.IsUpper(fieldName[0]))
                    {
                        fields.Add(fieldName);
                    }
                }
            }

            return fields.Distinct().ToList();
        }

        static List<string> ExtractPublicProperties(string content)
        {
            List<string> properties = [];
            
            // 匹配公共属性
            var pattern = @"public\s+[\w<>,\[\]\?]+\s+(\w+)\s*\{";
            
            var matches = Regex.Matches(content, pattern);
            
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    var propName = match.Groups[1].Value;
                    if (!string.IsNullOrEmpty(propName))
                    {
                        properties.Add(propName);
                    }
                }
            }

            return properties.Distinct().ToList();
        }
    }
}

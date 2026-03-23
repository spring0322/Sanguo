using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameObjects;

namespace WorldOfTheThreeKingdoms.AOTCompatibility
{
    /// <summary>
    /// AOT序列化兼容性分析器
    /// 检测代码中不兼容AOT编译的序列化模式
    /// </summary>
    public class AOTSerializationCompatibilityAnalyzer
    {
        /// <summary>
        /// 兼容性问题类型
        /// </summary>
        public enum CompatibilityIssueType
        {
            MissingJsonAttributes,      // 缺少JSON特性
            ReflectionDependency,       // 反射依赖
            DynamicTypeDiscovery,       // 动态类型发现
            UnsupportedConverter,       // 不支持的转换器
            MissingSourceGeneration,    // 缺少源生成
            PolymorphicSerialization   // 多态序列化问题
        }

        /// <summary>
        /// 兼容性问题
        /// </summary>
        public class CompatibilityIssue
        {
            public CompatibilityIssueType IssueType { get; set; }
            public string TypeName { get; set; }
            public string PropertyName { get; set; }
            public string Description { get; set; }
            public string Recommendation { get; set; }
            public string Severity { get; set; } // "Critical", "Warning", "Info"
        }

        /// <summary>
        /// 兼容性分析报告
        /// </summary>
        public class CompatibilityReport
        {
            public List<CompatibilityIssue> Issues { get; set; } = new List<CompatibilityIssue>();
            public int CriticalIssues => Issues.Count(i => i.Severity == "Critical");
            public int WarningIssues => Issues.Count(i => i.Severity == "Warning");
            public int InfoIssues => Issues.Count(i => i.Severity == "Info");
            public DateTime AnalysisTime { get; set; } = DateTime.Now;
            public bool IsAOTCompatible => CriticalIssues == 0;

            public string GetSummary()
            {
                return $"AOT兼容性分析: {CriticalIssues}个严重问题, {WarningIssues}个警告, {InfoIssues}个信息";
            }
        }

        private readonly List<Type> _gameObjectTypes;

        public AOTSerializationCompatibilityAnalyzer()
        {
            _gameObjectTypes = new List<Type>
            {
                typeof(GameObject),
                typeof(Person),
                typeof(Architecture),
                typeof(Faction),
                typeof(Troop),
                typeof(Legion)
            };
        }

        /// <summary>
        /// 分析AOT序列化兼容性
        /// </summary>
        /// <returns>兼容性报告</returns>
        [UnconditionalSuppressMessage("Trimming", "IL2072:'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicProperties'", 
            Justification = "_gameObjectTypes contains known game object types with public properties")]
        public CompatibilityReport AnalyzeCompatibility()
        {
            Debug.WriteLine("[AOTSerializationCompatibilityAnalyzer] 开始AOT序列化兼容性分析");

            var report = new CompatibilityReport();

            try
            {
                // 分析每个游戏对象类型
                foreach (var type in _gameObjectTypes)
                {
                    AnalyzeType(type, report);
                }

                // 分析序列化上下文
                AnalyzeSerializationContext(report);

                // 分析自定义转换器
                AnalyzeCustomConverters(report);

                Debug.WriteLine($"[AOTSerializationCompatibilityAnalyzer] 分析完成: {report.GetSummary()}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AOTSerializationCompatibilityAnalyzer] 分析异常: {ex.Message}");
                report.Issues.Add(new CompatibilityIssue
                {
                    IssueType = CompatibilityIssueType.ReflectionDependency,
                    TypeName = "AnalysisEngine",
                    Description = $"分析过程中发生异常: {ex.Message}",
                    Recommendation = "检查分析器实现和目标类型",
                    Severity = "Critical"
                });
            }

            return report;
        }

        /// <summary>
        /// 分析单个类型的兼容性
        /// </summary>
        private void AnalyzeType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type, CompatibilityReport report)
        {
            Debug.WriteLine($"[AOTSerializationCompatibilityAnalyzer] 分析类型: {type.Name}");

            try
            {
                // 检查类型级别的JSON特性
                CheckTypeJsonAttributes(type, report);

                // 检查属性级别的JSON特性
                CheckPropertyJsonAttributes(type, report);

                // 检查多态序列化支持
                CheckPolymorphicSerialization(type, report);

                // 检查反射依赖
                CheckReflectionDependencies(type, report);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AOTSerializationCompatibilityAnalyzer] 分析类型{type.Name}异常: {ex.Message}");
                report.Issues.Add(new CompatibilityIssue
                {
                    IssueType = CompatibilityIssueType.ReflectionDependency,
                    TypeName = type.Name,
                    Description = $"类型分析异常: {ex.Message}",
                    Recommendation = "检查类型定义和特性配置",
                    Severity = "Warning"
                });
            }
        }

        /// <summary>
        /// 检查类型级别的JSON特性
        /// </summary>
        private void CheckTypeJsonAttributes([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type, CompatibilityReport report)
        {
            // 检查是否有JsonConverter特性
            var converterAttr = type.GetCustomAttribute<JsonConverterAttribute>();
            if (converterAttr == null && type.IsClass && !type.IsAbstract)
            {
                // 对于复杂的游戏对象，建议添加JsonConverter特性
                if (type.GetProperties().Length > 5)
                {
                    report.Issues.Add(new CompatibilityIssue
                    {
                        IssueType = CompatibilityIssueType.MissingJsonAttributes,
                        TypeName = type.Name,
                        Description = "复杂类型缺少JsonConverter特性",
                        Recommendation = "考虑添加[JsonConverter]特性以优化序列化性能",
                        Severity = "Info"
                    });
                }
            }

            // 检查JsonSerializable特性（应该在JsonContext中定义）
            var serializableAttr = type.GetCustomAttribute<JsonSerializableAttribute>();
            if (serializableAttr == null)
            {
                report.Issues.Add(new CompatibilityIssue
                {
                    IssueType = CompatibilityIssueType.MissingSourceGeneration,
                    TypeName = type.Name,
                    Description = "类型未在JsonSerializationContext中注册",
                    Recommendation = "在GameJsonContext中添加[JsonSerializable]特性",
                    Severity = "Warning"
                });
            }
        }

        /// <summary>
        /// 检查属性级别的JSON特性
        /// </summary>
        private void CheckPropertyJsonAttributes([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type, CompatibilityReport report)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                // 检查复杂属性是否有适当的JSON特性
                if (IsComplexProperty(property))
                {
                    var includeAttr = property.GetCustomAttribute<JsonIncludeAttribute>();
                    var ignoreAttr = property.GetCustomAttribute<JsonIgnoreAttribute>();
                    var propertyNameAttr = property.GetCustomAttribute<JsonPropertyNameAttribute>();

                    // 如果是重要的关系属性，应该有明确的序列化配置
                    if (IsImportantRelationshipProperty(property.Name))
                    {
                        if (includeAttr == null && ignoreAttr == null)
                        {
                            report.Issues.Add(new CompatibilityIssue
                            {
                                IssueType = CompatibilityIssueType.MissingJsonAttributes,
                                TypeName = type.Name,
                                PropertyName = property.Name,
                                Description = "重要关系属性缺少JSON序列化配置",
                                Recommendation = "添加[JsonInclude]或[JsonIgnore]特性明确序列化行为",
                                Severity = "Warning"
                            });
                        }
                    }

                    // 检查循环引用风险
                    if (HasCircularReferenceRisk(property))
                    {
                        if (ignoreAttr == null)
                        {
                            report.Issues.Add(new CompatibilityIssue
                            {
                                IssueType = CompatibilityIssueType.PolymorphicSerialization,
                                TypeName = type.Name,
                                PropertyName = property.Name,
                                Description = "属性可能导致循环引用",
                                Recommendation = "考虑添加[JsonIgnore]或使用自定义转换器",
                                Severity = "Warning"
                            });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 检查多态序列化支持
        /// </summary>
        private void CheckPolymorphicSerialization(Type type, CompatibilityReport report)
        {
            if (type.IsAbstract || type.IsInterface)
            {
                // 抽象类或接口需要多态序列化支持
                var derivedTypesAttr = type.GetCustomAttribute<JsonDerivedTypeAttribute>();
                if (derivedTypesAttr == null)
                {
                    report.Issues.Add(new CompatibilityIssue
                    {
                        IssueType = CompatibilityIssueType.PolymorphicSerialization,
                        TypeName = type.Name,
                        Description = "抽象类型缺少多态序列化配置",
                        Recommendation = "添加[JsonDerivedType]特性支持多态序列化",
                        Severity = "Critical"
                    });
                }
            }
        }

        /// <summary>
        /// 检查反射依赖
        /// </summary>
        private void CheckReflectionDependencies([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type, CompatibilityReport report)
        {
            // 这里可以添加更复杂的反射依赖检测逻辑
            // 目前主要检查是否使用了不推荐的序列化模式

            var properties = type.GetProperties();
            foreach (var property in properties)
            {
                // 检查是否有动态类型属性
                if (property.PropertyType == typeof(object) || property.PropertyType == typeof(System.Dynamic.ExpandoObject))
                {
                    report.Issues.Add(new CompatibilityIssue
                    {
                        IssueType = CompatibilityIssueType.DynamicTypeDiscovery,
                        TypeName = type.Name,
                        PropertyName = property.Name,
                        Description = "属性使用动态类型，可能不兼容AOT",
                        Recommendation = "使用具体类型或添加自定义转换器",
                        Severity = "Critical"
                    });
                }
            }
        }

        /// <summary>
        /// 分析序列化上下文
        /// </summary>
        private void AnalyzeSerializationContext(CompatibilityReport report)
        {
            Debug.WriteLine("[AOTSerializationCompatibilityAnalyzer] 分析序列化上下文");

            try
            {
                // 检查GameJsonContext是否存在
                var contextType = Type.GetType("WorldOfTheThreeKingdoms.Serialization.GameJsonContext");
                if (contextType == null)
                {
                    report.Issues.Add(new CompatibilityIssue
                    {
                        IssueType = CompatibilityIssueType.MissingSourceGeneration,
                        TypeName = "GameJsonContext",
                        Description = "缺少AOT序列化上下文",
                        Recommendation = "创建GameJsonContext类并添加[JsonSerializable]特性",
                        Severity = "Critical"
                    });
                }
                else
                {
                    Debug.WriteLine("  ✅ 找到GameJsonContext");
                    
                    // 检查上下文是否包含所有必要的类型
                    CheckSerializationContextCompleteness(contextType, report);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AOTSerializationCompatibilityAnalyzer] 序列化上下文分析异常: {ex.Message}");
                report.Issues.Add(new CompatibilityIssue
                {
                    IssueType = CompatibilityIssueType.MissingSourceGeneration,
                    TypeName = "SerializationContext",
                    Description = $"序列化上下文分析失败: {ex.Message}",
                    Recommendation = "检查序列化上下文的实现",
                    Severity = "Warning"
                });
            }
        }

        /// <summary>
        /// 检查序列化上下文的完整性
        /// </summary>
        private void CheckSerializationContextCompleteness(Type contextType, CompatibilityReport report)
        {
            // 检查是否为每个游戏对象类型都添加了JsonSerializable特性
            var registeredTypes = new HashSet<Type>();
            foreach (var attrData in contextType.GetCustomAttributesData())
            {
                if (attrData.AttributeType == typeof(JsonSerializableAttribute))
                {
                    if (attrData.ConstructorArguments.Count > 0 && attrData.ConstructorArguments[0].Value is Type type)
                    {
                        registeredTypes.Add(type);
                    }
                }
            }
            
            foreach (var gameObjectType in _gameObjectTypes)
            {
                if (!registeredTypes.Contains(gameObjectType))
                {
                    report.Issues.Add(new CompatibilityIssue
                    {
                        IssueType = CompatibilityIssueType.MissingSourceGeneration,
                        TypeName = gameObjectType.Name,
                        Description = $"类型未在序列化上下文中注册",
                        Recommendation = $"在GameJsonContext中添加[JsonSerializable(typeof({gameObjectType.Name}))]",
                        Severity = "Warning"
                    });
                }
            }
        }

        /// <summary>
        /// 分析自定义转换器
        /// </summary>
        private void AnalyzeCustomConverters(CompatibilityReport report)
        {
            Debug.WriteLine("[AOTSerializationCompatibilityAnalyzer] 分析自定义转换器");

            // 检查是否有必要的自定义转换器
            var requiredConverters = new[]
            {
                "GameObjectReferenceConverter",
                "FactionLeaderConverter", 
                "PersonIdealTendencyConverter"
            };

            foreach (var converterName in requiredConverters)
            {
                var converterType = Type.GetType($"WorldOfTheThreeKingdoms.Serialization.SystemTextJson.{converterName}");
                if (converterType == null)
                {
                    report.Issues.Add(new CompatibilityIssue
                    {
                        IssueType = CompatibilityIssueType.UnsupportedConverter,
                        TypeName = converterName,
                        Description = "缺少必要的自定义转换器",
                        Recommendation = $"实现{converterName}类",
                        Severity = "Warning"
                    });
                }
                else
                {
                    Debug.WriteLine($"  ✅ 找到转换器: {converterName}");
                }
            }
        }

        /// <summary>
        /// 判断是否为复杂属性
        /// </summary>
        private bool IsComplexProperty(PropertyInfo property)
        {
            var type = property.PropertyType;
            return !type.IsPrimitive && 
                   type != typeof(string) && 
                   type != typeof(DateTime) && 
                   type != typeof(decimal) &&
                   !type.IsEnum;
        }

        /// <summary>
        /// 判断是否为重要的关系属性
        /// </summary>
        private bool IsImportantRelationshipProperty(string propertyName)
        {
            var importantProperties = new[]
            {
                "Leader", "BelongedFaction", "BelongedLegion", "Mayor", 
                "IdealTendency", "Persons", "Architectures", "Troops"
            };

            return importantProperties.Contains(propertyName);
        }

        /// <summary>
        /// 判断是否有循环引用风险
        /// </summary>
        private bool HasCircularReferenceRisk(PropertyInfo property)
        {
            var riskyProperties = new[]
            {
                "BelongedFaction", "Leader", "Mayor", "Persons", "Architectures", "Troops", "Legions"
            };

            return riskyProperties.Contains(property.Name);
        }

        /// <summary>
        /// 生成修复建议
        /// </summary>
        /// <param name="report">兼容性报告</param>
        /// <returns>修复建议列表</returns>
        public List<string> GenerateFixRecommendations(CompatibilityReport report)
        {
            var recommendations = new List<string>();

            if (report.CriticalIssues > 0)
            {
                recommendations.Add("🔴 发现严重的AOT兼容性问题，需要立即修复：");
                
                foreach (var issue in report.Issues.Where(i => i.Severity == "Critical"))
                {
                    recommendations.Add($"  - {issue.TypeName}: {issue.Description}");
                    recommendations.Add($"    建议: {issue.Recommendation}");
                }
            }

            if (report.WarningIssues > 0)
            {
                recommendations.Add("\n🟡 发现警告级别的兼容性问题，建议修复：");
                
                foreach (var issue in report.Issues.Where(i => i.Severity == "Warning"))
                {
                    recommendations.Add($"  - {issue.TypeName}: {issue.Description}");
                    recommendations.Add($"    建议: {issue.Recommendation}");
                }
            }

            if (report.IsAOTCompatible)
            {
                recommendations.Add("✅ AOT序列化兼容性检查通过！");
            }

            return recommendations;
        }
    }
}
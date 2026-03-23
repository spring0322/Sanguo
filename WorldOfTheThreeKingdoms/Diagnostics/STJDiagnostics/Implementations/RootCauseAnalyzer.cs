using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Interfaces;
using WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Models;
using WorldOfTheThreeKingdoms.Serialization;

namespace WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Implementations
{
    /// <summary>
    /// 根本原因分析器实现 - 系统性诊断STJ反序列化失败
    /// 实现需求: 1.1, 1.2, 1.3, 1.4, 1.5
    /// </summary>
    public class RootCauseAnalyzer : IRootCauseAnalyzer
    {
        private readonly Dictionary<Type, RegistrationStatus> _typeRegistrationCache;
        private readonly HashSet<Type> _analyzedTypes;

        public RootCauseAnalyzer()
        {
            _typeRegistrationCache = new Dictionary<Type, RegistrationStatus>();
            _analyzedTypes = new HashSet<Type>();
        }

        /// <summary>
        /// 分析反序列化失败的根本原因
        /// 需求 1.1: 检查完整的类型注册链
        /// </summary>
        public async Task<DiagnosticResult> AnalyzeDeserializationFailure(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type targetType, 
            string jsonData, 
            JsonSerializerOptions options)
        {
            var result = new DiagnosticResult();
            var issues = new List<DiagnosticIssue>();

            try
            {
                // 1. 验证类型注册链
                var typeRegistrationReport = await ValidateTypeRegistrationChain(targetType);
                result.TypeRegistrations = typeRegistrationReport;

                // 2. 检测命名空间冲突
                var namespaceConflictReport = await DetectNamespaceConflicts(targetType);
                result.NamespaceConflicts = namespaceConflictReport;

                // 3. 验证JSON结构
                var structuralMismatchReport = await ValidateJsonStructure(targetType, jsonData);
                result.StructuralMismatches = structuralMismatchReport;

                // 4. 分析问题并生成建议
                await AnalyzeIssuesAndGenerateRecommendations(result, issues);

                result.Issues = issues;
                result.IsSuccessful = !issues.Any(i => i.Severity == IssueSeverity.Critical || i.Severity == IssueSeverity.Error);
            }
            catch (Exception ex)
            {
                issues.Add(new DiagnosticIssue
                {
                    Severity = IssueSeverity.Critical,
                    Category = IssueCategory.Configuration,
                    Description = "分析过程中发生异常",
                    DetailedAnalysis = ex.Message,
                    RecommendedActions = { "检查分析器配置", "验证输入参数" }
                });
                result.IsSuccessful = false;
            }

            return result;
        }
        /// <summary>
        /// 验证完整的类型注册链
        /// 需求 1.1, 1.2: 检查完整类型注册链并验证所有依赖类型已注册
        /// </summary>
        public async Task<TypeRegistrationReport> ValidateTypeRegistrationChain([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type rootType)
        {
            var report = new TypeRegistrationReport();
            var visitedTypes = new HashSet<Type>();
            var dependencyStack = new Stack<Type>();

            await AnalyzeTypeRecursively(rootType, report, visitedTypes, dependencyStack);

            return report;
        }

        /// <summary>
        /// 递归分析类型及其依赖
        /// </summary>
        [UnconditionalSuppressMessage("Trimming", "IL2072:'type' argument does not satisfy 'DynamicallyAccessedMemberTypes'", 
            Justification = "Dependencies come from GetTypeDependencies which analyzes known game object types")]
        private async Task AnalyzeTypeRecursively(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type type, 
            TypeRegistrationReport report, 
            HashSet<Type> visitedTypes, 
            Stack<Type> dependencyStack)
        {
            if (visitedTypes.Contains(type))
            {
                // 检测循环依赖
                if (dependencyStack.Contains(type))
                {
                    report.CircularDependencies.Add(type);
                    report.TypeRegistrations[type] = RegistrationStatus.CircularDependency;
                }
                return;
            }

            visitedTypes.Add(type);
            dependencyStack.Push(type);

            // 检查类型是否在GameJsonContext中注册
            var registrationStatus = CheckTypeRegistration(type);
            report.TypeRegistrations[type] = registrationStatus;

            if (registrationStatus == RegistrationStatus.Missing)
            {
                report.MissingRegistrations.Add(type);
            }

            // 分析依赖类型
            var dependencies = GetTypeDependencies(type);
            report.DependencyChain[type] = dependencies.ToList();

            foreach (var dependency in dependencies)
            {
                await AnalyzeTypeRecursively(dependency, report, visitedTypes, dependencyStack);
            }

            dependencyStack.Pop();
        }

        /// <summary>
        /// 检查类型是否在GameJsonContext中注册
        /// </summary>
        private RegistrationStatus CheckTypeRegistration(Type type)
        {
            if (_typeRegistrationCache.TryGetValue(type, out var cachedStatus))
            {
                return cachedStatus;
            }

            var status = RegistrationStatus.Missing;

            try
            {
                // 检查是否有JsonSerializable属性注册
                var contextType = typeof(GameJsonContext);
                var attributes = contextType.GetCustomAttributes<JsonSerializableAttribute>();
                
                var isRegistered = attributes.Any(attr => attr.TypeInfoPropertyName != null && 
                    attr.TypeInfoPropertyName.Contains(type.Name));

                if (isRegistered)
                {
                    status = RegistrationStatus.Registered;
                }
                else
                {
                    // 检查是否存在命名空间冲突
                    var conflictingTypes = attributes
                        .Where(attr => attr.TypeInfoPropertyName != null && 
                               attr.TypeInfoPropertyName.EndsWith(type.Name))
                        .Count();

                    if (conflictingTypes > 1)
                    {
                        status = RegistrationStatus.Ambiguous;
                    }
                }
            }
            catch (Exception)
            {
                status = RegistrationStatus.Missing;
            }

            _typeRegistrationCache[type] = status;
            return status;
        }

        /// <summary>
        /// 获取类型的依赖类型
        /// </summary>
        private IEnumerable<Type> GetTypeDependencies([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type type)
        {
            var dependencies = new HashSet<Type>();

            // 分析属性类型
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                AddTypeDependency(property.PropertyType, dependencies);
            }

            // 分析字段类型
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                AddTypeDependency(field.FieldType, dependencies);
            }

            // 分析基类
            if (type.BaseType != null && type.BaseType != typeof(object))
            {
                dependencies.Add(type.BaseType);
            }

            return dependencies;
        }

        /// <summary>
        /// 添加类型依赖，处理泛型类型
        /// </summary>
        private void AddTypeDependency(Type type, HashSet<Type> dependencies)
        {
            if (type.IsPrimitive || type == typeof(string) || type == typeof(DateTime))
            {
                return;
            }

            if (type.IsGenericType)
            {
                // 处理泛型类型，如Dictionary<int, EventEffectKind>
                dependencies.Add(type.GetGenericTypeDefinition());
                foreach (var genericArg in type.GetGenericArguments())
                {
                    AddTypeDependency(genericArg, dependencies);
                }
            }
            else
            {
                dependencies.Add(type);
            }
        }
        /// <summary>
        /// 检测命名空间冲突
        /// 需求 1.3: 识别TroopDetail和ArchitectureDetail命名空间之间的模糊类型引用
        /// </summary>
        public async Task<NamespaceConflictReport> DetectNamespaceConflicts(Type targetType)
        {
            var report = new NamespaceConflictReport();
            var conflicts = new List<NamespaceConflict>();

            // 特别检查EventEffectKind相关的命名空间冲突
            if (IsEventEffectKindRelated(targetType))
            {
                await AnalyzeEventEffectKindConflicts(conflicts);
            }

            // 检查其他潜在的命名空间冲突
            await AnalyzeGeneralNamespaceConflicts(targetType, conflicts);

            report.Conflicts = conflicts;
            report.HasConflicts = conflicts.Any();

            // 构建模糊类型名称映射
            foreach (var conflict in conflicts)
            {
                if (!report.AmbiguousTypeNames.ContainsKey(conflict.TypeName))
                {
                    report.AmbiguousTypeNames[conflict.TypeName] = new List<Type>();
                }

                foreach (var ns in conflict.ConflictingNamespaces)
                {
                    var type = Type.GetType($"{ns}.{conflict.TypeName}");
                    if (type != null)
                    {
                        report.AmbiguousTypeNames[conflict.TypeName].Add(type);
                    }
                }
            }

            return report;
        }

        /// <summary>
        /// 检查是否与EventEffectKind相关
        /// </summary>
        private bool IsEventEffectKindRelated(Type type)
        {
            return type.Name.Contains("EventEffectKind") || 
                   type.Namespace?.Contains("EventEffect") == true ||
                   type.FullName?.Contains("EventEffectKindTable") == true;
        }

        /// <summary>
        /// 分析EventEffectKind相关的命名空间冲突
        /// </summary>
        private async Task AnalyzeEventEffectKindConflicts(List<NamespaceConflict> conflicts)
        {
            // 模拟异步命名空间冲突分析过程
            await Task.Delay(8).ConfigureAwait(false);
            
            // EventEffectKind在两个命名空间中都存在
            var troopNamespace = "GameObjects.TroopDetail.EventEffect";
            var architectureNamespace = "GameObjects.ArchitectureDetail.EventEffect";

            var eventEffectKindConflict = new NamespaceConflict
            {
                TypeName = "EventEffectKind",
                ConflictingNamespaces = { troopNamespace, architectureNamespace },
                Severity = ConflictSeverity.High,
                RecommendedResolution = "使用完全限定的类型名称或类型别名来区分两个EventEffectKind类型"
            };

            conflicts.Add(eventEffectKindConflict);

            // EventEffectKindTable也存在相同问题
            var eventEffectKindTableConflict = new NamespaceConflict
            {
                TypeName = "EventEffectKindTable",
                ConflictingNamespaces = { troopNamespace, architectureNamespace },
                Severity = ConflictSeverity.Critical,
                RecommendedResolution = "确保GameJsonContext中正确注册了两个不同命名空间的EventEffectKindTable类型"
            };

            conflicts.Add(eventEffectKindTableConflict);
        }

        /// <summary>
        /// 分析一般的命名空间冲突
        /// </summary>
        private async Task AnalyzeGeneralNamespaceConflicts(Type targetType, List<NamespaceConflict> conflicts)
        {
            // 模拟异步一般命名空间冲突分析过程
            await Task.Delay(6).ConfigureAwait(false);
            
            // 获取所有已加载的程序集中的类型
            var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => 
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch
                    {
                        return new Type[0];
                    }
                })
                .Where(t => t.IsClass && !t.IsAbstract)
                .ToList();

            // 按类型名称分组，查找重复
            var typeGroups = allTypes
                .GroupBy(t => t.Name)
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var group in typeGroups)
            {
                var typeName = group.Key;
                var types = group.ToList();

                // 检查是否与目标类型相关
                if (IsRelatedToTargetType(targetType, types))
                {
                    var conflict = new NamespaceConflict
                    {
                        TypeName = typeName,
                        ConflictingNamespaces = types.Select(t => t.Namespace).Distinct().ToList(),
                        Severity = DetermineSeverity(types),
                        RecommendedResolution = $"使用完全限定的类型名称来区分 {typeName} 的不同实现"
                    };

                    conflicts.Add(conflict);
                }
            }
        }

        /// <summary>
        /// 检查类型是否与目标类型相关
        /// </summary>
        private bool IsRelatedToTargetType(Type targetType, List<Type> types)
        {
            // 检查是否在相同的命名空间层次结构中
            var targetNamespace = targetType.Namespace ?? "";
            return types.Any(t => 
            {
                var typeNamespace = t.Namespace ?? "";
                return targetNamespace.StartsWith(typeNamespace) || 
                       typeNamespace.StartsWith(targetNamespace) ||
                       HasCommonNamespaceRoot(targetNamespace, typeNamespace);
            });
        }

        /// <summary>
        /// 检查是否有共同的命名空间根
        /// </summary>
        private bool HasCommonNamespaceRoot(string ns1, string ns2)
        {
            var parts1 = ns1.Split('.');
            var parts2 = ns2.Split('.');
            
            var minLength = Math.Min(parts1.Length, parts2.Length);
            for (int i = 0; i < minLength; i++)
            {
                if (parts1[i] != parts2[i])
                {
                    return i > 0; // 至少有一个共同的根部分
                }
            }
            
            return minLength > 0;
        }

        /// <summary>
        /// 确定冲突严重程度
        /// </summary>
        private ConflictSeverity DetermineSeverity(List<Type> conflictingTypes)
        {
            // 如果涉及序列化相关的类型，严重程度更高
            if (conflictingTypes.Any(t => 
                t.GetCustomAttributes<JsonSerializableAttribute>().Any() ||
                t.Name.Contains("Table") ||
                t.Name.Contains("Kind")))
            {
                return ConflictSeverity.High;
            }

            return ConflictSeverity.Medium;
        }
        /// <summary>
        /// 验证JSON结构与预期类型结构的匹配性
        /// 需求 1.4: 比较实际JSON数据与预期类结构
        /// </summary>
        public async Task<StructuralMismatchReport> ValidateJsonStructure([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type targetType, string jsonData)
        {
            var report = new StructuralMismatchReport();
            var mismatches = new List<StructuralMismatch>();

            try
            {
                // 分析JSON结构
                var jsonAnalysis = await AnalyzeJsonStructure(jsonData);
                report.JsonAnalysis = jsonAnalysis;

                // 分析类型结构
                var typeAnalysis = await AnalyzeTypeStructure(targetType);
                report.TypeAnalysis = typeAnalysis;

                // 比较结构并识别不匹配
                await CompareStructures(jsonAnalysis, typeAnalysis, mismatches);

                report.Mismatches = mismatches;
                report.HasMismatches = mismatches.Any();
            }
            catch (Exception ex)
            {
                report.HasMismatches = true;
                mismatches.Add(new StructuralMismatch
                {
                    PropertyPath = "root",
                    ExpectedType = targetType.Name,
                    ActualType = "unknown",
                    Type = MismatchType.InvalidFormat,
                    Description = $"JSON结构分析失败: {ex.Message}"
                });
                report.Mismatches = mismatches;
            }

            return report;
        }

        /// <summary>
        /// 分析JSON结构
        /// </summary>
        private async Task<JsonStructureAnalysis> AnalyzeJsonStructure(string jsonData)
        {
            // 模拟异步JSON结构分析过程
            await Task.Delay(7).ConfigureAwait(false);
            
            var analysis = new JsonStructureAnalysis();

            if (string.IsNullOrWhiteSpace(jsonData))
            {
                analysis.IsValid = false;
                analysis.ValidationErrors.Add("JSON数据为空或null");
                return analysis;
            }

            try
            {
                using var document = JsonDocument.Parse(jsonData);
                var root = document.RootElement;

                analysis.IsValid = true;
                analysis.Depth = CalculateJsonDepth(root);
                
                // 提取属性信息
                ExtractJsonProperties(root, analysis.Properties, "");
            }
            catch (JsonException ex)
            {
                analysis.IsValid = false;
                analysis.ValidationErrors.Add($"JSON格式错误: {ex.Message}");
            }

            return analysis;
        }

        /// <summary>
        /// 计算JSON深度
        /// </summary>
        private int CalculateJsonDepth(JsonElement element, int currentDepth = 0)
        {
            var maxDepth = currentDepth;

            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        var depth = CalculateJsonDepth(property.Value, currentDepth + 1);
                        maxDepth = Math.Max(maxDepth, depth);
                    }
                    break;

                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        var depth = CalculateJsonDepth(item, currentDepth + 1);
                        maxDepth = Math.Max(maxDepth, depth);
                    }
                    break;
            }

            return maxDepth;
        }

        /// <summary>
        /// 提取JSON属性
        /// </summary>
        private void ExtractJsonProperties(JsonElement element, Dictionary<string, object> properties, string path)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        var propertyPath = string.IsNullOrEmpty(path) ? property.Name : $"{path}.{property.Name}";
                        properties[propertyPath] = GetJsonValueType(property.Value);
                        
                        if (property.Value.ValueKind == JsonValueKind.Object || 
                            property.Value.ValueKind == JsonValueKind.Array)
                        {
                            ExtractJsonProperties(property.Value, properties, propertyPath);
                        }
                    }
                    break;

                case JsonValueKind.Array:
                    var arrayIndex = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        var itemPath = $"{path}[{arrayIndex}]";
                        properties[itemPath] = GetJsonValueType(item);
                        
                        if (item.ValueKind == JsonValueKind.Object || 
                            item.ValueKind == JsonValueKind.Array)
                        {
                            ExtractJsonProperties(item, properties, itemPath);
                        }
                        arrayIndex++;
                    }
                    break;
            }
        }

        /// <summary>
        /// 获取JSON值的类型描述
        /// </summary>
        private string GetJsonValueType(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => "string",
                JsonValueKind.Number => "number",
                JsonValueKind.True or JsonValueKind.False => "boolean",
                JsonValueKind.Null => "null",
                JsonValueKind.Object => "object",
                JsonValueKind.Array => "array",
                _ => "unknown"
            };
        }

        /// <summary>
        /// 分析类型结构
        /// </summary>
        private async Task<TypeStructureAnalysis> AnalyzeTypeStructure([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type type)
        {
            // 模拟异步类型结构分析过程
            await Task.Delay(9).ConfigureAwait(false);
            
            var analysis = new TypeStructureAnalysis();

            // 分析属性
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var propertyName = property.Name;
                var propertyType = GetFriendlyTypeName(property.PropertyType);
                
                analysis.Properties[propertyName] = propertyType;

                // 检查是否为必需属性
                if (IsRequiredProperty(property))
                {
                    analysis.RequiredProperties.Add(propertyName);
                }
                else
                {
                    analysis.OptionalProperties.Add(propertyName);
                }
            }

            // 分析字段
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var fieldName = field.Name;
                var fieldType = GetFriendlyTypeName(field.FieldType);
                
                analysis.Properties[fieldName] = fieldType;

                // 字段通常被认为是必需的，除非有特殊标记
                if (IsRequiredField(field))
                {
                    analysis.RequiredProperties.Add(fieldName);
                }
                else
                {
                    analysis.OptionalProperties.Add(fieldName);
                }
            }

            return analysis;
        }

        /// <summary>
        /// 获取友好的类型名称
        /// </summary>
        private string GetFriendlyTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                var genericTypeName = type.GetGenericTypeDefinition().Name;
                var genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetFriendlyTypeName));
                return $"{genericTypeName.Split('`')[0]}<{genericArgs}>";
            }

            return type.Name;
        }

        /// <summary>
        /// 检查属性是否为必需
        /// </summary>
        private bool IsRequiredProperty(PropertyInfo property)
        {
            // 检查JsonRequired属性或其他必需标记
            return property.GetCustomAttributes().Any(attr => 
                attr.GetType().Name.Contains("Required") ||
                attr.GetType().Name.Contains("DataMember"));
        }

        /// <summary>
        /// 检查字段是否为必需
        /// </summary>
        private bool IsRequiredField(FieldInfo field)
        {
            // 检查DataMember属性或其他必需标记
            return field.GetCustomAttributes().Any(attr => 
                attr.GetType().Name.Contains("Required") ||
                attr.GetType().Name.Contains("DataMember"));
        }

        /// <summary>
        /// 比较JSON结构和类型结构
        /// </summary>
        private async Task CompareStructures(
            JsonStructureAnalysis jsonAnalysis, 
            TypeStructureAnalysis typeAnalysis, 
            List<StructuralMismatch> mismatches)
        {
            // 模拟异步结构比较过程
            await Task.Delay(6).ConfigureAwait(false);
            
            // 检查缺失的必需属性
            foreach (var requiredProperty in typeAnalysis.RequiredProperties)
            {
                if (!jsonAnalysis.Properties.ContainsKey(requiredProperty))
                {
                    mismatches.Add(new StructuralMismatch
                    {
                        PropertyPath = requiredProperty,
                        ExpectedType = typeAnalysis.Properties.GetValueOrDefault(requiredProperty, "unknown"),
                        ActualType = "missing",
                        Type = MismatchType.MissingProperty,
                        Description = $"缺少必需属性: {requiredProperty}"
                    });
                }
            }

            // 检查额外的属性
            foreach (var jsonProperty in jsonAnalysis.Properties.Keys)
            {
                if (!typeAnalysis.Properties.ContainsKey(jsonProperty))
                {
                    mismatches.Add(new StructuralMismatch
                    {
                        PropertyPath = jsonProperty,
                        ExpectedType = "not expected",
                        ActualType = jsonAnalysis.Properties[jsonProperty].ToString(),
                        Type = MismatchType.ExtraProperty,
                        Description = $"JSON中存在额外属性: {jsonProperty}"
                    });
                }
            }

            // 检查类型不匹配
            foreach (var commonProperty in jsonAnalysis.Properties.Keys.Intersect(typeAnalysis.Properties.Keys))
            {
                var jsonType = jsonAnalysis.Properties[commonProperty].ToString();
                var expectedType = typeAnalysis.Properties[commonProperty];

                if (!AreTypesCompatible(jsonType, expectedType))
                {
                    mismatches.Add(new StructuralMismatch
                    {
                        PropertyPath = commonProperty,
                        ExpectedType = expectedType,
                        ActualType = jsonType,
                        Type = MismatchType.TypeMismatch,
                        Description = $"属性 {commonProperty} 的类型不匹配: 期望 {expectedType}, 实际 {jsonType}"
                    });
                }
            }
        }

        /// <summary>
        /// 检查类型是否兼容
        /// </summary>
        private bool AreTypesCompatible(string jsonType, string expectedType)
        {
            // 简单的类型兼容性检查
            var compatibilityMap = new Dictionary<string, string[]>
            {
                ["string"] = new[] { "String", "string" },
                ["number"] = new[] { "Int32", "int", "Double", "double", "Float", "float", "Decimal", "decimal" },
                ["boolean"] = new[] { "Boolean", "bool" },
                ["object"] = new[] { "Object", "Dictionary", "EventEffectKind", "EventEffectKindTable" },
                ["array"] = new[] { "List", "Array", "IEnumerable", "Collection" }
            };

            if (compatibilityMap.TryGetValue(jsonType, out var compatibleTypes))
            {
                return compatibleTypes.Any(ct => expectedType.Contains(ct));
            }

            return jsonType == expectedType;
        }
        /// <summary>
        /// 分析问题并生成修复建议
        /// </summary>
        private async Task AnalyzeIssuesAndGenerateRecommendations(
            DiagnosticResult result, 
            List<DiagnosticIssue> issues)
        {
            // 分析类型注册问题
            if (result.TypeRegistrations != null)
            {
                await AnalyzeTypeRegistrationIssues(result.TypeRegistrations, issues);
            }

            // 分析命名空间冲突问题
            if (result.NamespaceConflicts != null && result.NamespaceConflicts.HasConflicts)
            {
                await AnalyzeNamespaceConflictIssues(result.NamespaceConflicts, issues);
            }

            // 分析结构不匹配问题
            if (result.StructuralMismatches != null && result.StructuralMismatches.HasMismatches)
            {
                await AnalyzeStructuralMismatchIssues(result.StructuralMismatches, issues);
            }

            // 生成综合修复建议
            result.RecommendedFix = await GenerateComprehensiveFixRecommendation(result, issues);
        }

        /// <summary>
        /// 分析类型注册问题
        /// </summary>
        private async Task AnalyzeTypeRegistrationIssues(
            TypeRegistrationReport report, 
            List<DiagnosticIssue> issues)
        {
            // 模拟异步类型注册问题分析过程
            await Task.Delay(5).ConfigureAwait(false);
            
            // 缺失的类型注册
            foreach (var missingType in report.MissingRegistrations)
            {
                issues.Add(new DiagnosticIssue
                {
                    Severity = IssueSeverity.Critical,
                    Category = IssueCategory.TypeRegistration,
                    Description = $"类型 {missingType.FullName} 未在GameJsonContext中注册",
                    DetailedAnalysis = $"STJ需要显式注册所有要序列化的类型。类型 {missingType.Name} 缺少 [JsonSerializable] 属性注册。",
                    RecommendedActions = 
                    {
                        $"在GameJsonContext类上添加: [JsonSerializable(typeof({missingType.FullName}))]",
                        "重新编译项目以生成新的类型信息",
                        "验证所有依赖类型也已正确注册"
                    }
                });
            }

            // 循环依赖问题
            foreach (var circularType in report.CircularDependencies)
            {
                issues.Add(new DiagnosticIssue
                {
                    Severity = IssueSeverity.Error,
                    Category = IssueCategory.TypeRegistration,
                    Description = $"检测到循环依赖: {circularType.FullName}",
                    DetailedAnalysis = "循环依赖可能导致序列化过程中的无限递归或性能问题。",
                    RecommendedActions = 
                    {
                        "检查类型设计，考虑使用接口或抽象类打破循环",
                        "使用 [JsonIgnore] 属性忽略导致循环的属性",
                        "考虑使用 ReferenceHandler.Preserve 处理对象引用"
                    }
                });
            }

            // 模糊类型注册
            var ambiguousTypes = report.TypeRegistrations
                .Where(kvp => kvp.Value == RegistrationStatus.Ambiguous)
                .ToList();

            foreach (var ambiguousType in ambiguousTypes)
            {
                issues.Add(new DiagnosticIssue
                {
                    Severity = IssueSeverity.Warning,
                    Category = IssueCategory.TypeRegistration,
                    Description = $"类型注册可能存在歧义: {ambiguousType.Key.FullName}",
                    DetailedAnalysis = "存在多个同名类型的注册，可能导致反序列化时选择错误的类型。",
                    RecommendedActions = 
                    {
                        "使用完全限定的类型名称进行注册",
                        "检查是否存在命名空间冲突",
                        "考虑重命名冲突的类型"
                    }
                });
            }
        }

        /// <summary>
        /// 分析命名空间冲突问题
        /// </summary>
        private async Task AnalyzeNamespaceConflictIssues(
            NamespaceConflictReport report, 
            List<DiagnosticIssue> issues)
        {
            // 模拟异步命名空间冲突问题分析过程
            await Task.Delay(4).ConfigureAwait(false);
            
            foreach (var conflict in report.Conflicts)
            {
                var severity = conflict.Severity switch
                {
                    ConflictSeverity.Critical => IssueSeverity.Critical,
                    ConflictSeverity.High => IssueSeverity.Error,
                    ConflictSeverity.Medium => IssueSeverity.Warning,
                    _ => IssueSeverity.Info
                };

                issues.Add(new DiagnosticIssue
                {
                    Severity = severity,
                    Category = IssueCategory.NamespaceConflict,
                    Description = $"命名空间冲突: {conflict.TypeName}",
                    DetailedAnalysis = $"类型 {conflict.TypeName} 在多个命名空间中存在: {string.Join(", ", conflict.ConflictingNamespaces)}。这可能导致STJ无法确定要使用哪个类型进行反序列化。",
                    RecommendedActions = 
                    {
                        conflict.RecommendedResolution,
                        "在GameJsonContext中使用完全限定的类型名称",
                        "验证所有相关类型都已正确注册"
                    }
                });
            }
        }

        /// <summary>
        /// 分析结构不匹配问题
        /// </summary>
        private async Task AnalyzeStructuralMismatchIssues(
            StructuralMismatchReport report, 
            List<DiagnosticIssue> issues)
        {
            foreach (var mismatch in report.Mismatches)
            {
                var severity = mismatch.Type switch
                {
                    MismatchType.MissingProperty => IssueSeverity.Error,
                    MismatchType.TypeMismatch => IssueSeverity.Error,
                    MismatchType.ExtraProperty => IssueSeverity.Warning,
                    MismatchType.NullValue => IssueSeverity.Warning,
                    MismatchType.InvalidFormat => IssueSeverity.Critical,
                    _ => IssueSeverity.Info
                };

                issues.Add(new DiagnosticIssue
                {
                    Severity = severity,
                    Category = IssueCategory.JsonStructure,
                    Description = $"结构不匹配: {mismatch.PropertyPath}",
                    DetailedAnalysis = mismatch.Description,
                    RecommendedActions = GenerateStructuralMismatchActions(mismatch)
                });
            }
            
            // 添加真正的异步操作以避免CS1998警告
            await Task.CompletedTask;
        }

        /// <summary>
        /// 为结构不匹配生成修复建议
        /// </summary>
        private List<string> GenerateStructuralMismatchActions(StructuralMismatch mismatch)
        {
            return mismatch.Type switch
            {
                MismatchType.MissingProperty => new List<string>
                {
                    "检查JSON数据是否完整",
                    "验证属性名称是否正确",
                    "考虑使属性可选或提供默认值"
                },
                MismatchType.TypeMismatch => new List<string>
                {
                    "检查JSON数据中的值类型",
                    "验证类型定义是否正确",
                    "考虑添加类型转换器"
                },
                MismatchType.ExtraProperty => new List<string>
                {
                    "检查JSON数据是否包含不需要的属性",
                    "考虑在类型中添加对应的属性",
                    "使用JsonIgnoreCondition.WhenWritingNull忽略额外属性"
                },
                MismatchType.NullValue => new List<string>
                {
                    "检查null值是否符合预期",
                    "考虑使属性可空",
                    "添加null值处理逻辑"
                },
                MismatchType.InvalidFormat => new List<string>
                {
                    "验证JSON格式是否正确",
                    "检查是否存在语法错误",
                    "使用JSON验证工具检查数据"
                },
                _ => new List<string> { "检查数据结构和类型定义的一致性" }
            };
        }

        /// <summary>
        /// 生成综合修复建议
        /// </summary>
        private async Task<FixRecommendation> GenerateComprehensiveFixRecommendation(
            DiagnosticResult result, 
            List<DiagnosticIssue> issues)
        {
            // 模拟异步综合修复建议生成过程
            await Task.Delay(10).ConfigureAwait(false);
            
            var recommendation = new FixRecommendation();

            // 确定修复策略
            recommendation.Strategy = DetermineFixStrategy(issues);

            // 生成类型修复建议
            if (result.TypeRegistrations?.MissingRegistrations.Any() == true)
            {
                foreach (var missingType in result.TypeRegistrations.MissingRegistrations)
                {
                    recommendation.TypeFixes.Add(new TypeRegistrationFix
                    {
                        TypeName = missingType.Name,
                        Namespace = missingType.Namespace,
                        RequiredAttribute = $"[JsonSerializable(typeof({missingType.FullName}))]",
                        CodeSnippet = GenerateRegistrationCodeSnippet(missingType),
                        Priority = DetermineFixPriority(missingType)
                    });
                }
            }

            // 生成配置修复建议
            recommendation.ConfigurationFixes.AddRange(GenerateConfigurationFixes(result));

            // 评估影响
            recommendation.Impact = await EvaluateFixImpact(recommendation, issues);

            return recommendation;
        }

        /// <summary>
        /// 确定修复策略
        /// </summary>
        private FixStrategy DetermineFixStrategy(List<DiagnosticIssue> issues)
        {
            var criticalIssues = issues.Count(i => i.Severity == IssueSeverity.Critical);
            var errorIssues = issues.Count(i => i.Severity == IssueSeverity.Error);

            if (criticalIssues > 0 || errorIssues > 3)
            {
                return FixStrategy.ComprehensiveOverhaul;
            }

            var hasTypeRegistrationIssues = issues.Any(i => i.Category == IssueCategory.TypeRegistration);
            var hasNamespaceConflicts = issues.Any(i => i.Category == IssueCategory.NamespaceConflict);

            if (hasTypeRegistrationIssues && hasNamespaceConflicts)
            {
                return FixStrategy.ResolveNamespaceConflicts;
            }

            if (hasTypeRegistrationIssues)
            {
                return FixStrategy.AddMissingRegistrations;
            }

            return FixStrategy.RepairJsonStructure;
        }

        /// <summary>
        /// 生成类型注册代码片段
        /// </summary>
        private string GenerateRegistrationCodeSnippet(Type type)
        {
            return $"[JsonSerializable(typeof({type.FullName}))]";
        }

        /// <summary>
        /// 确定修复优先级
        /// </summary>
        private int DetermineFixPriority(Type type)
        {
            // EventEffectKind相关类型优先级最高
            if (type.Name.Contains("EventEffectKind"))
            {
                return 1;
            }

            // 字典类型优先级较高
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                return 2;
            }

            // 其他类型
            return 3;
        }

        /// <summary>
        /// 生成配置修复建议
        /// </summary>
        private List<ConfigurationFix> GenerateConfigurationFixes(DiagnosticResult result)
        {
            var fixes = new List<ConfigurationFix>();

            // 建议使用宽松的序列化选项
            fixes.Add(new ConfigurationFix
            {
                ConfigurationName = "JsonSerializerOptions.PropertyNameCaseInsensitive",
                CurrentValue = "false",
                RecommendedValue = "true",
                Justification = "提高属性名称匹配的容错性"
            });

            fixes.Add(new ConfigurationFix
            {
                ConfigurationName = "JsonSerializerOptions.AllowTrailingCommas",
                CurrentValue = "false",
                RecommendedValue = "true",
                Justification = "允许JSON中的尾随逗号，提高兼容性"
            });

            return fixes;
        }

        /// <summary>
        /// 评估修复影响
        /// </summary>
        private async Task<EstimatedImpact> EvaluateFixImpact(
            FixRecommendation recommendation, 
            List<DiagnosticIssue> issues)
        {
            var impact = new EstimatedImpact();

            // 评估性能影响
            var typeFixCount = recommendation.TypeFixes.Count;
            impact.PerformanceImpact = typeFixCount switch
            {
                0 => ImpactLevel.None,
                <= 5 => ImpactLevel.Low,
                <= 15 => ImpactLevel.Medium,
                _ => ImpactLevel.High
            };

            // 评估兼容性影响
            var hasNamespaceConflicts = issues.Any(i => i.Category == IssueCategory.NamespaceConflict);
            impact.CompatibilityImpact = hasNamespaceConflicts ? ImpactLevel.Medium : ImpactLevel.Low;

            // 评估维护影响
            impact.MaintenanceImpact = recommendation.Strategy switch
            {
                FixStrategy.ComprehensiveOverhaul => ImpactLevel.High,
                FixStrategy.ResolveNamespaceConflicts => ImpactLevel.Medium,
                _ => ImpactLevel.Low
            };

            // 风险评估
            impact.RiskAssessment = GenerateRiskAssessment(recommendation, issues);

            // 前置条件
            impact.Prerequisites.AddRange(GeneratePrerequisites(recommendation));

            // 添加真正的异步操作以避免CS1998警告
            await Task.CompletedTask;
            return impact;
        }

        /// <summary>
        /// 生成风险评估
        /// </summary>
        private string GenerateRiskAssessment(FixRecommendation recommendation, List<DiagnosticIssue> issues)
        {
            var riskFactors = new List<string>();

            if (recommendation.TypeFixes.Count > 10)
            {
                riskFactors.Add("大量类型注册更改可能影响编译时间");
            }

            if (issues.Any(i => i.Category == IssueCategory.NamespaceConflict))
            {
                riskFactors.Add("命名空间冲突解决可能需要代码重构");
            }

            if (recommendation.Strategy == FixStrategy.ComprehensiveOverhaul)
            {
                riskFactors.Add("全面改造风险较高，建议分阶段实施");
            }

            return riskFactors.Any() 
                ? $"中等风险: {string.Join("; ", riskFactors)}"
                : "低风险: 修复相对安全，影响范围有限";
        }

        /// <summary>
        /// 生成前置条件
        /// </summary>
        private List<string> GeneratePrerequisites(FixRecommendation recommendation)
        {
            var prerequisites = new List<string>
            {
                "备份当前的GameJsonContext.cs文件",
                "确保所有相关类型都已编译"
            };

            if (recommendation.TypeFixes.Any())
            {
                prerequisites.Add("验证所有要注册的类型都存在且可访问");
            }

            if (recommendation.Strategy == FixStrategy.ResolveNamespaceConflicts)
            {
                prerequisites.Add("确认命名空间冲突的解决方案");
            }

            return prerequisites;
        }
    }
}
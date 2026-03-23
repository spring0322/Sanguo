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
using GameObjects;
using GameObjects.TroopDetail.EventEffect;
using GameObjects.ArchitectureDetail.EventEffect;

namespace WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Implementations
{
    /// <summary>
    /// 类型注册验证器实现 - 全面验证STJ类型注册和依赖
    /// 实现需求: 2.1, 2.2, 2.3, 2.4, 2.5
    /// </summary>
    public class TypeRegistrationValidator : ITypeRegistrationValidator
    {
        private readonly Dictionary<Type, ValidationResult> _validationCache;
        private readonly HashSet<Type> _circularDependencyDetected;

        public TypeRegistrationValidator()
        {
            _validationCache = new Dictionary<Type, ValidationResult>();
            _circularDependencyDetected = new HashSet<Type>();
        }

        /// <summary>
        /// 验证EventEffectKindTable注册状态
        /// 需求 2.1: 验证EventEffectKindTable在两个命名空间中的注册
        /// </summary>
        public ValidationResult ValidateEventEffectKindTableRegistration()
        {
            var result = new ValidationResult { IsValid = true };

            try
            {
                // 验证TroopDetail.EventEffect.EventEffectKindTable
                var troopEventEffectKindTableType = GetTypeByName("GameObjects.TroopDetail.EventEffect.EventEffectKindTable");
                if (troopEventEffectKindTableType != null)
                {
                    var troopValidation = ValidateSpecificTypeRegistration(troopEventEffectKindTableType);
                    MergeValidationResults(result, troopValidation, "TroopDetail.EventEffectKindTable");
                }
                else
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Code = "TYPE001",
                        Message = "无法找到TroopDetail.EventEffect.EventEffectKindTable类型",
                        Details = "类型可能不存在或无法访问",
                        Recommendation = "检查类型定义和项目引用"
                    });
                }

                // 验证ArchitectureDetail.EventEffect.EventEffectKindTable
                var archEventEffectKindTableType = GetTypeByName("GameObjects.ArchitectureDetail.EventEffect.EventEffectKindTable");
                if (archEventEffectKindTableType != null)
                {
                    var archValidation = ValidateSpecificTypeRegistration(archEventEffectKindTableType);
                    MergeValidationResults(result, archValidation, "ArchitectureDetail.EventEffectKindTable");
                }
                else
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Code = "TYPE002",
                        Message = "无法找到ArchitectureDetail.EventEffect.EventEffectKindTable类型",
                        Details = "类型可能不存在或无法访问",
                        Recommendation = "检查类型定义和项目引用"
                    });
                }

                // 验证字典类型注册
                var troopEventEffectKindType = GetTypeByName("GameObjects.TroopDetail.EventEffect.EventEffectKind");
                if (troopEventEffectKindType != null)
                {
                    var troopDictType = typeof(Dictionary<,>).MakeGenericType(typeof(int), troopEventEffectKindType);
                    var troopDictValidation = ValidateSpecificTypeRegistration(troopDictType);
                    MergeValidationResults(result, troopDictValidation, "TroopDetail.EventEffectKind Dictionary");
                }

                var archEventEffectKindType = GetTypeByName("GameObjects.ArchitectureDetail.EventEffect.EventEffectKind");
                if (archEventEffectKindType != null)
                {
                    var archDictType = typeof(Dictionary<,>).MakeGenericType(typeof(int), archEventEffectKindType);
                    var archDictValidation = ValidateSpecificTypeRegistration(archDictType);
                    MergeValidationResults(result, archDictValidation, "ArchitectureDetail.EventEffectKind Dictionary");
                }

                // 添加特定的EventEffectKindTable验证元数据
                result.Metadata["EventEffectKindTableValidation"] = new
                {
                    TroopDetailRegistered = troopEventEffectKindTableType != null,
                    ArchitectureDetailRegistered = archEventEffectKindTableType != null,
                    TroopDictionaryRegistered = troopEventEffectKindType != null,
                    ArchitectureDictionaryRegistered = archEventEffectKindType != null
                };
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Code = "EEKT001",
                    Message = "EventEffectKindTable验证过程中发生异常",
                    Details = ex.Message,
                    RecommendedAction = "检查类型定义和GameJsonContext配置"
                });
            }

            return result;
        }
        /// <summary>
        /// 验证依赖类型
        /// 需求 2.2: 确保EventEffectKind及其所有属性都已注册
        /// </summary>
        public ValidationResult ValidateDependentTypes([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.Interfaces)] Type rootType)
        {
            if (_validationCache.TryGetValue(rootType, out var cachedResult))
            {
                return cachedResult;
            }

            var result = new ValidationResult { IsValid = true };
            var visitedTypes = new HashSet<Type>();
            var dependencyStack = new Stack<Type>();

            try
            {
                ValidateDependentTypesRecursive(rootType, result, visitedTypes, dependencyStack);
                
                // 添加依赖分析元数据
                result.Metadata["DependencyAnalysis"] = new
                {
                    RootType = rootType.FullName,
                    TotalDependencies = visitedTypes.Count,
                    ValidatedTypes = visitedTypes.Select(t => t.FullName).ToList()
                };
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Code = "DEP001",
                    Message = $"依赖类型验证失败: {rootType.FullName}",
                    Details = ex.Message,
                    RecommendedAction = "检查类型依赖关系和注册状态"
                });
            }

            _validationCache[rootType] = result;
            return result;
        }

        /// <summary>
        /// 递归验证依赖类型
        /// </summary>
        [UnconditionalSuppressMessage("Trimming", "IL2072:'type' argument does not satisfy 'DynamicallyAccessedMemberTypes'", 
            Justification = "Dependencies come from GetTypeDependencies which analyzes known game object types")]
        private void ValidateDependentTypesRecursive(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.Interfaces)] Type type, 
            ValidationResult result, 
            HashSet<Type> visitedTypes, 
            Stack<Type> dependencyStack)
        {
            if (visitedTypes.Contains(type))
            {
                // 检测循环依赖
                if (dependencyStack.Contains(type))
                {
                    _circularDependencyDetected.Add(type);
                    result.Warnings.Add(new ValidationWarning
                    {
                        Code = "CIRC001",
                        Message = $"检测到循环依赖: {type.FullName}",
                        Details = $"依赖链: {string.Join(" -> ", dependencyStack.Select(t => t.Name))} -> {type.Name}",
                        Recommendation = "考虑重构类型设计以避免循环依赖"
                    });
                }
                return;
            }

            visitedTypes.Add(type);
            dependencyStack.Push(type);

            // 验证当前类型的注册状态
            var typeValidation = ValidateSpecificTypeRegistration(type);
            if (!typeValidation.IsValid)
            {
                result.IsValid = false;
                foreach (var error in typeValidation.Errors)
                {
                    result.Errors.Add(error);
                }
            }

            // 分析依赖类型
            var dependencies = GetTypeDependencies(type);
            foreach (var dependency in dependencies)
            {
                ValidateDependentTypesRecursive(dependency, result, visitedTypes, dependencyStack);
            }

            dependencyStack.Pop();
        }

        /// <summary>
        /// 验证多态类型
        /// 需求 1.5: 验证所有派生的EventEffectKind类型都已注册
        /// </summary>
        public ValidationResult ValidatePolymorphicTypes(Type baseType)
        {
            var result = new ValidationResult { IsValid = true };

            try
            {
                // 获取所有派生类型
                var derivedTypes = GetDerivedTypes(baseType);
                
                foreach (var derivedType in derivedTypes)
                {
                    var derivedValidation = ValidateSpecificTypeRegistration(derivedType);
                    if (!derivedValidation.IsValid)
                    {
                        result.IsValid = false;
                        result.Errors.Add(new ValidationError
                        {
                            Code = "POLY001",
                            Message = $"多态派生类型未注册: {derivedType.FullName}",
                            Details = $"基类 {baseType.FullName} 的派生类型 {derivedType.FullName} 未在GameJsonContext中注册",
                            RecommendedAction = $"添加 [JsonSerializable(typeof({derivedType.FullName}))] 到GameJsonContext"
                        });
                    }
                }

                // 特别检查EventEffectKind派生类型
                if (baseType.Name.Contains("EventEffectKind"))
                {
                    ValidateEventEffectKindDerivedTypes(baseType, result);
                }

                result.Metadata["PolymorphicValidation"] = new
                {
                    BaseType = baseType.FullName,
                    DerivedTypesCount = derivedTypes.Count,
                    DerivedTypes = derivedTypes.Select(t => t.FullName).ToList()
                };
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Code = "POLY002",
                    Message = "多态类型验证过程中发生异常",
                    Details = ex.Message,
                    RecommendedAction = "检查基类型定义和继承关系"
                });
            }

            return result;
        }

        /// <summary>
        /// 验证JsonSerializerOptions配置
        /// 需求 2.5: 确认GameJsonContext配置正确
        /// </summary>
        public ValidationResult ValidateJsonSerializerOptions(JsonSerializerOptions options)
        {
            var result = new ValidationResult { IsValid = true };

            try
            {
                // 验证TypeInfoResolver
                if (options.TypeInfoResolver == null)
                {
                    result.Errors.Add(new ValidationError
                    {
                        Code = "OPT001",
                        Message = "TypeInfoResolver未配置",
                        Details = "JsonSerializerOptions.TypeInfoResolver为null",
                        RecommendedAction = "设置TypeInfoResolver为GameJsonContext.Default"
                    });
                    result.IsValid = false;
                }
                else if (options.TypeInfoResolver != GameJsonContext.Default)
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Code = "OPT002",
                        Message = "TypeInfoResolver不是GameJsonContext.Default",
                        Details = $"当前TypeInfoResolver类型: {options.TypeInfoResolver.GetType().FullName}",
                        Recommendation = "建议使用GameJsonContext.Default以确保类型注册正确"
                    });
                }

                // 验证关键配置选项
                ValidateSerializerOptionsConfiguration(options, result);

                // 验证转换器配置
                ValidateConvertersConfiguration(options, result);

                result.Metadata["OptionsValidation"] = new
                {
                    HasTypeInfoResolver = options.TypeInfoResolver != null,
                    TypeInfoResolverType = options.TypeInfoResolver?.GetType().FullName,
                    ConvertersCount = options.Converters?.Count ?? 0,
                    PropertyNamingPolicy = options.PropertyNamingPolicy?.GetType().Name,
                    IncludeFields = options.IncludeFields
                };
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Code = "OPT003",
                    Message = "JsonSerializerOptions验证过程中发生异常",
                    Details = ex.Message,
                    RecommendedAction = "检查JsonSerializerOptions配置"
                });
            }

            return result;
        }

        /// <summary>
        /// 生成修复计划
        /// </summary>
        public async Task<RegistrationFixPlan> GenerateFixPlan(ValidationResult result)
        {
            var fixPlan = new RegistrationFixPlan();

            try
            {
                // 分析错误并生成修复建议
                foreach (var error in result.Errors)
                {
                    var fix = GenerateFixForError(error);
                    if (fix != null)
                    {
                        if (IsRequiredFix(error))
                        {
                            fixPlan.RequiredFixes.Add(fix);
                        }
                        else
                        {
                            fixPlan.RecommendedFixes.Add(fix);
                        }
                    }
                }

                // 评估修复影响
                fixPlan.Impact = await EvaluateFixImpact(fixPlan);

                // 生成前置条件
                fixPlan.Prerequisites = GeneratePrerequisites(fixPlan);

                // 生成实现指导
                fixPlan.Implementation = GenerateImplementationGuide(fixPlan);
            }
            catch (Exception ex)
            {
                // 如果生成修复计划失败，至少提供基本信息
                fixPlan.RequiredFixes.Add(new TypeRegistrationFix
                {
                    TypeName = "Unknown",
                    Namespace = "Unknown",
                    RequiredAttribute = "Manual review required",
                    CodeSnippet = $"// 修复计划生成失败: {ex.Message}",
                    Priority = 1
                });
            }

            return fixPlan;
        }
        /// <summary>
        /// 验证特定类型的注册状态
        /// </summary>
        private ValidationResult ValidateSpecificTypeRegistration(Type type)
        {
            var result = new ValidationResult { IsValid = true };

            try
            {
                // 检查是否在GameJsonContext中注册
                var isRegistered = IsTypeRegisteredInGameJsonContext(type);
                
                if (!isRegistered)
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Code = "REG001",
                        Message = $"类型未注册: {type.FullName}",
                        Details = $"类型 {type.FullName} 未在GameJsonContext中找到JsonSerializable属性注册",
                        RecommendedAction = $"在GameJsonContext类上添加: [JsonSerializable(typeof({type.FullName}))]"
                    });
                }

                // 检查泛型类型的特殊情况
                if (type.IsGenericType)
                {
                    ValidateGenericTypeRegistration(type, result);
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Code = "REG002",
                    Message = $"类型注册验证异常: {type.FullName}",
                    Details = ex.Message,
                    RecommendedAction = "检查类型定义和可访问性"
                });
            }

            return result;
        }

        /// <summary>
        /// 检查类型是否在GameJsonContext中注册
        /// </summary>
        private bool IsTypeRegisteredInGameJsonContext(Type type)
        {
            try
            {
                var contextType = typeof(GameJsonContext);
                var attributes = contextType.GetCustomAttributes<JsonSerializableAttribute>();
                
                // 直接类型匹配
                var directMatch = attributes.Any(attr => 
                {
                    try
                    {
                        return attr.TypeInfoPropertyName != null && 
                               (attr.TypeInfoPropertyName.Contains(type.Name) ||
                                attr.TypeInfoPropertyName.Contains(type.FullName));
                    }
                    catch
                    {
                        return false;
                    }
                });

                if (directMatch) return true;

                // 对于泛型类型，检查是否有对应的注册
                if (type.IsGenericType)
                {
                    var genericTypeDefinition = type.GetGenericTypeDefinition();
                    var genericArgs = type.GetGenericArguments();
                    
                    // 检查泛型定义是否注册
                    var genericMatch = attributes.Any(attr =>
                    {
                        try
                        {
                            return attr.TypeInfoPropertyName != null &&
                                   attr.TypeInfoPropertyName.Contains(genericTypeDefinition.Name.Split('`')[0]);
                        }
                        catch
                        {
                            return false;
                        }
                    });

                    return genericMatch;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 验证泛型类型注册
        /// </summary>
        private void ValidateGenericTypeRegistration(Type type, ValidationResult result)
        {
            if (type.IsGenericType)
            {
                var genericArgs = type.GetGenericArguments();
                foreach (var arg in genericArgs)
                {
                    if (!IsTypeRegisteredInGameJsonContext(arg) && !arg.IsPrimitive && arg != typeof(string))
                    {
                        result.Warnings.Add(new ValidationWarning
                        {
                            Code = "GEN001",
                            Message = $"泛型参数类型可能未注册: {arg.FullName}",
                            Details = $"泛型类型 {type.FullName} 的参数类型 {arg.FullName} 可能需要单独注册",
                            Recommendation = $"考虑添加: [JsonSerializable(typeof({arg.FullName}))]"
                        });
                    }
                }
            }
        }

        /// <summary>
        /// 获取类型依赖
        /// </summary>
        private IEnumerable<Type> GetTypeDependencies([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.Interfaces)] Type type)
        {
            var dependencies = new HashSet<Type>();

            try
            {
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

                // 分析接口
                foreach (var interfaceType in type.GetInterfaces())
                {
                    if (!interfaceType.IsGenericType || interfaceType.Assembly != typeof(object).Assembly)
                    {
                        dependencies.Add(interfaceType);
                    }
                }
            }
            catch (Exception)
            {
                // 如果分析依赖失败，返回空集合
            }

            return dependencies;
        }

        /// <summary>
        /// 添加类型依赖
        /// </summary>
        private void AddTypeDependency(Type type, HashSet<Type> dependencies)
        {
            if (type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) || type == typeof(object))
            {
                return;
            }

            if (type.IsGenericType)
            {
                // 添加泛型定义
                dependencies.Add(type.GetGenericTypeDefinition());
                
                // 递归添加泛型参数
                foreach (var genericArg in type.GetGenericArguments())
                {
                    AddTypeDependency(genericArg, dependencies);
                }
            }
            else if (type.IsArray)
            {
                // 添加数组元素类型
                AddTypeDependency(type.GetElementType(), dependencies);
            }
            else
            {
                dependencies.Add(type);
            }
        }

        /// <summary>
        /// 获取派生类型
        /// </summary>
        private List<Type> GetDerivedTypes(Type baseType)
        {
            var derivedTypes = new List<Type>();

            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        var types = assembly.GetTypes()
                            .Where(t => t.IsClass && !t.IsAbstract && baseType.IsAssignableFrom(t) && t != baseType)
                            .ToList();
                        
                        derivedTypes.AddRange(types);
                    }
                    catch (ReflectionTypeLoadException)
                    {
                        // 忽略无法加载的程序集
                    }
                }
            }
            catch (Exception)
            {
                // 如果获取派生类型失败，返回空列表
            }

            return derivedTypes;
        }

        /// <summary>
        /// 验证EventEffectKind派生类型
        /// </summary>
        private void ValidateEventEffectKindDerivedTypes(Type baseType, ValidationResult result)
        {
            // 已知的EventEffectKind派生类型
            var knownDerivedTypes = new[]
            {
                "EventEffectKind0", "EventEffectKind1", "EventEffectKind10", "EventEffectKind15",
                "EventEffectKind20", "EventEffectKind25", "EventEffectKind30", "EventEffectKind35",
                "EventEffectKind40", "EventEffectKind45", "EventEffectKind50", "EventEffectKind60",
                "EventEffectKind80", "EventEffectKind100"
            };

            foreach (var typeName in knownDerivedTypes)
            {
                try
                {
                    // 检查TroopDetail命名空间中的派生类型
                    var troopDerivedTypeName = $"GameObjects.TroopDetail.EventEffect.EventEffectKindPack.{typeName}";
                    var troopDerivedType = Type.GetType(troopDerivedTypeName);
                    
                    if (troopDerivedType != null && !IsTypeRegisteredInGameJsonContext(troopDerivedType))
                    {
                        result.Warnings.Add(new ValidationWarning
                        {
                            Code = "EEKD001",
                            Message = $"EventEffectKind派生类型可能未注册: {typeName}",
                            Details = $"TroopDetail命名空间中的 {typeName} 可能需要注册",
                            Recommendation = $"考虑添加: [JsonSerializable(typeof({troopDerivedTypeName}))]"
                        });
                    }
                }
                catch (Exception)
                {
                    // 忽略类型加载错误
                }
            }
        }

        /// <summary>
        /// 验证序列化器选项配置
        /// </summary>
        private void ValidateSerializerOptionsConfiguration(JsonSerializerOptions options, ValidationResult result)
        {
            // 检查关键配置
            if (!options.IncludeFields)
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Code = "CFG001",
                    Message = "IncludeFields设置为false",
                    Details = "某些使用字段而非属性的类型可能无法正确序列化",
                    Recommendation = "考虑设置IncludeFields为true"
                });
            }

            if (options.PropertyNameCaseInsensitive == false)
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Code = "CFG002",
                    Message = "PropertyNameCaseInsensitive设置为false",
                    Details = "属性名称大小写敏感可能导致反序列化失败",
                    Recommendation = "考虑设置PropertyNameCaseInsensitive为true以提高兼容性"
                });
            }

            if (options.ReferenceHandler == null)
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Code = "CFG003",
                    Message = "ReferenceHandler未设置",
                    Details = "可能无法正确处理循环引用",
                    Recommendation = "考虑设置ReferenceHandler.Preserve"
                });
            }
        }

        /// <summary>
        /// 验证转换器配置
        /// </summary>
        private void ValidateConvertersConfiguration(JsonSerializerOptions options, ValidationResult result)
        {
            if (options.Converters == null || options.Converters.Count == 0)
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Code = "CONV001",
                    Message = "未配置自定义转换器",
                    Details = "可能需要自定义转换器来处理特殊类型",
                    Recommendation = "检查是否需要添加EventEffectKind等类型的转换器"
                });
                return;
            }

            // 检查是否有EventEffectKind相关的转换器
            var hasEventEffectKindConverter = options.Converters.Any(c => 
                c.GetType().Name.Contains("EventEffectKind") ||
                c.GetType().Name.Contains("LegacyDictionary"));

            if (!hasEventEffectKindConverter)
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Code = "CONV002",
                    Message = "缺少EventEffectKind转换器",
                    Details = "可能需要专门的转换器来处理EventEffectKind类型",
                    Recommendation = "考虑添加EventEffectKind或LegacyDictionary转换器"
                });
            }
        }

        /// <summary>
        /// 合并验证结果
        /// </summary>
        private void MergeValidationResults(ValidationResult target, ValidationResult source, string context)
        {
            if (!source.IsValid)
            {
                target.IsValid = false;
            }

            foreach (var error in source.Errors)
            {
                error.Details = $"[{context}] {error.Details}";
                target.Errors.Add(error);
            }

            foreach (var warning in source.Warnings)
            {
                warning.Details = $"[{context}] {warning.Details}";
                target.Warnings.Add(warning);
            }
        }

        /// <summary>
        /// 为错误生成修复建议
        /// </summary>
        private TypeRegistrationFix GenerateFixForError(ValidationError error)
        {
            if (error.Code.StartsWith("REG") && error.RecommendedAction.Contains("JsonSerializable"))
            {
                // 提取类型名称
                var typeName = ExtractTypeNameFromError(error);
                if (!string.IsNullOrEmpty(typeName))
                {
                    return new TypeRegistrationFix
                    {
                        TypeName = typeName.Split('.').Last(),
                        Namespace = typeName.Contains('.') ? string.Join(".", typeName.Split('.').Take(typeName.Split('.').Length - 1)) : "",
                        RequiredAttribute = error.RecommendedAction,
                        CodeSnippet = error.RecommendedAction,
                        Priority = DetermineFixPriority(typeName)
                    };
                }
            }

            return null;
        }

        /// <summary>
        /// 从错误中提取类型名称
        /// </summary>
        private string ExtractTypeNameFromError(ValidationError error)
        {
            try
            {
                var message = error.Message;
                if (message.Contains("类型未注册:"))
                {
                    var startIndex = message.IndexOf("类型未注册:") + "类型未注册:".Length;
                    return message.Substring(startIndex).Trim();
                }
                return "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// 确定修复优先级
        /// </summary>
        private int DetermineFixPriority(string typeName)
        {
            if (typeName.Contains("EventEffectKind"))
                return 1;
            if (typeName.Contains("Dictionary"))
                return 2;
            return 3;
        }

        /// <summary>
        /// 判断是否为必需修复
        /// </summary>
        private bool IsRequiredFix(ValidationError error)
        {
            return error.Code.StartsWith("REG") || error.Code.StartsWith("EEKT");
        }

        /// <summary>
        /// 评估修复影响
        /// </summary>
        private async Task<EstimatedImpact> EvaluateFixImpact(RegistrationFixPlan fixPlan)
        {
            // 模拟异步影响评估过程
            await Task.Delay(5).ConfigureAwait(false);
            
            var impact = new EstimatedImpact();

            var totalFixes = fixPlan.RequiredFixes.Count + fixPlan.RecommendedFixes.Count;
            
            impact.PerformanceImpact = totalFixes switch
            {
                0 => ImpactLevel.None,
                <= 5 => ImpactLevel.Low,
                <= 15 => ImpactLevel.Medium,
                _ => ImpactLevel.High
            };

            impact.CompatibilityImpact = fixPlan.RequiredFixes.Any() ? ImpactLevel.Medium : ImpactLevel.Low;
            impact.MaintenanceImpact = ImpactLevel.Low;
            impact.RiskAssessment = "低风险：主要是添加类型注册，不会破坏现有功能";

            return impact;
        }

        /// <summary>
        /// 生成前置条件
        /// </summary>
        private List<string> GeneratePrerequisites(RegistrationFixPlan fixPlan)
        {
            var prerequisites = new List<string>
            {
                "备份当前的GameJsonContext.cs文件",
                "确保项目可以正常编译"
            };

            if (fixPlan.RequiredFixes.Any())
            {
                prerequisites.Add("验证所有要注册的类型都存在且可访问");
            }

            return prerequisites;
        }

        /// <summary>
        /// 生成实现指导
        /// </summary>
        private string GenerateImplementationGuide(RegistrationFixPlan fixPlan)
        {
            var guide = "实现步骤：\n";
            guide += "1. 打开WorldOfTheThreeKingdoms/Serialization/GameJsonContext.cs文件\n";
            guide += "2. 在类声明上方添加缺失的JsonSerializable属性\n";
            
            if (fixPlan.RequiredFixes.Any())
            {
                guide += "3. 添加以下必需的类型注册：\n";
                foreach (var fix in fixPlan.RequiredFixes)
                {
                    guide += $"   - {fix.RequiredAttribute}\n";
                }
            }

            guide += "4. 重新编译项目\n";
            guide += "5. 运行测试验证修复效果\n";

            return guide;
        }

        /// <summary>
        /// 通过名称获取类型
        /// </summary>
        [UnconditionalSuppressMessage("Trimming", "IL2057:Unrecognized value passed to the parameter 'typeName' of method 'System.Type.GetType(String)'", 
            Justification = "Type names are validated and come from known game object types")]
        private Type GetTypeByName(string typeName)
        {
            try
            {
                // 首先尝试从当前程序集获取
                var currentAssembly = Assembly.GetExecutingAssembly();
                var type = currentAssembly.GetType(typeName);
                if (type != null) return type;

                // 尝试从所有已加载的程序集获取
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        type = assembly.GetType(typeName);
                        if (type != null) return type;
                    }
                    catch (Exception)
                    {
                        // 忽略无法访问的程序集
                    }
                }

                // 尝试使用Type.GetType
                return Type.GetType(typeName);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
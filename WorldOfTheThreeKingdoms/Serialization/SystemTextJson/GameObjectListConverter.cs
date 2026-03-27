using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameObjects;
using GameObjects.PersonDetail;
using GameObjects.ArchitectureDetail;
using GameObjects.FactionDetail;
using GameObjects.TroopDetail;
using System.Diagnostics.CodeAnalysis;

namespace WorldOfTheThreeKingdoms.Serialization.SystemTextJson
{
    /// <summary>
    /// System.Text.Json版本的GameObjectList转换器
    /// 替换Newtonsoft.Json的UniversalGameObjectListConverter
    /// </summary>
    public class GameObjectListConverter : JsonConverter<GameObjectList>
    {
        // 已有专用转换器的类型列表
        // 🔥 C# 12 语法：使用集合表达式
        private static readonly HashSet<Type> SpecializedTypes = [];
        // 注释：所有 GameObjectList 类型都使用通用 GameObjectListConverter 处理
        // 不再需要专用转换器列表

        public override bool CanConvert(Type typeToConvert)
        {
            // 如果有专用转换器，不处理
            if (SpecializedTypes.Contains(typeToConvert))
            {
                return false;
            }
            
            // 处理其他GameObjectList派生类
            return typeof(GameObjectList).IsAssignableFrom(typeToConvert);
        }

        public override GameObjectList Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableGameObjectListConverterLog)
            {
                System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] 🔥 开始处理: {typeToConvert.Name}");
            }
            
            // 🔥 AOT 修复：移除外层 try-catch 吞异常
            // 原实现 catch 后返回空列表，导致 ArchitectureList 等集合为空，
            // 后续 Architectures[0] 访问触发 IndexOutOfRangeException，掩盖了真正的 FormatException。
            // 现在让异常向上传播，暴露真正的根本原因。
            var list = CreateGameObjectList(typeToConvert);

            // Handle null
            if (reader.TokenType == JsonTokenType.Null)
            {
                if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableGameObjectListConverterLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] {typeToConvert.Name} - Token为Null");
                }
                return list;
            }

            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;
            
            if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableGameObjectListConverterLog)
            {
                System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] {typeToConvert.Name} - Token类型: {root.ValueKind}");
            }

            // 情况1: 对象格式 {"GameObjects": [...]}
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableGameObjectListConverterLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] {typeToConvert.Name} - 处理对象格式");
                }
                
                if (root.TryGetProperty("GameObjects", out var gameObjectsElement))
                {
                    if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableGameObjectListConverterLog)
                    {
                        System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] {typeToConvert.Name} - 找到GameObjects属性");
                    }
                    ProcessGameObjectsElement(gameObjectsElement, list, options);
                }
                else
                {
                    if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableGameObjectListConverterLog)
                    {
                        System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] {typeToConvert.Name} - ⚠️ 对象格式但没有GameObjects属性");
                        foreach (var prop in root.EnumerateObject())
                        {
                            System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] {typeToConvert.Name} - 发现属性: {prop.Name}");
                        }
                    }
                }
            }
            // 情况2: 直接是数组 [...]
            else if (root.ValueKind == JsonValueKind.Array)
            {
                if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableGameObjectListConverterLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] {typeToConvert.Name} - 直接数组格式");
                }
                ProcessGameObjectsElement(root, list, options);
            }

            if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableGameObjectListConverterLog)
            {
                System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] ✅ 完成处理: {typeToConvert.Name}, 项目数: {list.GameObjects.Count}");
            }
            return list;
        }

        /// <summary>
        /// 🔥 AOT 修复：使用 switch 代替 Activator.CreateInstance
        /// 编译器能看到显式的 new 调用，不会被 AOT 修剪
        /// </summary>
        private static GameObjectList CreateGameObjectList(Type typeToConvert)
        {
            // 使用类型名称匹配（避免 Type.GetType）
            var typeName = typeToConvert.Name;
            
            return typeName switch
            {
                "PersonList" => new PersonList(),
                "ArchitectureList" => new ArchitectureList(),
                "FactionList" => new FactionList(),
                "FactionListWithQueue" => new FactionListWithQueue(),
                "TroopList" => new TroopList(),
                "TroopListWithQueue" => new TroopListWithQueue(),
                "MilitaryList" => new MilitaryList(),
                "LegionList" => new LegionList(),
                "SectionList" => new SectionList(),
                "RegionList" => new RegionList(),
                "StateList" => new StateList(),
                "TreasureList" => new TreasureList(),
                "InformationList" => new InformationList(),
                "RoutewayList" => new RoutewayList(),
                "FacilityList" => new FacilityList(),
                "CaptiveList" => new CaptiveList(),
                "InformationKindList" => new InformationKindList(),
                "PersonGeneratorTypeList" => new PersonGeneratorTypeList(),
                "TrainPolicyList" => new TrainPolicyList(),
                "TreasureCreationSettingList" => new TreasureCreationSettingList(),
                "TroopEventList" => new TroopEventList(),
                "EventList" => new EventList(),
                // 🔥 2026-02-12 根本修复：添加 KindList 类型支持
                "AttackDefaultKindList" => new AttackDefaultKindList(),
                "AttackTargetKindList" => new AttackTargetKindList(),
                "CastDefaultKindList" => new CastDefaultKindList(),
                "CastTargetKindList" => new CastTargetKindList(),
                "MilitaryKindList" => new MilitaryKindList(),
                // 🔥 2026-03-13 根本修复：添加 IdealTendencyKindList 类型支持
                // 问题：AllIdealTendencyKinds 使用 IdealTendencyKindList 类型，但转换器不支持
                // 解决：添加 IdealTendencyKindList 到类型映射表
                "IdealTendencyKindList" => new IdealTendencyKindList(),
                // 🔥 2026-02-12 场景加载修复：添加 YearTable 类型支持
                "YearTable" => new YearTable(),
                "GameObjectList" => new GameObjectList(),
                _ => new GameObjectList() // 默认返回基类
            };
        }

        public override void Write(Utf8JsonWriter writer, GameObjectList value, JsonSerializerOptions options)
        {
            // 使用默认序列化
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName("GameObjects");
            writer.WriteStartArray();

            for (int i = 0; i < value.GameObjects.Count; i++)
            {
                var gameObject = value.GameObjects[i];
                if (gameObject == null)
                {
                    writer.WriteNullValue();
                    continue;
                }

                JsonSerializer.Serialize(writer, gameObject, JsonTypeInfoHelper.Resolve(options, gameObject.GetType()));
            }

            writer.WriteEndArray();
            writer.WriteBoolean("IsNumber", value.IsNumber);

            if (value.PropertyName == null)
            {
                writer.WriteNull("PropertyName");
            }
            else
            {
                writer.WriteString("PropertyName", value.PropertyName);
            }

            writer.WriteBoolean("SmallToBig", value.SmallToBig);
            writer.WriteEndObject();
        }

        private void ProcessGameObjectsElement(JsonElement element, GameObjectList list, JsonSerializerOptions options)
        {
            if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableGameObjectListConverterLog)
            {
                System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] ProcessGameObjectsElement - 元素类型: {element.ValueKind}");
            }
            
            if (element.ValueKind == JsonValueKind.Array)
            {
                if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableGameObjectListConverterLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] 处理数组，包含 {element.GetArrayLength()} 个项目");
                }
                
                // 🔥 根本修复：根据列表类型推断元素类型
                // CommonData.json 里的 Kind 类配置对象没有 $type 字段，必须通过列表类型推断
                // 🔥 C# 12 语法：使用 switch 表达式进行类型匹配
                Type inferredElementType = list switch
                {
                    IdealTendencyKindList      => typeof(IdealTendencyKind),
                    MilitaryKindList           => typeof(MilitaryKind),
                    InformationKindList        => typeof(InformationKind),
                    AttackDefaultKindList      => typeof(AttackDefaultKind),
                    AttackTargetKindList       => typeof(AttackTargetKind),
                    CastDefaultKindList        => typeof(CastDefaultKind),
                    CastTargetKindList         => typeof(CastTargetKind),
                    PersonGeneratorTypeList    => typeof(PersonGeneratorType),
                    TrainPolicyList            => typeof(TrainPolicy),
                    TreasureCreationSettingList => typeof(TreasureCreationSetting),
                    _ => null
                };
                
                if (inferredElementType != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] 🔥 检测到 {list.GetType().Name}，推断元素类型为 {inferredElementType.Name}");
                }
                
                foreach (var item in element.EnumerateArray())
                {
                    try
                    {
                        // 尝试根据$type属性确定具体类型
                        GameObject gameObject = null;
                        
                        if (item.ValueKind == JsonValueKind.Object && (item.TryGetProperty("$type", out var typeProperty) || item.TryGetProperty("__type", out typeProperty)))
                        {
                            var typeName = typeProperty.GetString();
                            if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableGameObjectListConverterLog)
                            {
                                System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] 找到类型元数据: {typeName}");
                            }
                            
                            var type = GetGameObjectType(typeName);
                            if (type != null)
                            {
                                if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableGameObjectListConverterLog)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] 使用类型 {type.Name} 进行反序列化");
                                }
                                
                                // 🔥 特殊处理：对于IdealTendencyKind，确保跳过__type属性
                                if (type == typeof(IdealTendencyKind))
                                {
                                    gameObject = DeserializeIdealTendencyKind(item);
                                    if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableGameObjectListConverterLog)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] ✅ 使用专用方法创建IdealTendencyKind对象");
                                    }
                                }
                                else
                                {
                                    gameObject = JsonSerializer.Deserialize(item.GetRawText(), JsonTypeInfoHelper.Resolve(options, type)) as GameObject;
                                }
                                
                                if (gameObject != null)
                                {
                                    if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableGameObjectListConverterLog)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] ✅ 成功创建 {type.Name} 对象，ID: {gameObject.ID}, Name: {gameObject.Name}");
                                    }
                                }
                                else
                                {
                                    // 这是异常情况，总是输出
                                    System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] ❌ 反序列化返回null");
                                }
                            }
                            else
                            {
                                // 这是异常情况，总是输出
                                System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] ❌ 无法解析类型: {typeName}");
                            }
                        }
                        
                        // 🔥 2026-03-13 根本修复：如果没有$type属性，使用推断的元素类型
                        if (gameObject == null && inferredElementType != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] 🔥 使用推断类型 {inferredElementType.Name} 进行反序列化");
                            
                            if (inferredElementType == typeof(IdealTendencyKind))
                            {
                                gameObject = DeserializeIdealTendencyKind(item);
                                System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] ✅ 创建 IdealTendencyKind: ID={gameObject.ID}, Name={gameObject.Name}, Type={gameObject.GetType().Name}");
                            }
                            else
                            {
                                gameObject = JsonSerializer.Deserialize(item.GetRawText(), JsonTypeInfoHelper.Resolve(options, inferredElementType)) as GameObject;
                            }
                        }
                        
                        // 如果没有$type属性且没有推断类型，尝试通用反序列化
                        if (gameObject == null)
                        {
                            if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableGameObjectListConverterLog)
                            {
                                System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] 没有类型元数据，尝试推断类型");
                            }
                            // 根据属性推断类型
                            gameObject = InferAndDeserializeGameObject(item, options);
                        }
                        
                        if (gameObject != null)
                        {
                            list.GameObjects.Add(gameObject);
                            if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableGameObjectListConverterLog)
                            {
                                System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] ✅ 添加对象到列表: {gameObject.GetType().Name}");
                            }
                        }
                        else
                        {
                            // gameObject 仍为 null：既没有 $type，inferredElementType 也为 null
                            // 说明这个列表类型没有在 inferredElementType switch 中注册
                            var rawText2 = item.GetRawText();
                            throw new InvalidOperationException(
                                $"[GameObjectListConverter] 无法反序列化对象：列表类型 '{list.GetType().Name}' 未在 inferredElementType switch 中注册，且对象无 $type 字段。JSON片段: {rawText2[..Math.Min(200, rawText2.Length)]}");
                        }
                    }
                    catch (Exception ex)
                    {
                        // 🔥 根本修复：不再吞掉单个对象的异常
                        // 原实现"单个对象失败不中断整个列表"导致列表不完整，后续访问触发 IndexOutOfRangeException，
                        // 掩盖了真正的 FormatException 根本原因。
                        var rawText = item.GetRawText();
                        System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] ❌ 项目转换失败，重新抛出: {ex.GetType().Name}: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter]   JSON片段: {rawText[..Math.Min(400, rawText.Length)]}");
                        throw; // 暴露真正的根本原因
                    }
                }
            }
            else if (element.ValueKind == JsonValueKind.Object)
            {
                if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableGameObjectListConverterLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] 处理单个对象");
                }
                // 单个对象
                try
                {
                    var gameObject = InferAndDeserializeGameObject(element, options);
                    if (gameObject != null)
                    {
                        list.GameObjects.Add(gameObject);
                        if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableGameObjectListConverterLog)
                        {
                            System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] ✅ 添加单个对象到列表: {gameObject.GetType().Name}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 🔥 根本修复：不再吞掉单对象异常，重新抛出暴露根本原因
                    System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] ❌ 单对象转换失败，重新抛出: {ex.GetType().Name}: {ex.Message}");
                    if (ex.InnerException != null)
                        System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter]   InnerException: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                    throw;
                }
            }
        }

        private GameObject InferAndDeserializeGameObject(JsonElement element, JsonSerializerOptions options)
        {
            // 根据JSON属性推断GameObject类型
            if (element.TryGetProperty("$type", out var typeProperty) || element.TryGetProperty("__type", out typeProperty))
            {
                var typeName = typeProperty.GetString();
                var type = GetGameObjectType(typeName);
                if (type != null)
                {
                    // 🔥 不 catch：如果有 $type 但反序列化失败，说明数据损坏，应该 Fail Fast
                    return JsonSerializer.Deserialize(element.GetRawText(), JsonTypeInfoHelper.Resolve(options, type)) as GameObject;
                }
                // $type 存在但无法解析 → 数据损坏，Fail Fast
                var raw = element.GetRawText();
                throw new InvalidOperationException(
                    $"[GameObjectListConverter] 无法解析 $type='{typeName}'，该类型未在 GetGameObjectType 中注册。JSON片段: {raw[..Math.Min(200, raw.Length)]}");
            }

            // 没有 $type 字段 → 返回 null，由调用方（inferredElementType 路径）处理
            // 不在此处抛异常：CommonData.json 里的 Kind 配置类本来就没有 $type
            return null;
        }


        private IdealTendencyKind DeserializeIdealTendencyKind(JsonElement element)
        {
            var idealTendencyKind = new IdealTendencyKind();
            
            foreach (var property in element.EnumerateObject())
            {
                switch (property.Name)
                {
                    case "ID":
                        if (property.Value.ValueKind == JsonValueKind.Number)
                        {
                            idealTendencyKind.ID = property.Value.GetInt32();
                        }
                        break;
                    case "Name":
                        if (property.Value.ValueKind == JsonValueKind.String)
                        {
                            idealTendencyKind.Name = property.Value.GetString();
                        }
                        break;
                    case "Offset":
                        if (property.Value.ValueKind == JsonValueKind.Number)
                        {
                            idealTendencyKind.Offset = property.Value.GetInt32();
                        }
                        break;
                    case "__type":
                        // 跳过类型元数据
                        break;
                    default:
                        if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableGameObjectListConverterLog)
                        {
                            System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] IdealTendencyKind未知属性: {property.Name}");
                        }
                        break;
                }
            }
            
            if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableGameObjectListConverterLog)
            {
                System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] 创建IdealTendencyKind: ID={idealTendencyKind.ID}, Name={idealTendencyKind.Name}, Offset={idealTendencyKind.Offset}");
            }
            return idealTendencyKind;
        }

        private Type GetGameObjectType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableGameObjectListConverterLog)
            {
                System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] 解析类型名称: {typeName}");
            }

            // 移除命名空间前缀，只保留类名。处理多种可能的格式：
            // 1. "Namespace.ClassName"
            // 2. "ClassName:#Namespace" (Legacy DataContract format)
            // 3. "Namespace.ClassName, AssemblyName" (Assembly-qualified name)
            string className = typeName;
            
            // 🔥 修复：首先处理程序集限定名称，去除程序集名
            // 格式: "Namespace.ClassName, AssemblyName"
            if (typeName.Contains(", "))
            {
                className = typeName.Substring(0, typeName.IndexOf(", "));
                if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableGameObjectListConverterLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] 去除程序集名后: {className}");
                }
            }
            
            if (className.Contains(":#"))
            {
                className = className.Substring(0, className.IndexOf(":#"));
                if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableGameObjectListConverterLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] 从Legacy格式提取类名: {className}");
                }
            }
            else if (className.Contains('.'))
            {
                className = className.Substring(className.LastIndexOf('.') + 1);
                if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableGameObjectListConverterLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] 从命名空间格式提取类名: {className}");
                }
            }

            var resolvedType = className switch
            {
                "Person" => typeof(Person),
                "Architecture" => typeof(Architecture),
                "Faction" => typeof(Faction),
                "Troop" => typeof(Troop),
                "Legion" => typeof(Legion),
                "Military" => typeof(Military),
                "Treasure" => typeof(Treasure),
                "Information" => typeof(Information),
                "Routeway" => typeof(Routeway),
                "Facility" => typeof(Facility),
                "Captive" => typeof(Captive),
                "TroopEvent" => typeof(TroopEvent),
                "Event" => typeof(Event),
                "InformationKind" => typeof(InformationKind),
                "IdealTendencyKind" => typeof(IdealTendencyKind),
                "AttackDefaultKind" => typeof(AttackDefaultKind),
                "AttackTargetKind" => typeof(AttackTargetKind),
                "CastDefaultKind" => typeof(CastDefaultKind),
                "CastTargetKind" => typeof(CastTargetKind),
                "CharacterKind" => typeof(CharacterKind),
                "PersonGeneratorType" => typeof(PersonGeneratorType),
                "TrainPolicy" => typeof(TrainPolicy),
                "TreasureCreationSetting" => typeof(TreasureCreationSetting),
                "YearTableEntry" => typeof(YearTableEntry),
                // 🔥 添加缺失的类型
                "State" => typeof(global::GameObjects.ArchitectureDetail.State),
                "Region" => typeof(global::GameObjects.ArchitectureDetail.Region),
                "Section" => typeof(global::GameObjects.Section),
                _ => null
            };

            if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableGameObjectListConverterLog)
            {
                System.Diagnostics.Debug.WriteLine($"[GameObjectListConverter] 类名 '{className}' 解析为类型: {resolvedType?.Name ?? "null"}");
            }
            return resolvedType;
        }
        

    }
}

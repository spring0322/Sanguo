using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// 测试 GameObjectListConverter 是否会导致无限递归
/// </summary>
class TestConverterRecursion
{
    static void Main()
    {
        Console.WriteLine("测试 GameObjectListConverter 递归问题");
        Console.WriteLine();
        
        // 测试1：新格式（直接数组）
        Console.WriteLine("测试1：新格式（直接数组）");
        string newFormatJson = @"[{""ID"":0,""Name"":""Test1""},{""ID"":1,""Name"":""Test2""}]";
        TestDeserialization(newFormatJson);
        Console.WriteLine();
        
        // 测试2：旧格式（GameObjectList 包装）
        Console.WriteLine("测试2：旧格式（GameObjectList 包装）");
        string oldFormatJson = @"{""GameObjects"":[{""ID"":0,""Name"":""Test1""},{""ID"":1,""Name"":""Test2""}]}";
        TestDeserialization(oldFormatJson);
        Console.WriteLine();
        
        // 测试3：嵌套对象
        Console.WriteLine("测试3：嵌套对象（模拟 GameScenarioDTO）");
        string nestedJson = @"{""Persons"":{""GameObjects"":[{""ID"":0,""Name"":""Person1""}]}}";
        TestNestedDeserialization(nestedJson);
    }
    
    static void TestDeserialization(string json)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new TestGameObjectListConverter<TestDTO>() }
            };
            
            var result = JsonSerializer.Deserialize<List<TestDTO>>(json, options);
            Console.WriteLine($"  ✅ 反序列化成功，元素数量: {result?.Count ?? 0}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ 反序列化失败: {ex.Message}");
        }
    }
    
    static void TestNestedDeserialization(string json)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new TestGameObjectListConverter<TestDTO>() }
            };
            
            var result = JsonSerializer.Deserialize<TestScenarioDTO>(json, options);
            Console.WriteLine($"  ✅ 反序列化成功，Persons 数量: {result?.Persons?.Count ?? 0}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ 反序列化失败: {ex.Message}");
        }
    }
}

// 测试用 DTO
class TestDTO
{
    public int ID { get; set; }
    public string? Name { get; set; }
}

class TestScenarioDTO
{
    [JsonConverter(typeof(TestGameObjectListConverter<TestDTO>))]
    public List<TestDTO>? Persons { get; set; }
}

// 测试用转换器（与实际代码相同）
class TestGameObjectListConverter<T> : JsonConverter<List<T>>
{
    private static int _recursionDepth = 0;
    
    public override List<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        _recursionDepth++;
        Console.WriteLine($"    [递归深度: {_recursionDepth}] TokenType: {reader.TokenType}");
        
        if (_recursionDepth > 10)
        {
            throw new InvalidOperationException("检测到无限递归！");
        }
        
        try
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                Console.WriteLine($"    [递归深度: {_recursionDepth}] 处理数组格式");
                List<T>? result = JsonSerializer.Deserialize<List<T>>(ref reader, options);
                return result ?? [];
            }
            
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                Console.WriteLine($"    [递归深度: {_recursionDepth}] 处理对象格式");
                using JsonDocument doc = JsonDocument.ParseValue(ref reader);
                if (doc.RootElement.TryGetProperty("GameObjects", out JsonElement gameObjects))
                {
                    Console.WriteLine($"    [递归深度: {_recursionDepth}] 提取 GameObjects 数组");
                    List<T>? result = JsonSerializer.Deserialize<List<T>>(gameObjects.GetRawText(), options);
                    return result ?? [];
                }
                
                return [];
            }
            
            return [];
        }
        finally
        {
            _recursionDepth--;
        }
    }
    
    public override void Write(Utf8JsonWriter writer, List<T> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}

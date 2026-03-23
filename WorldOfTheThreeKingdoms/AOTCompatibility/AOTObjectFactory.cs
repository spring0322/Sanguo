using GameObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.AOTCompatibility
{
    /// <summary>
    /// AOT兼容的对象工厂
    /// 替换反射依赖的对象创建，使用编译时类型发现
    /// </summary>
    public static class AOTObjectFactory
    {
        private static readonly Dictionary<string, Func<GameObject>> _objectCreators;
        private static readonly Dictionary<Type, Func<GameObject>> _typeCreators;
        private static readonly AOTCompatibilityAdapter _adapter;

        static AOTObjectFactory()
        {
            _objectCreators = new Dictionary<string, Func<GameObject>>();
            _typeCreators = new Dictionary<Type, Func<GameObject>>();
            _adapter = new AOTCompatibilityAdapter();
            
            RegisterDefaultCreators();
        }

        /// <summary>
        /// 注册默认的对象创建器
        /// </summary>
        private static void RegisterDefaultCreators()
        {
            Debug.WriteLine("[AOTObjectFactory] 注册默认对象创建器");

            // 按类型名注册
            _objectCreators["Faction"] = () => new Faction();
            _objectCreators["Person"] = () => new Person();
            _objectCreators["Architecture"] = () => new Architecture();
            _objectCreators["Troop"] = () => new Troop();
            _objectCreators["Legion"] = () => new Legion();

            // 按类型注册
            _typeCreators[typeof(Faction)] = () => new Faction();
            _typeCreators[typeof(Person)] = () => new Person();
            _typeCreators[typeof(Architecture)] = () => new Architecture();
            _typeCreators[typeof(Troop)] = () => new Troop();
            _typeCreators[typeof(Legion)] = () => new Legion();

            Debug.WriteLine($"[AOTObjectFactory] 已注册 {_objectCreators.Count} 个对象创建器");
        }

        /// <summary>
        /// 根据类型名创建对象
        /// </summary>
        /// <param name="typeName">类型名</param>
        /// <returns>创建的对象</returns>
        public static GameObject CreateByTypeName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                Debug.WriteLine("[AOTObjectFactory] 类型名为空");
                return null;
            }

            try
            {
                if (_objectCreators.TryGetValue(typeName, out var creator))
                {
                    var obj = creator();
                    _adapter.InitializeObject(obj);
                    
                    Debug.WriteLine($"[AOTObjectFactory] 成功创建对象: {typeName}[{obj.ID}]");
                    return obj;
                }

                Debug.WriteLine($"[AOTObjectFactory] 不支持的类型名: {typeName}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AOTObjectFactory] 创建对象失败 {typeName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 根据类型创建对象
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>创建的对象</returns>
        public static GameObject CreateByType(Type type)
        {
            if (type == null)
            {
                Debug.WriteLine("[AOTObjectFactory] 类型为null");
                return null;
            }

            try
            {
                if (_typeCreators.TryGetValue(type, out var creator))
                {
                    var obj = creator();
                    _adapter.InitializeObject(obj);
                    
                    Debug.WriteLine($"[AOTObjectFactory] 成功创建对象: {type.Name}[{obj.ID}]");
                    return obj;
                }

                // 尝试使用适配器创建
                var adapterObj = _adapter.CreateInstance(type);
                if (adapterObj != null)
                {
                    Debug.WriteLine($"[AOTObjectFactory] 使用适配器创建对象: {type.Name}[{adapterObj.ID}]");
                    return adapterObj;
                }

                Debug.WriteLine($"[AOTObjectFactory] 不支持的类型: {type.Name}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AOTObjectFactory] 创建对象失败 {type.Name}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 创建泛型对象
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <returns>创建的对象</returns>
        public static T Create<T>() where T : GameObject, new()
        {
            try
            {
                var obj = _adapter.CreateInstance<T>();
                Debug.WriteLine($"[AOTObjectFactory] 成功创建泛型对象: {typeof(T).Name}[{obj.ID}]");
                return obj;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AOTObjectFactory] 创建泛型对象失败 {typeof(T).Name}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 批量创建对象
        /// </summary>
        /// <param name="typeName">类型名</param>
        /// <param name="count">创建数量</param>
        /// <returns>创建的对象列表</returns>
        public static List<GameObject> CreateBatch(string typeName, int count)
        {
            var objects = new List<GameObject>();

            try
            {
                for (int i = 0; i < count; i++)
                {
                    var obj = CreateByTypeName(typeName);
                    if (obj != null)
                    {
                        objects.Add(obj);
                    }
                }

                Debug.WriteLine($"[AOTObjectFactory] 批量创建完成: {typeName} x {objects.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AOTObjectFactory] 批量创建失败 {typeName}: {ex.Message}");
            }

            return objects;
        }

        /// <summary>
        /// 异步创建对象
        /// </summary>
        /// <param name="typeName">类型名</param>
        /// <returns>创建的对象</returns>
        public static async Task<GameObject> CreateAsync(string typeName)
        {
            return await Task.Run(() => CreateByTypeName(typeName));
        }

        /// <summary>
        /// 异步批量创建对象
        /// </summary>
        /// <param name="typeName">类型名</param>
        /// <param name="count">创建数量</param>
        /// <returns>创建的对象列表</returns>
        public static async Task<List<GameObject>> CreateBatchAsync(string typeName, int count)
        {
            return await Task.Run(() => CreateBatch(typeName, count));
        }

        /// <summary>
        /// 注册自定义对象创建器
        /// </summary>
        /// <param name="typeName">类型名</param>
        /// <param name="creator">创建器函数</param>
        public static void RegisterCreator(string typeName, Func<GameObject> creator)
        {
            if (!string.IsNullOrEmpty(typeName) && creator != null)
            {
                _objectCreators[typeName] = creator;
                Debug.WriteLine($"[AOTObjectFactory] 注册自定义创建器: {typeName}");
            }
        }

        /// <summary>
        /// 注册自定义类型创建器
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="creator">创建器函数</param>
        public static void RegisterCreator(Type type, Func<GameObject> creator)
        {
            if (type != null && creator != null)
            {
                _typeCreators[type] = creator;
                Debug.WriteLine($"[AOTObjectFactory] 注册自定义类型创建器: {type.Name}");
            }
        }

        /// <summary>
        /// 检查是否支持指定类型
        /// </summary>
        /// <param name="typeName">类型名</param>
        /// <returns>是否支持</returns>
        public static bool IsSupported(string typeName)
        {
            return !string.IsNullOrEmpty(typeName) && _objectCreators.ContainsKey(typeName);
        }

        /// <summary>
        /// 检查是否支持指定类型
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>是否支持</returns>
        public static bool IsSupported(Type type)
        {
            return type != null && (_typeCreators.ContainsKey(type) || _adapter.IsAOTCompatible(type));
        }

        /// <summary>
        /// 获取支持的类型列表
        /// </summary>
        /// <returns>支持的类型名列表</returns>
        public static List<string> GetSupportedTypes()
        {
            return new List<string>(_objectCreators.Keys);
        }

        /// <summary>
        /// 获取工厂统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        public static string GetStats()
        {
            return $"AOT对象工厂: 支持 {_objectCreators.Count} 种类型的对象创建";
        }
    }
}
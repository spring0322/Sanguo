using GameObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace WorldOfTheThreeKingdoms.AOTCompatibility
{
    /// <summary>
    /// AOT兼容性适配器实现
    /// 提供编译时类型发现和对象创建，替换反射依赖
    /// </summary>
    public class AOTCompatibilityAdapter : IAOTCompatibilityAdapter
    {
        private readonly Dictionary<Type, ITypeHandler> _typeHandlers;
        private readonly Dictionary<string, Type> _typeNameMap;

        public AOTCompatibilityAdapter()
        {
            _typeHandlers = new Dictionary<Type, ITypeHandler>();
            _typeNameMap = new Dictionary<string, Type>();
            
            RegisterTypeHandlers();
        }

        /// <summary>
        /// 注册默认的类型处理器
        /// </summary>
        public void RegisterTypeHandlers()
        {
            Debug.WriteLine("[AOTCompatibilityAdapter] 开始注册类型处理器");

            // 注册主要游戏对象类型的处理器
            RegisterHandler(new FactionTypeHandler());
            RegisterHandler(new PersonTypeHandler());
            RegisterHandler(new ArchitectureTypeHandler());
            RegisterHandler(new TroopTypeHandler());
            RegisterHandler(new LegionTypeHandler());

            Debug.WriteLine($"[AOTCompatibilityAdapter] 已注册 {_typeHandlers.Count} 个类型处理器");
        }

        /// <summary>
        /// 注册单个类型处理器
        /// </summary>
        /// <param name="handler">类型处理器</param>
        public void RegisterHandler(ITypeHandler handler)
        {
            if (handler?.HandledType != null)
            {
                _typeHandlers[handler.HandledType] = handler;
                _typeNameMap[handler.HandledType.Name] = handler.HandledType;
                _typeNameMap[handler.HandledType.FullName] = handler.HandledType;
                
                Debug.WriteLine($"[AOTCompatibilityAdapter] 注册类型处理器: {handler.HandledType.Name}");
            }
        }

        /// <summary>
        /// 创建泛型游戏对象实例
        /// </summary>
        public T CreateInstance<T>() where T : GameObject, new()
        {
            try
            {
                var instance = new T();
                InitializeObject(instance);
                return instance;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AOTCompatibilityAdapter] 创建实例失败 {typeof(T).Name}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 创建指定类型的游戏对象实例
        /// </summary>
        public GameObject CreateInstance(Type type)
        {
            if (_typeHandlers.TryGetValue(type, out var handler))
            {
                try
                {
                    var instance = handler.CreateInstance();
                    handler.InitializeObject(instance);
                    return instance;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AOTCompatibilityAdapter] 使用处理器创建实例失败 {type.Name}: {ex.Message}");
                }
            }

            // 回退到基本创建方法
            try
            {
                if (type == typeof(Faction))
                    return CreateInstance<Faction>();
                if (type == typeof(Person))
                    return CreateInstance<Person>();
                if (type == typeof(Architecture))
                    return CreateInstance<Architecture>();
                if (type == typeof(Troop))
                    return CreateInstance<Troop>();
                if (type == typeof(Legion))
                    return CreateInstance<Legion>();

                Debug.WriteLine($"[AOTCompatibilityAdapter] 不支持的类型: {type.Name}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AOTCompatibilityAdapter] 创建实例失败 {type.Name}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 初始化游戏对象
        /// </summary>
        public void InitializeObject<T>(T obj) where T : GameObject
        {
            if (obj == null) return;

            try
            {
                var type = obj.GetType();
                if (_typeHandlers.TryGetValue(type, out var handler))
                {
                    handler.InitializeObject(obj);
                }
                else
                {
                    // 基本初始化
                    if (obj.ID <= 0)
                    {
                        obj.ID = GenerateId();
                    }
                }

                Debug.WriteLine($"[AOTCompatibilityAdapter] 初始化对象完成: {type.Name}[{obj.ID}]");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AOTCompatibilityAdapter] 初始化对象失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取类型的属性信息
        /// </summary>
        public List<AOTPropertyInfo> GetTypeProperties(Type type)
        {
            if (_typeHandlers.TryGetValue(type, out var handler))
            {
                return handler.GetProperties();
            }

            // 返回基本属性信息
            return new List<AOTPropertyInfo>
            {
                new AOTPropertyInfo { Name = "ID", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Name", PropertyType = typeof(string), CanRead = true, CanWrite = true, IsPublic = true }
            };
        }

        /// <summary>
        /// 设置对象属性值
        /// </summary>
        public bool SetPropertyValue(object obj, string propertyName, object value)
        {
            if (obj is not GameObject gameObj) return false;

            var type = obj.GetType();
            if (_typeHandlers.TryGetValue(type, out var handler))
            {
                return handler.SetProperty(gameObj, propertyName, value);
            }

            // 基本属性设置
            try
            {
                switch (propertyName)
                {
                    case "ID":
                        if (value is int id)
                        {
                            gameObj.ID = id;
                            return true;
                        }
                        break;
                    case "Name":
                        if (value is string name)
                        {
                            gameObj.Name = name;
                            return true;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AOTCompatibilityAdapter] 设置属性失败 {propertyName}: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// 获取对象属性值
        /// </summary>
        public object GetPropertyValue(object obj, string propertyName)
        {
            if (obj is not GameObject gameObj) return null;

            var type = obj.GetType();
            if (_typeHandlers.TryGetValue(type, out var handler))
            {
                return handler.GetProperty(gameObj, propertyName);
            }

            // 基本属性获取
            try
            {
                switch (propertyName)
                {
                    case "ID":
                        return gameObj.ID;
                    case "Name":
                        return gameObj.Name;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AOTCompatibilityAdapter] 获取属性失败 {propertyName}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 检查类型是否支持AOT
        /// </summary>
        public bool IsAOTCompatible(Type type)
        {
            // 检查是否有注册的处理器
            if (_typeHandlers.ContainsKey(type))
            {
                return true;
            }

            // 检查是否是支持的基础类型
            if (typeof(GameObject).IsAssignableFrom(type))
            {
                return type == typeof(Faction) || 
                       type == typeof(Person) || 
                       type == typeof(Architecture) || 
                       type == typeof(Troop) || 
                       type == typeof(Legion);
            }

            // 检查基础类型
            return type.IsPrimitive || 
                   type == typeof(string) || 
                   type == typeof(DateTime) ||
                   type.IsEnum;
        }

        /// <summary>
        /// 获取已注册的类型处理器
        /// </summary>
        public Dictionary<Type, ITypeHandler> GetRegisteredHandlers()
        {
            return new Dictionary<Type, ITypeHandler>(_typeHandlers);
        }

        /// <summary>
        /// 根据类型名获取类型
        /// </summary>
        /// <param name="typeName">类型名</param>
        /// <returns>类型</returns>
        public Type GetTypeByName(string typeName)
        {
            if (_typeNameMap.TryGetValue(typeName, out var type))
            {
                return type;
            }

            return null;
        }

        /// <summary>
        /// 生成唯一ID
        /// </summary>
        private int GenerateId()
        {
            return Math.Abs(Guid.NewGuid().GetHashCode());
        }

        /// <summary>
        /// 获取适配器统计信息
        /// </summary>
        public string GetStats()
        {
            return $"AOT兼容性适配器: 已注册 {_typeHandlers.Count} 个类型处理器";
        }
    }
}
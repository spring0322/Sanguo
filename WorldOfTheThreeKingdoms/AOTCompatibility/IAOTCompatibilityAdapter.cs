using GameObjects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.AOTCompatibility
{
    /// <summary>
    /// AOT兼容性适配器接口
    /// 提供替换反射依赖的AOT友好方法
    /// </summary>
    public interface IAOTCompatibilityAdapter
    {
        /// <summary>
        /// 注册类型处理器
        /// </summary>
        void RegisterTypeHandlers();

        /// <summary>
        /// 创建游戏对象实例
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <returns>创建的实例</returns>
        T CreateInstance<T>() where T : GameObject, new();

        /// <summary>
        /// 创建指定类型的游戏对象实例
        /// </summary>
        /// <param name="type">对象类型</param>
        /// <returns>创建的实例</returns>
        GameObject CreateInstance(Type type);

        /// <summary>
        /// 初始化游戏对象
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="obj">要初始化的对象</param>
        void InitializeObject<T>(T obj) where T : GameObject;

        /// <summary>
        /// 获取类型的属性信息
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>属性信息列表</returns>
        List<AOTPropertyInfo> GetTypeProperties(Type type);

        /// <summary>
        /// 设置对象属性值
        /// </summary>
        /// <param name="obj">目标对象</param>
        /// <param name="propertyName">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns>设置是否成功</returns>
        bool SetPropertyValue(object obj, string propertyName, object value);

        /// <summary>
        /// 获取对象属性值
        /// </summary>
        /// <param name="obj">目标对象</param>
        /// <param name="propertyName">属性名</param>
        /// <returns>属性值</returns>
        object GetPropertyValue(object obj, string propertyName);

        /// <summary>
        /// 检查类型是否支持AOT
        /// </summary>
        /// <param name="type">要检查的类型</param>
        /// <returns>是否支持AOT</returns>
        bool IsAOTCompatible(Type type);

        /// <summary>
        /// 获取已注册的类型处理器
        /// </summary>
        /// <returns>类型处理器字典</returns>
        Dictionary<Type, ITypeHandler> GetRegisteredHandlers();
    }

    /// <summary>
    /// AOT属性信息
    /// </summary>
    public class AOTPropertyInfo
    {
        public string Name { get; set; } = string.Empty;
        public Type PropertyType { get; set; }
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public bool IsPublic { get; set; }
        public object DefaultValue { get; set; }
    }

    /// <summary>
    /// 类型处理器接口
    /// </summary>
    public interface ITypeHandler
    {
        /// <summary>
        /// 处理的类型
        /// </summary>
        Type HandledType { get; }

        /// <summary>
        /// 创建实例
        /// </summary>
        /// <returns>创建的实例</returns>
        GameObject CreateInstance();

        /// <summary>
        /// 初始化对象
        /// </summary>
        /// <param name="obj">要初始化的对象</param>
        void InitializeObject(GameObject obj);

        /// <summary>
        /// 获取属性信息
        /// </summary>
        /// <returns>属性信息列表</returns>
        List<AOTPropertyInfo> GetProperties();

        /// <summary>
        /// 设置属性值
        /// </summary>
        /// <param name="obj">目标对象</param>
        /// <param name="propertyName">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns>设置是否成功</returns>
        bool SetProperty(GameObject obj, string propertyName, object value);

        /// <summary>
        /// 获取属性值
        /// </summary>
        /// <param name="obj">目标对象</param>
        /// <param name="propertyName">属性名</param>
        /// <returns>属性值</returns>
        object GetProperty(GameObject obj, string propertyName);
    }
}
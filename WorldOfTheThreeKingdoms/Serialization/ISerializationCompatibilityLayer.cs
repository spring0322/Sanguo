using GameObjects;
using System;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.Serialization
{
    /// <summary>
    /// 序列化兼容性层接口
    /// 提供AOT兼容的序列化方案
    /// </summary>
    public interface ISerializationCompatibilityLayer
    {
        /// <summary>
        /// 序列化游戏对象到JSON字符串
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="obj">要序列化的对象</param>
        /// <returns>JSON字符串</returns>
        Task<string> SerializeAsync<T>(T obj) where T : GameObject;

        /// <summary>
        /// 从JSON字符串反序列化游戏对象
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="json">JSON字符串</param>
        /// <returns>反序列化的对象</returns>
        Task<T> DeserializeAsync<T>(string json) where T : GameObject;

        /// <summary>
        /// 验证序列化兼容性
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="obj">要验证的对象</param>
        /// <returns>验证是否成功</returns>
        Task<bool> ValidateSerializationAsync<T>(T obj) where T : GameObject;

        /// <summary>
        /// 序列化对象到JSON字符串（非泛型版本）
        /// </summary>
        /// <param name="obj">要序列化的对象</param>
        /// <param name="type">对象类型</param>
        /// <returns>JSON字符串</returns>
        Task<string> SerializeAsync(object obj, Type type);

        /// <summary>
        /// 从JSON字符串反序列化对象（非泛型版本）
        /// </summary>
        /// <param name="json">JSON字符串</param>
        /// <param name="type">目标类型</param>
        /// <returns>反序列化的对象</returns>
        Task<object> DeserializeAsync(string json, Type type);

        /// <summary>
        /// 检查类型是否支持AOT序列化
        /// </summary>
        /// <param name="type">要检查的类型</param>
        /// <returns>是否支持</returns>
        bool IsAOTCompatible(Type type);

        /// <summary>
        /// 获取序列化统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        SerializationStats GetStats();
    }

    /// <summary>
    /// 序列化统计信息
    /// </summary>
    public class SerializationStats
    {
        public int SuccessfulSerializations { get; set; }
        public int FailedSerializations { get; set; }
        public int SuccessfulDeserializations { get; set; }
        public int FailedDeserializations { get; set; }
        public TimeSpan TotalSerializationTime { get; set; }
        public TimeSpan TotalDeserializationTime { get; set; }
        public DateTime LastOperationTime { get; set; }

        public double SuccessRate => 
            (SuccessfulSerializations + SuccessfulDeserializations) > 0 
                ? (double)(SuccessfulSerializations + SuccessfulDeserializations) / 
                  (SuccessfulSerializations + FailedSerializations + SuccessfulDeserializations + FailedDeserializations) 
                : 0.0;

        public override string ToString()
        {
            return $"Serialization Stats: {SuccessfulSerializations} successful serializations, " +
                   $"{SuccessfulDeserializations} successful deserializations, " +
                   $"{FailedSerializations + FailedDeserializations} failures, " +
                   $"Success Rate: {SuccessRate:P2}";
        }
    }
}
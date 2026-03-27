using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.Serialization
{
    /// <summary>
    /// Implementation of asynchronous JSON serialization service using System.Text.Json.
    /// Integrates with GameJsonContext for AOT compatibility.
    /// </summary>
    public class AsyncSerializationService : IAsyncSerializationService
    {
        private readonly JsonSerializerOptions _options;
        
        /// <summary>
        /// Initializes a new instance of AsyncSerializationService with GameJsonContext.
        /// </summary>
        /// <param name="context">The source-generated JSON context for AOT compatibility</param>
        public AsyncSerializationService(GameJsonContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            
            _options = new JsonSerializerOptions
            {
                TypeInfoResolver = context,
                WriteIndented = true,
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                IncludeFields = true,
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };
        }

        private JsonTypeInfo<T> ResolveTypeInfo<T>()
        {
            JsonTypeInfo typeInfo = _options.GetTypeInfo(typeof(T));
            if (typeInfo is not JsonTypeInfo<T> typedTypeInfo)
            {
                throw new InvalidOperationException($"AOT metadata not registered for type: {typeof(T).FullName}");
            }

            return typedTypeInfo;
        }
        
        /// <summary>
        /// Serializes an object to a JSON string asynchronously.
        /// Uses MemoryStream for intermediate processing to enable true async operation.
        /// </summary>
        public async Task<string> SerializeToStringAsync<T>(
            T value, 
            CancellationToken cancellationToken = default)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(
                stream, 
                value, 
                ResolveTypeInfo<T>(),
                cancellationToken)
                .ConfigureAwait(false);
            
            return Encoding.UTF8.GetString(stream.ToArray());
        }
        
        /// <summary>
        /// Deserializes a JSON string to an object asynchronously.
        /// Converts string to MemoryStream for async deserialization.
        /// </summary>
        public async Task<T> DeserializeFromStringAsync<T>(
            string json, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentException("JSON string cannot be null or empty", nameof(json));
            
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            T? result = await JsonSerializer.DeserializeAsync(
                stream, 
                ResolveTypeInfo<T>(),
                cancellationToken)
                .ConfigureAwait(false);
            if (result == null)
            {
                throw new InvalidOperationException($"System.Text.Json 反序列化 {typeof(T).Name} 返回 null");
            }

            return result;
        }
        
        /// <summary>
        /// Serializes an object to a stream asynchronously.
        /// Uses JsonSerializer.SerializeAsync directly with stream for optimal performance.
        /// </summary>
        public async Task SerializeToStreamAsync<T>(
            Stream stream, 
            T value, 
            CancellationToken cancellationToken = default)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            
            await JsonSerializer.SerializeAsync(
                stream, 
                value, 
                ResolveTypeInfo<T>(),
                cancellationToken)
                .ConfigureAwait(false);
        }
        
        /// <summary>
        /// Deserializes an object from a stream asynchronously.
        /// Uses JsonSerializer.DeserializeAsync directly with stream for optimal performance.
        /// </summary>
        public async Task<T> DeserializeFromStreamAsync<T>(
            Stream stream, 
            CancellationToken cancellationToken = default)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            
            T? result = await JsonSerializer.DeserializeAsync(
                stream, 
                ResolveTypeInfo<T>(),
                cancellationToken)
                .ConfigureAwait(false);
            if (result == null)
            {
                throw new InvalidOperationException($"System.Text.Json 反序列化 {typeof(T).Name} 返回 null");
            }

            return result;
        }
    }
}

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
                _options, 
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
            return await JsonSerializer.DeserializeAsync<T>(
                stream, 
                _options, 
                cancellationToken)
                .ConfigureAwait(false);
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
                _options, 
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
            
            return await JsonSerializer.DeserializeAsync<T>(
                stream, 
                _options, 
                cancellationToken)
                .ConfigureAwait(false);
        }
    }
}

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.Serialization
{
    /// <summary>
    /// Interface for asynchronous JSON serialization operations.
    /// Provides true async methods for serializing and deserializing objects using System.Text.Json.
    /// </summary>
    public interface IAsyncSerializationService
    {
        /// <summary>
        /// Serializes an object to a JSON string asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of object to serialize</typeparam>
        /// <param name="value">The object to serialize</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>A task that represents the asynchronous operation, containing the JSON string</returns>
        Task<string> SerializeToStringAsync<T>(
            T value, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Deserializes a JSON string to an object asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of object to deserialize to</typeparam>
        /// <param name="json">The JSON string to deserialize</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>A task that represents the asynchronous operation, containing the deserialized object</returns>
        Task<T> DeserializeFromStringAsync<T>(
            string json, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Serializes an object to a stream asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of object to serialize</typeparam>
        /// <param name="stream">The stream to write to</param>
        /// <param name="value">The object to serialize</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task SerializeToStreamAsync<T>(
            Stream stream, 
            T value, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Deserializes an object from a stream asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of object to deserialize to</typeparam>
        /// <param name="stream">The stream to read from</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>A task that represents the asynchronous operation, containing the deserialized object</returns>
        Task<T> DeserializeFromStreamAsync<T>(
            Stream stream, 
            CancellationToken cancellationToken = default);
    }
}

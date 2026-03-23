using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.Helpers
{
    /// <summary>
    /// Interface for asynchronous file I/O operations.
    /// Provides true async methods for reading and writing files.
    /// </summary>
    public interface IAsyncFileService
    {
        /// <summary>
        /// Reads all text from a file asynchronously.
        /// </summary>
        /// <param name="path">The file path to read from</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>A task that represents the asynchronous operation, containing the file contents</returns>
        Task<string> ReadAllTextAsync(
            string path, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Writes all text to a file asynchronously.
        /// </summary>
        /// <param name="path">The file path to write to</param>
        /// <param name="content">The content to write</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task WriteAllTextAsync(
            string path, 
            string content, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Reads all bytes from a file asynchronously.
        /// </summary>
        /// <param name="path">The file path to read from</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>A task that represents the asynchronous operation, containing the file bytes</returns>
        Task<byte[]> ReadAllBytesAsync(
            string path, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Writes all bytes to a file asynchronously.
        /// </summary>
        /// <param name="path">The file path to write to</param>
        /// <param name="content">The bytes to write</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task WriteAllBytesAsync(
            string path, 
            byte[] content, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Opens a file stream for reading asynchronously.
        /// </summary>
        /// <param name="path">The file path to open</param>
        /// <returns>A task that represents the asynchronous operation, containing the read stream</returns>
        Task<Stream> OpenReadStreamAsync(string path);
        
        /// <summary>
        /// Opens a file stream for writing asynchronously.
        /// </summary>
        /// <param name="path">The file path to open</param>
        /// <returns>A task that represents the asynchronous operation, containing the write stream</returns>
        Task<Stream> OpenWriteStreamAsync(string path);
    }
}

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.Helpers
{
    /// <summary>
    /// Implementation of asynchronous file I/O service.
    /// Wraps native async file operations from System.IO.
    /// </summary>
    public class AsyncFileService : IAsyncFileService
    {
        /// <summary>
        /// Reads all text from a file asynchronously using File.ReadAllTextAsync.
        /// </summary>
        public async Task<string> ReadAllTextAsync(
            string path, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));
            
            return await File.ReadAllTextAsync(path, cancellationToken)
                .ConfigureAwait(false);
        }
        
        /// <summary>
        /// Writes all text to a file asynchronously using File.WriteAllTextAsync.
        /// </summary>
        public async Task WriteAllTextAsync(
            string path, 
            string content, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));
            
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            
            await File.WriteAllTextAsync(path, content, cancellationToken)
                .ConfigureAwait(false);
        }
        
        /// <summary>
        /// Reads all bytes from a file asynchronously using File.ReadAllBytesAsync.
        /// </summary>
        public async Task<byte[]> ReadAllBytesAsync(
            string path, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));
            
            return await File.ReadAllBytesAsync(path, cancellationToken)
                .ConfigureAwait(false);
        }
        
        /// <summary>
        /// Writes all bytes to a file asynchronously using File.WriteAllBytesAsync.
        /// </summary>
        public async Task WriteAllBytesAsync(
            string path, 
            byte[] content, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));
            
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            
            await File.WriteAllBytesAsync(path, content, cancellationToken)
                .ConfigureAwait(false);
        }
        
        /// <summary>
        /// Opens a file stream for reading with async support.
        /// Creates FileStream with useAsync: true parameter for optimal async performance.
        /// </summary>
        public Task<Stream> OpenReadStreamAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));
            
            return Task.FromResult<Stream>(
                new FileStream(
                    path, 
                    FileMode.Open, 
                    FileAccess.Read, 
                    FileShare.Read, 
                    bufferSize: 4096, 
                    useAsync: true));
        }
        
        /// <summary>
        /// Opens a file stream for writing with async support.
        /// Creates FileStream with useAsync: true parameter for optimal async performance.
        /// </summary>
        public Task<Stream> OpenWriteStreamAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));
            
            return Task.FromResult<Stream>(
                new FileStream(
                    path, 
                    FileMode.Create, 
                    FileAccess.Write, 
                    FileShare.None, 
                    bufferSize: 4096, 
                    useAsync: true));
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using GameObjects;
using WorldOfTheThreeKingdoms.Helpers;
using WorldOfTheThreeKingdoms.Serialization;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// Service for true async game save/load operations.
    /// Uses stream-based serialization for optimal performance with large game files.
    /// </summary>
    public class SaveGameService : ISaveGameService
    {
        private readonly IAsyncSerializationService _serialization;
        private readonly IAsyncFileService _fileService;
        
        /// <summary>
        /// Initializes a new instance of SaveGameService with required dependencies.
        /// </summary>
        /// <param name="serialization">The async serialization service</param>
        /// <param name="fileService">The async file service</param>
        /// <exception cref="ArgumentNullException">Thrown when any dependency is null</exception>
        public SaveGameService(
            IAsyncSerializationService serialization,
            IAsyncFileService fileService)
        {
            _serialization = serialization ?? throw new ArgumentNullException(nameof(serialization));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        }
        
        /// <summary>
        /// Saves game data asynchronously using stream-based serialization.
        /// Uses ConfigureAwait(false) to prevent deadlocks in library code.
        /// </summary>
        /// <typeparam name="T">The type of game data to save</typeparam>
        /// <param name="gameData">The game data to save</param>
        /// <param name="savePath">The path where to save the game data</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>A task representing the async save operation</returns>
        /// <exception cref="ArgumentNullException">Thrown when gameData or savePath is null</exception>
        /// <exception cref="ArgumentException">Thrown when savePath is empty or whitespace</exception>
        /// <exception cref="OperationCanceledException">Thrown when cancellation is requested</exception>
        public async Task SaveGameAsync<T>(
            T gameData, 
            string savePath, 
            CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (gameData == null)
                throw new ArgumentNullException(nameof(gameData));
            
            if (string.IsNullOrWhiteSpace(savePath))
                throw new ArgumentException("Save path cannot be null or empty", nameof(savePath));
            
            try
            {
                // Use stream-based approach for large game saves to reduce memory usage
                using var stream = await _fileService.OpenWriteStreamAsync(savePath)
                    .ConfigureAwait(false);
                
                await _serialization.SerializeToStreamAsync(
                    stream, 
                    gameData, 
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Re-throw cancellation exceptions without wrapping
                throw;
            }
            catch (Exception ex)
            {
                // Wrap other exceptions with context
                throw new InvalidOperationException(
                    $"Failed to save game data to '{savePath}': {ex.Message}", 
                    ex);
            }
        }
        
        /// <summary>
        /// Loads game data asynchronously using stream-based deserialization.
        /// Uses ConfigureAwait(false) to prevent deadlocks in library code.
        /// </summary>
        /// <typeparam name="T">The type of game data to load</typeparam>
        /// <param name="savePath">The path from where to load the game data</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>A task representing the async load operation with the loaded game data</returns>
        /// <exception cref="ArgumentException">Thrown when savePath is null, empty or whitespace</exception>
        /// <exception cref="OperationCanceledException">Thrown when cancellation is requested</exception>
        public async Task<T> LoadGameAsync<T>(
            string savePath, 
            CancellationToken cancellationToken = default)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(savePath))
                throw new ArgumentException("Save path cannot be null or empty", nameof(savePath));
            
            try
            {
                // Use stream-based approach for large game loads to reduce memory usage
                using var stream = await _fileService.OpenReadStreamAsync(savePath)
                    .ConfigureAwait(false);
                
                return await _serialization.DeserializeFromStreamAsync<T>(
                    stream, 
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Re-throw cancellation exceptions without wrapping
                throw;
            }
            catch (Exception ex)
            {
                // Wrap other exceptions with context
                throw new InvalidOperationException(
                    $"Failed to load game data from '{savePath}': {ex.Message}", 
                    ex);
            }
        }
    }
}
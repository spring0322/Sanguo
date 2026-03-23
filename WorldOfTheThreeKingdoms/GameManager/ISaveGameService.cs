using System.Threading;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// Interface for true async game save/load operations.
    /// Provides methods for saving and loading game state asynchronously.
    /// </summary>
    public interface ISaveGameService
    {
        /// <summary>
        /// Saves game data asynchronously to the specified path.
        /// </summary>
        /// <typeparam name="T">The type of game data to save</typeparam>
        /// <param name="gameData">The game data to save</param>
        /// <param name="savePath">The path where to save the game data</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>A task representing the async save operation</returns>
        Task SaveGameAsync<T>(
            T gameData, 
            string savePath, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Loads game data asynchronously from the specified path.
        /// </summary>
        /// <typeparam name="T">The type of game data to load</typeparam>
        /// <param name="savePath">The path from where to load the game data</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>A task representing the async load operation with the loaded game data</returns>
        Task<T> LoadGameAsync<T>(
            string savePath, 
            CancellationToken cancellationToken = default);
    }
}
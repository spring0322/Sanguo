using System;

namespace WorldOfTheThreeKingdoms.Serialization.DTOs
{
    /// <summary>
    /// Metadata for save files
    /// Stored in a separate .meta file for fast loading of save list
    /// </summary>
    public class SaveMetadata
    {
        /// <summary>
        /// Scenario name
        /// </summary>
        public string ScenarioName { get; set; }
        
        /// <summary>
        /// Time when the save was created
        /// </summary>
        public DateTime SaveTime { get; set; }
        
        /// <summary>
        /// Current turn number in the game
        /// </summary>
        public int CurrentTurn { get; set; }
        
        /// <summary>
        /// Game version that created this save
        /// </summary>
        public string GameVersion { get; set; }
        
        /// <summary>
        /// ID of the player's faction
        /// </summary>
        public int PlayerFactionID { get; set; }
        
        /// <summary>
        /// Name of the player's faction
        /// </summary>
        public string PlayerFactionName { get; set; }
        
        /// <summary>
        /// Current game date (year)
        /// </summary>
        public int Year { get; set; }
        
        /// <summary>
        /// Current game date (month)
        /// </summary>
        public int Month { get; set; }
        
        /// <summary>
        /// Current game date (day)
        /// </summary>
        public int Day { get; set; }
        
        /// <summary>
        /// Total playtime in seconds
        /// </summary>
        public int GameTime { get; set; }
        
        /// <summary>
        /// MOD name if applicable
        /// </summary>
        public string MOD { get; set; }
    }
}

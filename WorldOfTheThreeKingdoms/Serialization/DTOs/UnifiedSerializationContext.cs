using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorldOfTheThreeKingdoms.Serialization.DTOs
{
    /// <summary>
    /// JsonSerializerContext for the unified JSON serialization architecture
    /// Provides AOT compilation support for all DTO types
    /// </summary>
    [JsonSourceGenerationOptions(
        WriteIndented = false,  // Minimize file size
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,  // Use camelCase for JSON properties
        GenerationMode = JsonSourceGenerationMode.Default,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,  // Don't serialize null values
        IncludeFields = false,  // Only serialize properties, not fields
        PropertyNameCaseInsensitive = true,  // Allow case-insensitive deserialization
        AllowTrailingCommas = true,  // Be lenient with JSON format
        ReadCommentHandling = JsonCommentHandling.Skip,  // Skip comments in JSON
        NumberHandling = JsonNumberHandling.AllowReadingFromString  // Allow numbers as strings
    )]
    
    // Core DTO types
    [JsonSerializable(typeof(GameScenarioDTO))]
    [JsonSerializable(typeof(PersonDTO))]
    [JsonSerializable(typeof(FactionDTO))]
    [JsonSerializable(typeof(ArchitectureDTO))]
    [JsonSerializable(typeof(CityDTO))]
    [JsonSerializable(typeof(PortDTO))]
    [JsonSerializable(typeof(GateDTO))]
    [JsonSerializable(typeof(TreasureDTO))]
    [JsonSerializable(typeof(LegionDTO))]
    [JsonSerializable(typeof(TroopDTO))]
    [JsonSerializable(typeof(SectionDTO))]
    [JsonSerializable(typeof(RegionDTO))]
    [JsonSerializable(typeof(RoutewayDTO))]
    [JsonSerializable(typeof(MilitaryDTO))]
    [JsonSerializable(typeof(FacilityDTO))]
    [JsonSerializable(typeof(InformationDTO))]
    [JsonSerializable(typeof(TroopEventDTO))]
    [JsonSerializable(typeof(CaptiveDTO))]
    [JsonSerializable(typeof(EventDTO))]
    [JsonSerializable(typeof(PersonIDRelationDTO))]
    [JsonSerializable(typeof(SaveMetadata))]
    
    // Collection types - explicitly declare for AOT
    [JsonSerializable(typeof(List<PersonDTO>))]
    [JsonSerializable(typeof(List<FactionDTO>))]
    [JsonSerializable(typeof(List<ArchitectureDTO>))]
    [JsonSerializable(typeof(List<CityDTO>))]
    [JsonSerializable(typeof(List<PortDTO>))]
    [JsonSerializable(typeof(List<GateDTO>))]
    [JsonSerializable(typeof(List<TreasureDTO>))]
    [JsonSerializable(typeof(List<LegionDTO>))]
    [JsonSerializable(typeof(List<TroopDTO>))]
    [JsonSerializable(typeof(List<SectionDTO>))]
    [JsonSerializable(typeof(List<RegionDTO>))]
    [JsonSerializable(typeof(List<RoutewayDTO>))]
    [JsonSerializable(typeof(List<MilitaryDTO>))]
    [JsonSerializable(typeof(List<FacilityDTO>))]
    [JsonSerializable(typeof(List<InformationDTO>))]
    [JsonSerializable(typeof(List<TroopEventDTO>))]
    [JsonSerializable(typeof(List<CaptiveDTO>))]
    [JsonSerializable(typeof(List<EventDTO>))]
    [JsonSerializable(typeof(List<PersonIDRelationDTO>))]
    
    // Primitive collection types
    [JsonSerializable(typeof(List<int>))]
    [JsonSerializable(typeof(List<string>))]
    [JsonSerializable(typeof(int[]))]
    [JsonSerializable(typeof(string[]))]
    
    // Dictionary types
    [JsonSerializable(typeof(Dictionary<int, int>))]
    [JsonSerializable(typeof(Dictionary<int, int[]>))]
    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    
    public partial class UnifiedSerializationContext : JsonSerializerContext
    {
        /// <summary>
        /// Get default serialization options for saving
        /// </summary>
        public static JsonSerializerOptions GetSaveOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = false,  // Compact JSON for smaller file size
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                TypeInfoResolver = Default,  // Use source-generated context
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
        }
        
        /// <summary>
        /// Get default deserialization options for loading
        /// </summary>
        public static JsonSerializerOptions GetLoadOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                TypeInfoResolver = Default,  // Use source-generated context
                DefaultIgnoreCondition = JsonIgnoreCondition.Never  // Read all properties
            };
        }
        
        /// <summary>
        /// Get options for metadata files (human-readable)
        /// </summary>
        public static JsonSerializerOptions GetMetadataOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,  // Pretty-print for readability
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                TypeInfoResolver = Default,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
        }
    }
}

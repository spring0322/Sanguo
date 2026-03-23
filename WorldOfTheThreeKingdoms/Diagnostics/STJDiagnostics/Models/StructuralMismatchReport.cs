using System.Collections.Generic;

namespace WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Models
{
    /// <summary>
    /// Report detailing structural mismatches between JSON data and expected types
    /// </summary>
    public class StructuralMismatchReport
    {
        public bool HasMismatches { get; set; }
        public List<StructuralMismatch> Mismatches { get; set; } = new List<StructuralMismatch>();
        public JsonStructureAnalysis JsonAnalysis { get; set; }
        public TypeStructureAnalysis TypeAnalysis { get; set; }
    }

    /// <summary>
    /// Individual structural mismatch
    /// </summary>
    public class StructuralMismatch
    {
        public string PropertyPath { get; set; }
        public string ExpectedType { get; set; }
        public string ActualType { get; set; }
        public MismatchType Type { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// Analysis of JSON structure
    /// </summary>
    public class JsonStructureAnalysis
    {
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public int Depth { get; set; }
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Analysis of expected type structure
    /// </summary>
    public class TypeStructureAnalysis
    {
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
        public List<string> RequiredProperties { get; set; } = new List<string>();
        public List<string> OptionalProperties { get; set; } = new List<string>();
    }

    /// <summary>
    /// Types of structural mismatches
    /// </summary>
    public enum MismatchType
    {
        MissingProperty,
        ExtraProperty,
        TypeMismatch,
        NullValue,
        InvalidFormat
    }
}
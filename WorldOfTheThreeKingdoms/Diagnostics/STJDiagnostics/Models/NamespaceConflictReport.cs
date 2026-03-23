using System;
using System.Collections.Generic;

namespace WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Models
{
    /// <summary>
    /// Report detailing namespace conflicts that could cause type resolution issues
    /// </summary>
    public class NamespaceConflictReport
    {
        public bool HasConflicts { get; set; }
        public List<NamespaceConflict> Conflicts { get; set; } = new List<NamespaceConflict>();
        public Dictionary<string, List<Type>> AmbiguousTypeNames { get; set; } = new Dictionary<string, List<Type>>();
    }

    /// <summary>
    /// Individual namespace conflict
    /// </summary>
    public class NamespaceConflict
    {
        public string TypeName { get; set; }
        public List<string> ConflictingNamespaces { get; set; } = new List<string>();
        public ConflictSeverity Severity { get; set; }
        public string RecommendedResolution { get; set; }
    }

    /// <summary>
    /// Severity of namespace conflicts
    /// </summary>
    public enum ConflictSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
}
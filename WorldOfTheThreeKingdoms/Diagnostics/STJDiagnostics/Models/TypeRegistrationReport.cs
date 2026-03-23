using System;
using System.Collections.Generic;

namespace WorldOfTheThreeKingdoms.Diagnostics.STJDiagnostics.Models
{
    /// <summary>
    /// Report detailing type registration status and dependencies
    /// </summary>
    public class TypeRegistrationReport
    {
        public Dictionary<Type, RegistrationStatus> TypeRegistrations { get; set; } = new Dictionary<Type, RegistrationStatus>();
        public List<Type> MissingRegistrations { get; set; } = new List<Type>();
        public List<Type> CircularDependencies { get; set; } = new List<Type>();
        public Dictionary<Type, List<Type>> DependencyChain { get; set; } = new Dictionary<Type, List<Type>>();
    }

    /// <summary>
    /// Status of type registration in JsonSerializerContext
    /// </summary>
    public enum RegistrationStatus
    {
        Registered,
        Missing,
        Ambiguous,
        CircularDependency
    }
}
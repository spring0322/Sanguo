# STJ Deserialization Diagnostic System

## Overview

This diagnostic system provides comprehensive analysis and resolution capabilities for System.Text.Json (STJ) deserialization failures, specifically targeting the persistent EventEffectKindTable deserialization issues in the WorldOfTheThreeKingdoms game system.

## Architecture

The system is organized into the following components:

### Core Interfaces

- **IRootCauseAnalyzer**: Systematically diagnoses STJ deserialization failures
- **ITypeRegistrationValidator**: Validates STJ type registrations and dependencies
- **IDeserializationTester**: Provides isolated testing capabilities
- **IFallbackHandler**: Handles graceful error recovery
- **IDiagnosticSystem**: Comprehensive monitoring and health checking

### Data Models

- **DiagnosticResult**: Comprehensive analysis results
- **TypeRegistrationReport**: Type registration status and dependencies
- **NamespaceConflictReport**: Namespace conflict detection results
- **StructuralMismatchReport**: JSON structure validation results
- **ValidationResult**: General validation outcomes
- **FixRecommendation**: Actionable fix recommendations
- **TestResult**: Testing outcomes and metrics
- **SystemHealthReport**: Overall system health status
- **ErrorContext**: Comprehensive error context capture

## Directory Structure

```
WorldOfTheThreeKingdoms/Diagnostics/STJDiagnostics/
├── Interfaces/
│   ├── IRootCauseAnalyzer.cs
│   ├── ITypeRegistrationValidator.cs
│   ├── IDeserializationTester.cs
│   ├── IFallbackHandler.cs
│   └── IDiagnosticSystem.cs
├── Models/
│   ├── DiagnosticResult.cs
│   ├── TypeRegistrationReport.cs
│   ├── NamespaceConflictReport.cs
│   ├── StructuralMismatchReport.cs
│   ├── ValidationResult.cs
│   ├── FixRecommendation.cs
│   ├── TestResult.cs
│   ├── SystemHealthReport.cs
│   └── ErrorContext.cs
├── Implementations/ (to be created in subsequent tasks)
└── README.md
```

## Testing Framework

The system uses NUnit with FsCheck.NUnit for comprehensive testing:

- **Unit Tests**: Specific scenario validation
- **Property-Based Tests**: General correctness properties
- **Integration Tests**: End-to-end workflow validation

### Test Project Structure

```
STJDiagnosticsTests/
├── Infrastructure/
│   ├── TestBase.cs
│   └── TestArbitraries.cs
├── RootCauseAnalyzerTests.cs
├── TypeRegistrationValidatorTests.cs
├── DeserializationTesterTests.cs
├── FallbackHandlerTests.cs
├── DiagnosticSystemTests.cs
├── GlobalUsings.cs
└── STJDiagnosticsTests.csproj
```

## Requirements Mapping

Each interface and component is designed to validate specific requirements:

- **Requirements 1.x**: Root cause analysis capabilities
- **Requirements 2.x**: Type registration validation
- **Requirements 3.x**: Isolated deserialization testing
- **Requirements 4.x**: Comprehensive error handling
- **Requirements 5.x**: Permanent fix implementation
- **Requirements 6.x**: Diagnostic and monitoring system

## Next Steps

1. Implement core analyzer classes (Task 2)
2. Implement validation components (Task 3)
3. Implement testing capabilities (Task 5)
4. Implement error handling (Task 6)
5. Implement monitoring system (Task 7)
6. Integrate all components (Task 9)
7. Apply specific EventEffectKindTable fixes (Task 10)

## Usage

The diagnostic system will be integrated into the main game system to:

1. Automatically detect STJ deserialization failures
2. Provide comprehensive root cause analysis
3. Offer actionable fix recommendations
4. Enable graceful error recovery
5. Monitor system health and prevent regression

This system ensures that the EventEffectKindTable deserialization issue is permanently resolved while providing a robust framework for preventing and diagnosing future STJ-related issues.
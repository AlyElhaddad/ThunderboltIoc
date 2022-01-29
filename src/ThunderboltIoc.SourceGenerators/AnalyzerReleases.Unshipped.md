; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md
### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
TB000 | AnalyzerError | Info | AnalyzerException
TB001 | Design | Info | RegistrationNotAttached
TB102 | Design | Warning | MissingPartialModifier
TB103 | Design | Warning | MissingRegistration
TB104 | Design | Warning | GenerationFailureForType
TB205 | Design | Error | NoSuitableConstructor
TB206 | Design | Error | CyclicDependencies
TB207 | Design | Error | TopLevelRegistration
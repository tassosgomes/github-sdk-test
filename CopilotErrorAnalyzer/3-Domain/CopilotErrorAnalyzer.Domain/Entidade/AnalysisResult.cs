using System.Collections.Generic;

namespace CopilotErrorAnalyzer.Domain.Entities;

public class AnalysisResult
{
    public required ErrorClassification Classification { get; init; }
    public required string RootCause { get; init; }
    public required string Summary { get; init; }
    public required List<string> Suggestions { get; init; }
    public string? RawResponse { get; init; }
}

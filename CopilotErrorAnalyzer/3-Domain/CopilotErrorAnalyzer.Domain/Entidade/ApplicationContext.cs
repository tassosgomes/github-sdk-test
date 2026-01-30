using System.Collections.Generic;

namespace CopilotErrorAnalyzer.Domain.Entities;

public record ApplicationContext(
    string? ApplicationName,
    string? Version,
    string? Environment,
    Dictionary<string, string>? Metadata
);

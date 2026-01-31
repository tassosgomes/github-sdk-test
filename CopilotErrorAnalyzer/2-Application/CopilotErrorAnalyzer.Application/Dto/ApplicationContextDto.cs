using System.Collections.Generic;

namespace CopilotErrorAnalyzer.Application.Dto;

public record ApplicationContextDto(
    string? ApplicationName,
    string? Version,
    string? Environment,
    Dictionary<string, string>? Metadata
);

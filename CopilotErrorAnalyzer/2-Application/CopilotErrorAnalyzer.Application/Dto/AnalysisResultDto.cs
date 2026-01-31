using System.Collections.Generic;

namespace CopilotErrorAnalyzer.Application.Dto;

public record AnalysisResultDto(
    string Classification,
    string RootCause,
    string Summary,
    List<string> Suggestions
);

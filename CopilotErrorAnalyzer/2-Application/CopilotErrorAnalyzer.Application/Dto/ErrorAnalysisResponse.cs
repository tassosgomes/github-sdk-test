using System;

namespace CopilotErrorAnalyzer.Application.Dto;

public record ErrorAnalysisResponse(
    string Id,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    AnalysisResultDto? Result
);

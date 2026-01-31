namespace CopilotErrorAnalyzer.Application.Dto;

public record AnalyzeErrorResponse(
    string Id,
    string Status,
    string StatusUrl
);

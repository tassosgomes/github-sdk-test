using System;

namespace CopilotErrorAnalyzer.Domain.Entities;

public class ErrorEvent
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public required string Source { get; init; }
    public required string Message { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public string? StackTrace { get; init; }
    public KubernetesContext? KubernetesContext { get; init; }
    public ApplicationContext? ApplicationContext { get; init; }
    public AnalysisStatus Status { get; set; } = AnalysisStatus.Pending;
    public AnalysisResult? Result { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
}

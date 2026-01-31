using System.Collections.Generic;

namespace CopilotErrorAnalyzer.Application.Dto;

public record KubernetesContextDto(
    string? Namespace,
    string? PodName,
    string? ContainerName,
    string? NodeName,
    Dictionary<string, string>? Labels,
    string? ManifestYaml
);

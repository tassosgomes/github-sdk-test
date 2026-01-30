using System.Collections.Generic;

namespace CopilotErrorAnalyzer.Domain.Entities;

public record KubernetesContext(
    string? Namespace,
    string? PodName,
    string? ContainerName,
    string? NodeName,
    Dictionary<string, string>? Labels,
    string? ManifestYaml
);

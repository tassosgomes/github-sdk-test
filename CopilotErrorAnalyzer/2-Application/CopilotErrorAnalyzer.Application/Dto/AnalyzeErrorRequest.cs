using System;
using System.ComponentModel.DataAnnotations;

namespace CopilotErrorAnalyzer.Application.Dto;

public record AnalyzeErrorRequest(
    [Required(ErrorMessage = "Source is required")]
    [RegularExpression("^(application|kubernetes)$", ErrorMessage = "Source must be 'application' or 'kubernetes'")]
    string Source,

    [Required(ErrorMessage = "Message is required")]
    [MinLength(1, ErrorMessage = "Message cannot be empty")]
    string Message,

    [Required(ErrorMessage = "Timestamp is required")]
    DateTimeOffset Timestamp,

    string? StackTrace,
    KubernetesContextDto? KubernetesContext,
    ApplicationContextDto? ApplicationContext
);

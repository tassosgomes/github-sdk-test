---
status: completed
parallelizable: true
blocked_by: ["1.0"]
---

<task_context>
<domain>application/dto</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>low</complexity>
<dependencies>domain_entities</dependencies>
<unblocks>["4.0", "5.0"]</unblocks>
</task_context>

# Tarefa 3.0: Criar DTOs e Interfaces da Camada Application

## Visão Geral

Criar os Data Transfer Objects (DTOs) para comunicação com a API e as interfaces de serviço da camada Application. Os DTOs definem o contrato da API e os mappers convertem entre DTOs e entidades do Domain.

<requirements>
- Criar DTOs de request e response conforme Tech Spec
- Criar interfaces para handlers e services
- Usar records para DTOs imutáveis
- Incluir validações via Data Annotations
</requirements>

## Subtarefas

- [x] 3.1 Criar `AnalyzeErrorRequest` DTO com validações
- [x] 3.2 Criar `AnalyzeErrorResponse` DTO
- [x] 3.3 Criar `ErrorAnalysisResponse` DTO
- [x] 3.4 Criar `AnalysisResultDto` DTO
- [x] 3.5 Criar DTOs auxiliares (KubernetesContextDto, ApplicationContextDto)
- [x] 3.6 Criar interface `IAnalyzeErrorCommandHandler`
- [x] 3.7 Criar interface `IGetAnalysisQueryHandler`
- [x] 3.8 Criar interface `IReportGenerator`

## Sequenciamento

- **Bloqueado por:** 1.0 (Domain entities)
- **Desbloqueia:** 4.0 (Handlers), 5.0 (Controller)
- **Paralelizável:** Sim (pode rodar em paralelo com 2.0, 6.0, 8.0)

## Detalhes de Implementação

### DTOs de Request

```csharp
// 2-Application/Dto/AnalyzeErrorRequest.cs
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

// 2-Application/Dto/KubernetesContextDto.cs
public record KubernetesContextDto(
    string? Namespace,
    string? PodName,
    string? ContainerName,
    string? NodeName,
    Dictionary<string, string>? Labels,
    string? ManifestYaml
);

// 2-Application/Dto/ApplicationContextDto.cs
public record ApplicationContextDto(
    string? ApplicationName,
    string? Version,
    string? Environment,
    Dictionary<string, string>? Metadata
);
```

### DTOs de Response

```csharp
// 2-Application/Dto/AnalyzeErrorResponse.cs
namespace CopilotErrorAnalyzer.Application.Dto;

public record AnalyzeErrorResponse(
    string Id,
    string Status,
    string StatusUrl
);

// 2-Application/Dto/ErrorAnalysisResponse.cs
public record ErrorAnalysisResponse(
    string Id,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    AnalysisResultDto? Result
);

// 2-Application/Dto/AnalysisResultDto.cs
public record AnalysisResultDto(
    string Classification,
    string RootCause,
    string Summary,
    List<string> Suggestions
);
```

### Interfaces de Handlers

```csharp
// 2-Application/Interface/IAnalyzeErrorCommandHandler.cs
namespace CopilotErrorAnalyzer.Application.Interface;

public interface IAnalyzeErrorCommandHandler
{
    Task<AnalyzeErrorResponse> HandleAsync(AnalyzeErrorRequest request, CancellationToken ct);
}

// 2-Application/Interface/IGetAnalysisQueryHandler.cs
public interface IGetAnalysisQueryHandler
{
    Task<ErrorAnalysisResponse?> HandleAsync(string id, CancellationToken ct);
}

// 2-Application/Interface/IReportGenerator.cs
public interface IReportGenerator
{
    string GenerateMarkdown(ErrorEvent errorEvent, AnalysisResult result);
}
```

## Critérios de Sucesso

- [x] Todos os DTOs criados conforme Tech Spec
- [x] Validações via Data Annotations funcionando
- [x] Records usados para imutabilidade
- [x] Interfaces definidas para handlers e services
- [x] Projeto compila sem erros
- [x] Namespaces organizados corretamente

## Checklist de Conclusão

- [x] 3.0 Criar DTOs e interfaces da camada Application ✅ CONCLUÍDA
    - [x] 3.1 Implementação completada
    - [x] 3.2 Definição da tarefa, PRD e tech spec validados
    - [x] 3.3 Análise de regras e conformidade verificadas
    - [x] 3.4 Revisão de código completada
    - [x] 3.5 Pronto para deploy

---
status: pending
parallelizable: false
blocked_by: ["2.0", "3.0"]
---

<task_context>
<domain>application/handlers</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>error_store, dtos</dependencies>
<unblocks>["5.0"]</unblocks>
</task_context>

# Tarefa 4.0: Implementar Command e Query Handlers

## Visão Geral

Implementar os handlers que orquestram a lógica de negócio da aplicação. O `AnalyzeErrorCommandHandler` processa novos erros e o `GetAnalysisQueryHandler` recupera análises existentes. Estes handlers são chamados pelo Controller.

<requirements>
- Implementar `AnalyzeErrorCommandHandler` para criar e enfileirar erros
- Implementar `GetAnalysisQueryHandler` para consultar análises
- Mapear entre DTOs e entidades do Domain
- Seguir padrões de Command/Query Separation
</requirements>

## Subtarefas

- [ ] 4.1 Criar classe `AnalyzeErrorCommandHandler`
- [ ] 4.2 Implementar mapeamento de `AnalyzeErrorRequest` para `ErrorEvent`
- [ ] 4.3 Implementar lógica de save e enqueue
- [ ] 4.4 Criar classe `GetAnalysisQueryHandler`
- [ ] 4.5 Implementar mapeamento de `ErrorEvent` para `ErrorAnalysisResponse`
- [ ] 4.6 Registrar handlers no DI container

## Sequenciamento

- **Bloqueado por:** 2.0 (IErrorStore impl), 3.0 (DTOs e interfaces)
- **Desbloqueia:** 5.0 (Controller)
- **Paralelizável:** Não

## Detalhes de Implementação

### AnalyzeErrorCommandHandler

```csharp
// 2-Application/Command/AnalyzeErrorCommandHandler.cs
using CopilotErrorAnalyzer.Application.Dto;
using CopilotErrorAnalyzer.Application.Interface;
using CopilotErrorAnalyzer.Domain.Entidade;
using CopilotErrorAnalyzer.Domain.Interface;

namespace CopilotErrorAnalyzer.Application.Command;

public class AnalyzeErrorCommandHandler : IAnalyzeErrorCommandHandler
{
    private readonly IErrorStore _errorStore;

    public AnalyzeErrorCommandHandler(IErrorStore errorStore)
    {
        _errorStore = errorStore;
    }

    public async Task<AnalyzeErrorResponse> HandleAsync(AnalyzeErrorRequest request, CancellationToken ct)
    {
        var errorEvent = MapToEntity(request);
        
        var id = await _errorStore.SaveAsync(errorEvent, ct);
        await _errorStore.EnqueueForAnalysisAsync(id, ct);

        return new AnalyzeErrorResponse(
            Id: id,
            Status: errorEvent.Status.ToString(),
            StatusUrl: $"/api/errors/{id}"
        );
    }

    private static ErrorEvent MapToEntity(AnalyzeErrorRequest request)
    {
        return new ErrorEvent
        {
            Source = request.Source,
            Message = request.Message,
            Timestamp = request.Timestamp,
            StackTrace = request.StackTrace,
            KubernetesContext = request.KubernetesContext is not null
                ? new KubernetesContext(
                    request.KubernetesContext.Namespace,
                    request.KubernetesContext.PodName,
                    request.KubernetesContext.ContainerName,
                    request.KubernetesContext.NodeName,
                    request.KubernetesContext.Labels,
                    request.KubernetesContext.ManifestYaml)
                : null,
            ApplicationContext = request.ApplicationContext is not null
                ? new ApplicationContext(
                    request.ApplicationContext.ApplicationName,
                    request.ApplicationContext.Version,
                    request.ApplicationContext.Environment,
                    request.ApplicationContext.Metadata)
                : null
        };
    }
}
```

### GetAnalysisQueryHandler

```csharp
// 2-Application/Query/GetAnalysisQueryHandler.cs
using CopilotErrorAnalyzer.Application.Dto;
using CopilotErrorAnalyzer.Application.Interface;
using CopilotErrorAnalyzer.Domain.Entidade;
using CopilotErrorAnalyzer.Domain.Interface;

namespace CopilotErrorAnalyzer.Application.Query;

public class GetAnalysisQueryHandler : IGetAnalysisQueryHandler
{
    private readonly IErrorStore _errorStore;

    public GetAnalysisQueryHandler(IErrorStore errorStore)
    {
        _errorStore = errorStore;
    }

    public async Task<ErrorAnalysisResponse?> HandleAsync(string id, CancellationToken ct)
    {
        var errorEvent = await _errorStore.GetByIdAsync(id, ct);
        
        if (errorEvent is null)
            return null;

        return MapToResponse(errorEvent);
    }

    private static ErrorAnalysisResponse MapToResponse(ErrorEvent errorEvent)
    {
        return new ErrorAnalysisResponse(
            Id: errorEvent.Id,
            Status: errorEvent.Status.ToString(),
            CreatedAt: errorEvent.CreatedAt,
            CompletedAt: errorEvent.CompletedAt,
            Result: errorEvent.Result is not null
                ? new AnalysisResultDto(
                    Classification: errorEvent.Result.Classification.ToString(),
                    RootCause: errorEvent.Result.RootCause,
                    Summary: errorEvent.Result.Summary,
                    Suggestions: errorEvent.Result.Suggestions)
                : null
        );
    }
}
```

### Registro no DI

```csharp
// Program.cs
builder.Services.AddScoped<IAnalyzeErrorCommandHandler, AnalyzeErrorCommandHandler>();
builder.Services.AddScoped<IGetAnalysisQueryHandler, GetAnalysisQueryHandler>();
```

## Critérios de Sucesso

- [ ] Handlers implementados conforme interfaces
- [ ] Mapeamento correto entre DTOs e entidades
- [ ] `AnalyzeErrorCommandHandler` salva e enfileira corretamente
- [ ] `GetAnalysisQueryHandler` retorna null para ID inexistente
- [ ] Handlers registrados no DI como Scoped
- [ ] Testes unitários passam

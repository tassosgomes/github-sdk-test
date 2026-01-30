---
status: completed
parallelizable: false
blocked_by: []
---

<task_context>
<domain>infra/estrutura</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>dotnet_sdk</dependencies>
<unblocks>["2.0", "3.0", "6.0", "8.0"]</unblocks>
</task_context>

# Tarefa 1.0: Criar Estrutura do Projeto e Camada Domain

## Visão Geral

Criar a estrutura completa do projeto .NET 8 seguindo Clean Architecture com 4 camadas numeradas, conforme definido na Tech Spec. Esta tarefa estabelece a fundação para todas as demais tarefas.

<requirements>
- Criar solution `CopilotErrorAnalyzer.sln`
- Criar projetos para cada camada (1-Services, 2-Application, 3-Domain, 4-Infra, 5-Tests)
- Implementar todas as entidades do Domain
- Implementar interfaces do Domain
- Configurar referências entre projetos
</requirements>

## Subtarefas

- [x] 1.1 Criar estrutura de pastas e solution
- [x] 1.2 Criar projeto `CopilotErrorAnalyzer.API` (1-Services)
- [x] 1.3 Criar projeto `CopilotErrorAnalyzer.Application` (2-Application)
- [x] 1.4 Criar projeto `CopilotErrorAnalyzer.Domain` (3-Domain)
- [x] 1.5 Criar projeto `CopilotErrorAnalyzer.Infra` (4-Infra)
- [x] 1.6 Criar projeto `CopilotErrorAnalyzer.UnitTests` (5-Tests)
- [x] 1.7 Implementar entidades do Domain (ErrorEvent, AnalysisResult, etc.)
- [x] 1.8 Implementar enums (ErrorClassification, AnalysisStatus)
- [x] 1.9 Implementar interfaces (IErrorStore, IErrorAnalyzer)
- [x] 1.10 Configurar referências entre projetos

## Checklist de Conclusão

- [x] 1.0 Criar Estrutura do Projeto e Camada Domain ✅ CONCLUÍDA
    - [x] 1.1 Implementação completada
    - [x] 1.2 Definição da tarefa, PRD e tech spec validados
    - [x] 1.3 Análise de regras e conformidade verificadas
    - [x] 1.4 Revisão de código completada
    - [x] 1.5 Pronto para deploy

## Sequenciamento

- **Bloqueado por:** Nenhuma tarefa
- **Desbloqueia:** 2.0, 3.0, 6.0, 8.0
- **Paralelizável:** Não (tarefa inicial)

## Detalhes de Implementação

### Estrutura de Pastas

```
CopilotErrorAnalyzer/
├── CopilotErrorAnalyzer.sln
├── 1-Services/
│   └── CopilotErrorAnalyzer.API/
│       ├── CopilotErrorAnalyzer.API.csproj
│       ├── Program.cs
│       ├── Controllers/
│       └── appsettings.json
├── 2-Application/
│   └── CopilotErrorAnalyzer.Application/
│       ├── CopilotErrorAnalyzer.Application.csproj
│       ├── Dto/
│       ├── Command/
│       ├── Query/
│       ├── Service/
│       └── Interface/
├── 3-Domain/
│   └── CopilotErrorAnalyzer.Domain/
│       ├── CopilotErrorAnalyzer.Domain.csproj
│       ├── Entidade/
│       └── Interface/
├── 4-Infra/
│   └── CopilotErrorAnalyzer.Infra/
│       ├── CopilotErrorAnalyzer.Infra.csproj
│       ├── Store/
│       └── Analyzer/
└── 5-Tests/
    └── CopilotErrorAnalyzer.UnitTests/
        └── CopilotErrorAnalyzer.UnitTests.csproj
```

### Entidades do Domain

```csharp
// ErrorEvent.cs
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

// KubernetesContext.cs
public record KubernetesContext(
    string? Namespace,
    string? PodName,
    string? ContainerName,
    string? NodeName,
    Dictionary<string, string>? Labels,
    string? ManifestYaml
);

// ApplicationContext.cs
public record ApplicationContext(
    string? ApplicationName,
    string? Version,
    string? Environment,
    Dictionary<string, string>? Metadata
);

// AnalysisResult.cs
public class AnalysisResult
{
    public required ErrorClassification Classification { get; init; }
    public required string RootCause { get; init; }
    public required string Summary { get; init; }
    public required List<string> Suggestions { get; init; }
    public string? RawResponse { get; init; }
}
```

### Enums

```csharp
// ErrorClassification.cs
public enum ErrorClassification
{
    KubernetesConfig,
    ApplicationBug,
    Unknown
}

// AnalysisStatus.cs
public enum AnalysisStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}
```

### Interfaces do Domain

```csharp
// IErrorStore.cs
public interface IErrorStore
{
    Task<string> SaveAsync(ErrorEvent errorEvent, CancellationToken ct);
    Task<ErrorEvent?> GetByIdAsync(string id, CancellationToken ct);
    Task UpdateAsync(ErrorEvent errorEvent, CancellationToken ct);
    Task EnqueueForAnalysisAsync(string id, CancellationToken ct);
    Task<string?> DequeueForAnalysisAsync(CancellationToken ct);
}

// IErrorAnalyzer.cs
public interface IErrorAnalyzer
{
    Task<AnalysisResult> AnalyzeAsync(ErrorEvent errorEvent, CancellationToken ct);
}
```

### Referências entre Projetos

| Projeto | Referencia |
|---------|------------|
| API | Application |
| Application | Domain |
| Infra | Domain |
| UnitTests | Application, Domain, Infra |

## Critérios de Sucesso

- [x] Solution compila sem erros (`dotnet build`)
- [x] Estrutura de pastas segue convenção numerada
- [x] Todas as entidades implementadas conforme Tech Spec
- [x] Interfaces definidas no Domain
- [x] Referências entre projetos configuradas corretamente
- [x] Projetos usam .NET 8.0

## Comandos de Validação

```bash
cd CopilotErrorAnalyzer
dotnet build
dotnet test --no-build
```

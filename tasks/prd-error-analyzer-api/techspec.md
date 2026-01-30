# Tech Spec: CopilotErrorAnalyzer API

## Resumo Executivo

Esta especificação detalha a implementação de uma API REST em .NET 8 que utiliza o GitHub Copilot SDK para análise automatizada de erros. A arquitetura segue Clean Architecture com 4 camadas (API, Application, Domain, Infra), processamento assíncrono com armazenamento em memória via `ConcurrentDictionary`, e integração com o Copilot SDK usando o modelo `claude-sonnet-4.5`.

O fluxo principal é: webhook recebe erro → armazena e retorna ID (HTTP 202) → background service processa via Copilot SDK → cliente consulta resultado/relatório via GET.

## Arquitetura do Sistema

### Visão Geral dos Componentes

```
┌─────────────────────────────────────────────────────────────────┐
│                        1-Services                                │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │              ErrorsController                            │    │
│  │  POST /api/errors/analyze → Aceita erro, retorna ID     │    │
│  │  GET  /api/errors/{id}    → Status da análise           │    │
│  │  GET  /api/errors/{id}/report → Relatório Markdown      │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      2-Application                               │
│  ┌──────────────────┐  ┌──────────────────┐  ┌───────────────┐ │
│  │AnalyzeErrorCmd   │  │GetAnalysisQuery  │  │ReportGenerator│ │
│  │Handler           │  │Handler           │  │Service        │ │
│  └──────────────────┘  └──────────────────┘  └───────────────┘ │
│                              │                                   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │            ErrorAnalysisBackgroundService                 │   │
│  │         (processa fila, chama Copilot SDK)               │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                        3-Domain                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌───────────────────────┐ │
│  │ErrorEvent    │  │AnalysisResult│  │ErrorClassification    │ │
│  │(entidade)    │  │(entidade)    │  │(enum)                 │ │
│  └──────────────┘  └──────────────┘  └───────────────────────┘ │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │              Interfaces (IErrorAnalyzer, IErrorStore)     │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                         4-Infra                                  │
│  ┌──────────────────────┐  ┌─────────────────────────────────┐ │
│  │InMemoryErrorStore    │  │CopilotErrorAnalyzer             │ │
│  │(ConcurrentDictionary)│  │(GitHub.Copilot.SDK)             │ │
│  └──────────────────────┘  └─────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

### Fluxo de Dados

1. **POST /api/errors/analyze**: Controller valida payload → cria `ErrorEvent` → salva no `IErrorStore` com status `Pending` → enfileira para processamento → retorna HTTP 202 com ID
2. **Background Service**: Consome fila → chama `IErrorAnalyzer` (Copilot SDK) → atualiza `ErrorEvent` com `AnalysisResult` → status `Completed`
3. **GET /api/errors/{id}**: Busca no `IErrorStore` → retorna status e resultado (se disponível)
4. **GET /api/errors/{id}/report**: Busca análise → `ReportGeneratorService` gera Markdown → retorna `text/markdown`

## Design de Implementação

### Interfaces Principais

```csharp
// Domain/Interface/IErrorStore.cs
public interface IErrorStore
{
    Task<string> SaveAsync(ErrorEvent errorEvent, CancellationToken ct);
    Task<ErrorEvent?> GetByIdAsync(string id, CancellationToken ct);
    Task UpdateAsync(ErrorEvent errorEvent, CancellationToken ct);
    Task EnqueueForAnalysisAsync(string id, CancellationToken ct);
    Task<string?> DequeueForAnalysisAsync(CancellationToken ct);
}

// Domain/Interface/IErrorAnalyzer.cs
public interface IErrorAnalyzer
{
    Task<AnalysisResult> AnalyzeAsync(ErrorEvent errorEvent, CancellationToken ct);
}

// Application/Interface/IReportGenerator.cs
public interface IReportGenerator
{
    string GenerateMarkdown(ErrorEvent errorEvent, AnalysisResult result);
}
```

### Modelos de Dados

```csharp
// Domain/Entidade/ErrorEvent.cs
public class ErrorEvent
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public required string Source { get; init; }           // "application" | "kubernetes"
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

// Domain/Entidade/KubernetesContext.cs
public record KubernetesContext(
    string? Namespace,
    string? PodName,
    string? ContainerName,
    string? NodeName,
    Dictionary<string, string>? Labels,
    string? ManifestYaml
);

// Domain/Entidade/ApplicationContext.cs
public record ApplicationContext(
    string? ApplicationName,
    string? Version,
    string? Environment,
    Dictionary<string, string>? Metadata
);

// Domain/Entidade/AnalysisResult.cs
public class AnalysisResult
{
    public required ErrorClassification Classification { get; init; }
    public required string RootCause { get; init; }
    public required string Summary { get; init; }
    public required List<string> Suggestions { get; init; }
    public string? RawResponse { get; init; }
}

// Domain/Entidade/ErrorClassification.cs
public enum ErrorClassification
{
    KubernetesConfig,
    ApplicationBug,
    Unknown
}

// Domain/Entidade/AnalysisStatus.cs
public enum AnalysisStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}
```

### DTOs de API

```csharp
// Application/Dto/AnalyzeErrorRequest.cs
public record AnalyzeErrorRequest(
    [Required] string Source,
    [Required] string Message,
    [Required] DateTimeOffset Timestamp,
    string? StackTrace,
    KubernetesContextDto? KubernetesContext,
    ApplicationContextDto? ApplicationContext
);

// Application/Dto/AnalyzeErrorResponse.cs
public record AnalyzeErrorResponse(
    string Id,
    string Status,
    string StatusUrl
);

// Application/Dto/ErrorAnalysisResponse.cs
public record ErrorAnalysisResponse(
    string Id,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    AnalysisResultDto? Result
);

// Application/Dto/AnalysisResultDto.cs
public record AnalysisResultDto(
    string Classification,
    string RootCause,
    string Summary,
    List<string> Suggestions
);
```

### Endpoints de API

| Método | Path | Descrição | Request | Response |
|--------|------|-----------|---------|----------|
| `POST` | `/api/errors/analyze` | Submete erro para análise | `AnalyzeErrorRequest` (JSON) | `202 Accepted` + `AnalyzeErrorResponse` |
| `GET` | `/api/errors/{id}` | Consulta status/resultado | - | `200 OK` + `ErrorAnalysisResponse` |
| `GET` | `/api/errors/{id}/report` | Obtém relatório Markdown | - | `200 OK` + `text/markdown` |

### Controller

```csharp
// API/Controllers/ErrorsController.cs
[ApiController]
[Route("api/errors")]
[Produces("application/json")]
public class ErrorsController : ControllerBase
{
    private readonly IAnalyzeErrorCommandHandler _analyzeHandler;
    private readonly IGetAnalysisQueryHandler _getHandler;
    private readonly IReportGenerator _reportGenerator;

    [HttpPost("analyze")]
    [ProducesResponseType(typeof(AnalyzeErrorResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AnalyzeError(
        [FromBody] AnalyzeErrorRequest request,
        CancellationToken ct)
    {
        var result = await _analyzeHandler.HandleAsync(request, ct);
        return AcceptedAtAction(
            nameof(GetAnalysis), 
            new { id = result.Id }, 
            result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ErrorAnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAnalysis(string id, CancellationToken ct)
    {
        var result = await _getHandler.HandleAsync(id, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpGet("{id}/report")]
    [Produces("text/markdown")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReport(string id, CancellationToken ct)
    {
        var analysis = await _getHandler.HandleAsync(id, ct);
        if (analysis?.Result is null) 
            return NotFound(new ProblemDetails { Title = "Analysis not found or not completed" });
        
        var report = _reportGenerator.GenerateMarkdown(/* ... */);
        return Content(report, "text/markdown");
    }
}
```

## Pontos de Integração

### GitHub Copilot SDK

```csharp
// Infra/Analyzer/CopilotErrorAnalyzerService.cs
public class CopilotErrorAnalyzerService : IErrorAnalyzer, IAsyncDisposable
{
    private readonly CopilotClient _client;
    private readonly ILogger<CopilotErrorAnalyzerService> _logger;

    public CopilotErrorAnalyzerService(ILogger<CopilotErrorAnalyzerService> logger)
    {
        _logger = logger;
        _client = new CopilotClient();
    }

    public async Task<AnalysisResult> AnalyzeAsync(ErrorEvent errorEvent, CancellationToken ct)
    {
        await _client.StartAsync();
        
        await using var session = await _client.CreateSessionAsync(new SessionConfig
        {
            Model = "claude-sonnet-4.5",
            SystemMessage = new SystemMessageConfig
            {
                Mode = SystemMessageMode.Append,
                Content = GetSystemPrompt()
            }
        });

        var prompt = BuildAnalysisPrompt(errorEvent);
        var response = new StringBuilder();
        var done = new TaskCompletionSource();

        session.On(evt =>
        {
            if (evt is AssistantMessageEvent msg)
                response.Append(msg.Data.Content);
            else if (evt is SessionIdleEvent)
                done.SetResult();
            else if (evt is SessionErrorEvent err)
                done.SetException(new Exception(err.Data.Message));
        });

        await session.SendAsync(new MessageOptions { Prompt = prompt });
        await done.Task;

        return ParseResponse(response.ToString());
    }

    private string GetSystemPrompt() => """
        Você é um especialista em troubleshooting de erros de aplicação e Kubernetes.
        Ao analisar um erro, você deve:
        1. Identificar se é um erro de configuração do Kubernetes ou bug de código
        2. Determinar a causa raiz provável
        3. Fornecer sugestões de correção específicas e acionáveis
        
        Responda SEMPRE no formato JSON:
        {
            "classification": "KUBERNETES_CONFIG" | "APPLICATION_BUG" | "UNKNOWN",
            "rootCause": "descrição da causa raiz",
            "summary": "resumo em uma frase",
            "suggestions": ["sugestão 1", "sugestão 2"]
        }
        """;

    private string BuildAnalysisPrompt(ErrorEvent error) => $"""
        Analise o seguinte erro e forneça diagnóstico:

        ## Informações do Erro
        - **Fonte**: {error.Source}
        - **Mensagem**: {error.Message}
        - **Timestamp**: {error.Timestamp:O}

        {(error.StackTrace is not null ? $"## Stack Trace\n```\n{error.StackTrace}\n```" : "")}

        {FormatKubernetesContext(error.KubernetesContext)}

        {FormatApplicationContext(error.ApplicationContext)}

        Forneça sua análise no formato JSON especificado.
        """;

    public async ValueTask DisposeAsync() => await _client.StopAsync();
}
```

### Requisitos do Ambiente

| Dependência | Versão | Obrigatório |
|-------------|--------|-------------|
| .NET SDK | 8.0+ | Sim |
| GitHub Copilot CLI | Latest | Sim |
| Copilot Subscription | Ativa | Sim |

## Análise de Impacto

| Componente Afetado | Tipo de Impacto | Descrição & Nível de Risco | Ação Requerida |
|-------------------|-----------------|---------------------------|----------------|
| Novo projeto | Criação | Projeto greenfield, sem impacto em sistemas existentes. Risco: Baixo | Nenhuma |
| GitHub Copilot CLI | Dependência externa | Requer instalação e autenticação prévia. Risco: Médio | Documentar setup |
| Custo Copilot | Billing | Cada análise consome premium requests. Risco: Médio | Monitorar uso |

## Abordagem de Testes

### Testes Unitários

**Componentes a testar:**
- `AnalyzeErrorCommandHandler` - validação de request, criação de entidade
- `ReportGeneratorService` - geração de Markdown
- `CopilotErrorAnalyzerService.ParseResponse` - parsing do JSON de resposta

**Estratégia de Mock:**
- `IErrorStore` - mock para simular armazenamento
- `IErrorAnalyzer` - mock para evitar chamadas reais ao Copilot SDK

```csharp
[Fact]
public async Task HandleAsync_WithValidRequest_ShouldCreateErrorEventAndEnqueue()
{
    // Arrange
    var storeMock = new Mock<IErrorStore>();
    storeMock.Setup(s => s.SaveAsync(It.IsAny<ErrorEvent>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync("test-id-123");
    
    var handler = new AnalyzeErrorCommandHandler(storeMock.Object);
    var request = new AnalyzeErrorRequest("application", "NullReferenceException", DateTimeOffset.UtcNow, null, null, null);

    // Act
    var result = await handler.HandleAsync(request, CancellationToken.None);

    // Assert
    result.Id.Should().Be("test-id-123");
    result.Status.Should().Be("Pending");
    storeMock.Verify(s => s.EnqueueForAnalysisAsync("test-id-123", It.IsAny<CancellationToken>()), Times.Once);
}
```

### Testes de Integração

**Escopo:** Testar fluxo completo com Copilot SDK real (opcional, requer ambiente configurado).

**Localização:** `5-Tests/CopilotErrorAnalyzer.IntegrationTests/`

**Critério de skip:** Pular se `COPILOT_CLI_PATH` não estiver configurado ou CLI não disponível.

## Sequenciamento de Desenvolvimento

### Ordem de Construção

| Fase | Componente | Dependência | Estimativa |
|------|------------|-------------|------------|
| 1 | Estrutura do projeto + Domain | - | 1h |
| 2 | InMemoryErrorStore (Infra) | Domain | 1h |
| 3 | Application handlers + DTOs | Domain, Store | 2h |
| 4 | ErrorsController (API) | Application | 1h |
| 5 | CopilotErrorAnalyzerService | Domain | 2h |
| 6 | BackgroundService | Infra, Application | 1h |
| 7 | ReportGeneratorService | Domain | 1h |
| 8 | Testes unitários | Todos | 2h |
| 9 | Documentação + Swagger | API | 1h |

**Total estimado:** ~12h

### Dependências Técnicas

- [ ] GitHub Copilot CLI instalado no ambiente de desenvolvimento
- [ ] Autenticação Copilot configurada (`copilot auth login`)
- [ ] .NET 8 SDK instalado

## Monitoramento e Observabilidade

### Logs Estruturados

```csharp
// Níveis de log por operação
_logger.LogInformation("Error event received: {ErrorId}, Source: {Source}", id, source);
_logger.LogInformation("Analysis started for {ErrorId}", id);
_logger.LogInformation("Analysis completed for {ErrorId}, Classification: {Classification}", id, classification);
_logger.LogWarning("Analysis failed for {ErrorId}: {Error}", id, error);
```

### Métricas (futuro)

| Métrica | Tipo | Descrição |
|---------|------|-----------|
| `error_analysis_requests_total` | Counter | Total de requisições de análise |
| `error_analysis_duration_seconds` | Histogram | Tempo de processamento |
| `error_analysis_classification` | Counter | Contagem por classificação |

## Considerações Técnicas

### Decisões Principais

| Decisão | Justificativa | Alternativas Rejeitadas |
|---------|--------------|------------------------|
| `ConcurrentDictionary` para storage | Simples, thread-safe, adequado para POC | Redis (complexidade), SQLite (overhead) |
| Background Service com Channel | Pattern nativo .NET, eficiente | Hangfire (overkill), Queue externa (complexidade) |
| Processamento síncrono no SDK | SDK não suporta true async, aguardamos resposta | - |
| JSON para resposta do Copilot | Estruturado, fácil de parsear | Texto livre (difícil extração) |

### Riscos Conhecidos

| Risco | Probabilidade | Impacto | Mitigação |
|-------|--------------|---------|-----------|
| Copilot SDK em preview | Alta | Médio | Isolar integração em camada Infra, fácil substituição |
| Resposta não-JSON do modelo | Média | Médio | Fallback para classificação UNKNOWN, retry com prompt ajustado |
| Perda de dados em memória | Alta | Baixo (POC) | Documentar limitação, evoluir para persistência |
| Latência alta do Copilot | Média | Médio | Logs de duração, considerar timeout em versão futura |

### Conformidade com Padrões

| Regra | Status | Observação |
|-------|--------|------------|
| `dotnet-architecture.md` | ✅ Conforme | Clean Architecture com 4 camadas |
| `dotnet-folders.md` | ✅ Conforme | Estrutura numerada de pastas |
| `dotnet-testing.md` | ✅ Conforme | xUnit + Moq + AwesomeAssertions |
| `restful.md` | ✅ Conforme | RFC 9457 para erros, versionamento via path |

## Estrutura de Pastas Final

```
CopilotErrorAnalyzer/
├── CopilotErrorAnalyzer.sln
├── 1-Services/
│   └── CopilotErrorAnalyzer.API/
│       ├── CopilotErrorAnalyzer.API.csproj
│       ├── Program.cs
│       ├── Controllers/
│       │   └── ErrorsController.cs
│       └── appsettings.json
├── 2-Application/
│   └── CopilotErrorAnalyzer.Application/
│       ├── CopilotErrorAnalyzer.Application.csproj
│       ├── Dto/
│       ├── Command/
│       ├── Query/
│       ├── Service/
│       │   ├── ReportGeneratorService.cs
│       │   └── ErrorAnalysisBackgroundService.cs
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
│       │   └── InMemoryErrorStore.cs
│       └── Analyzer/
│           └── CopilotErrorAnalyzerService.cs
└── 5-Tests/
    └── CopilotErrorAnalyzer.UnitTests/
        ├── CopilotErrorAnalyzer.UnitTests.csproj
        └── Application/
            └── AnalyzeErrorCommandHandlerTests.cs
```

---

**Documento criado em:** 30/01/2026  
**Versão:** 1.0  
**Status:** Aprovado para implementação

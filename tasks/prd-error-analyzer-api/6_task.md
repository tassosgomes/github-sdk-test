---
status: pending
parallelizable: true
blocked_by: ["1.0"]
---

<task_context>
<domain>infra/integration</domain>
<type>integration</type>
<scope>core_feature</scope>
<complexity>high</complexity>
<dependencies>github_copilot_sdk</dependencies>
<unblocks>["7.0"]</unblocks>
</task_context>

# Tarefa 6.0: Implementar CopilotErrorAnalyzerService

## Visão Geral

Implementar a integração com o GitHub Copilot SDK para realizar análise automatizada de erros. Este serviço é a capacidade central do produto e utiliza o modelo `claude-sonnet-4.5` para troubleshooting.

<requirements>
- Implementar `IErrorAnalyzer` usando GitHub Copilot SDK
- Criar sessão com modelo `claude-sonnet-4.5`
- Construir prompt estruturado para análise de erros
- Parsear resposta JSON do modelo
- Implementar tratamento de erros e fallback
</requirements>

## Subtarefas

- [ ] 6.1 Adicionar referência ao pacote GitHub.Copilot.SDK
- [ ] 6.2 Criar classe `CopilotErrorAnalyzerService`
- [ ] 6.3 Implementar lifecycle do `CopilotClient`
- [ ] 6.4 Implementar método `GetSystemPrompt()`
- [ ] 6.5 Implementar método `BuildAnalysisPrompt(ErrorEvent)`
- [ ] 6.6 Implementar método `ParseResponse(string)`
- [ ] 6.7 Implementar método `AnalyzeAsync(ErrorEvent, CancellationToken)`
- [ ] 6.8 Implementar `IAsyncDisposable` para cleanup
- [ ] 6.9 Registrar como Singleton no DI

## Sequenciamento

- **Bloqueado por:** 1.0 (Domain interfaces)
- **Desbloqueia:** 7.0 (Background Service)
- **Paralelizável:** Sim (pode rodar em paralelo com 2.0, 3.0, 8.0)

## Detalhes de Implementação

### CopilotErrorAnalyzerService

```csharp
// 4-Infra/CopilotErrorAnalyzer.Infra/Analyzer/CopilotErrorAnalyzerService.cs
using System.Text;
using System.Text.Json;
using CopilotErrorAnalyzer.Domain.Entidade;
using CopilotErrorAnalyzer.Domain.Interface;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.Logging;

namespace CopilotErrorAnalyzer.Infra.Analyzer;

public class CopilotErrorAnalyzerService : IErrorAnalyzer, IAsyncDisposable
{
    private readonly CopilotClient _client;
    private readonly ILogger<CopilotErrorAnalyzerService> _logger;
    private bool _isStarted;

    public CopilotErrorAnalyzerService(ILogger<CopilotErrorAnalyzerService> logger)
    {
        _logger = logger;
        _client = new CopilotClient();
    }

    public async Task<AnalysisResult> AnalyzeAsync(ErrorEvent errorEvent, CancellationToken ct)
    {
        _logger.LogInformation("Starting analysis for error {ErrorId}", errorEvent.Id);

        if (!_isStarted)
        {
            await _client.StartAsync();
            _isStarted = true;
        }

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
            switch (evt)
            {
                case AssistantMessageEvent msg:
                    response.Append(msg.Data.Content);
                    break;
                case SessionIdleEvent:
                    done.TrySetResult();
                    break;
                case SessionErrorEvent err:
                    _logger.LogError("Copilot session error: {Error}", err.Data.Message);
                    done.TrySetException(new InvalidOperationException(err.Data.Message));
                    break;
            }
        });

        await session.SendAsync(new MessageOptions { Prompt = prompt });
        
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromMinutes(5)); // Timeout de segurança
        
        try
        {
            await done.Task.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Analysis timed out for error {ErrorId}", errorEvent.Id);
            return CreateUnknownResult("Analysis timed out");
        }

        var result = ParseResponse(response.ToString(), errorEvent.Id);
        _logger.LogInformation("Analysis completed for {ErrorId}, Classification: {Classification}", 
            errorEvent.Id, result.Classification);
        
        return result;
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

    private static string FormatKubernetesContext(KubernetesContext? ctx)
    {
        if (ctx is null) return "";
        
        var sb = new StringBuilder("## Contexto Kubernetes\n");
        if (ctx.Namespace is not null) sb.AppendLine($"- **Namespace**: {ctx.Namespace}");
        if (ctx.PodName is not null) sb.AppendLine($"- **Pod**: {ctx.PodName}");
        if (ctx.ContainerName is not null) sb.AppendLine($"- **Container**: {ctx.ContainerName}");
        if (ctx.NodeName is not null) sb.AppendLine($"- **Node**: {ctx.NodeName}");
        if (ctx.ManifestYaml is not null) sb.AppendLine($"\n### Manifest\n```yaml\n{ctx.ManifestYaml}\n```");
        
        return sb.ToString();
    }

    private static string FormatApplicationContext(ApplicationContext? ctx)
    {
        if (ctx is null) return "";
        
        var sb = new StringBuilder("## Contexto da Aplicação\n");
        if (ctx.ApplicationName is not null) sb.AppendLine($"- **Aplicação**: {ctx.ApplicationName}");
        if (ctx.Version is not null) sb.AppendLine($"- **Versão**: {ctx.Version}");
        if (ctx.Environment is not null) sb.AppendLine($"- **Ambiente**: {ctx.Environment}");
        
        return sb.ToString();
    }

    private AnalysisResult ParseResponse(string response, string errorId)
    {
        try
        {
            // Tenta extrair JSON da resposta (pode vir com texto adicional)
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            
            if (jsonStart < 0 || jsonEnd < jsonStart)
            {
                _logger.LogWarning("No JSON found in response for {ErrorId}", errorId);
                return CreateUnknownResult(response);
            }

            var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
            var parsed = JsonSerializer.Deserialize<CopilotResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (parsed is null)
            {
                return CreateUnknownResult(response);
            }

            return new AnalysisResult
            {
                Classification = ParseClassification(parsed.Classification),
                RootCause = parsed.RootCause ?? "Unknown",
                Summary = parsed.Summary ?? "No summary provided",
                Suggestions = parsed.Suggestions ?? new List<string>(),
                RawResponse = response
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON response for {ErrorId}", errorId);
            return CreateUnknownResult(response);
        }
    }

    private static ErrorClassification ParseClassification(string? classification) =>
        classification?.ToUpperInvariant() switch
        {
            "KUBERNETES_CONFIG" => ErrorClassification.KubernetesConfig,
            "APPLICATION_BUG" => ErrorClassification.ApplicationBug,
            _ => ErrorClassification.Unknown
        };

    private static AnalysisResult CreateUnknownResult(string rawResponse) => new()
    {
        Classification = ErrorClassification.Unknown,
        RootCause = "Unable to determine root cause",
        Summary = "Analysis could not be completed",
        Suggestions = new List<string> { "Review the error manually", "Check the raw response for details" },
        RawResponse = rawResponse
    };

    public async ValueTask DisposeAsync()
    {
        if (_isStarted)
        {
            await _client.StopAsync();
        }
    }

    private record CopilotResponse(
        string? Classification,
        string? RootCause,
        string? Summary,
        List<string>? Suggestions
    );
}
```

### Registro no DI

```csharp
// Program.cs
builder.Services.AddSingleton<IErrorAnalyzer, CopilotErrorAnalyzerService>();
```

## Requisitos de Ambiente

- GitHub Copilot CLI instalado
- Autenticação Copilot configurada (`copilot auth login`)
- Subscription GitHub Copilot ativa

## Critérios de Sucesso

- [ ] Serviço compila sem erros
- [ ] Integração com Copilot SDK funcional
- [ ] Prompt estruturado gera análises úteis
- [ ] Parser extrai JSON corretamente
- [ ] Fallback para UNKNOWN em erros de parsing
- [ ] Logging adequado para troubleshooting
- [ ] Timeout implementado para evitar bloqueio indefinido
- [ ] Dispose libera recursos corretamente

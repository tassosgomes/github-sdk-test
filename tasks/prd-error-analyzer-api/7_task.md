---
status: pending
parallelizable: false
blocked_by: ["2.0", "6.0"]
---

<task_context>
<domain>application/background</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>error_store, error_analyzer</dependencies>
<unblocks>["9.0"]</unblocks>
</task_context>

# Tarefa 7.0: Implementar ErrorAnalysisBackgroundService

## Visão Geral

Implementar o serviço de background que consome a fila de erros e processa cada um usando o `IErrorAnalyzer`. Este serviço garante processamento assíncrono, permitindo que a API responda imediatamente com HTTP 202.

<requirements>
- Implementar `BackgroundService` que consome a fila de processamento
- Processar erros sequencialmente (um por vez)
- Atualizar status do erro durante processamento
- Tratar erros e marcar como Failed quando necessário
- Implementar graceful shutdown
</requirements>

## Subtarefas

- [ ] 7.1 Criar classe `ErrorAnalysisBackgroundService` herdando de `BackgroundService`
- [ ] 7.2 Implementar loop de consumo da fila
- [ ] 7.3 Implementar atualização de status para `Processing`
- [ ] 7.4 Implementar chamada ao `IErrorAnalyzer`
- [ ] 7.5 Implementar atualização de status para `Completed`
- [ ] 7.6 Implementar tratamento de erros e status `Failed`
- [ ] 7.7 Implementar logging estruturado
- [ ] 7.8 Registrar como Hosted Service no DI

## Sequenciamento

- **Bloqueado por:** 2.0 (IErrorStore impl), 6.0 (IErrorAnalyzer impl)
- **Desbloqueia:** 9.0 (Testes)
- **Paralelizável:** Não

## Detalhes de Implementação

### ErrorAnalysisBackgroundService

```csharp
// 2-Application/Service/ErrorAnalysisBackgroundService.cs
using CopilotErrorAnalyzer.Domain.Entidade;
using CopilotErrorAnalyzer.Domain.Interface;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CopilotErrorAnalyzer.Application.Service;

public class ErrorAnalysisBackgroundService : BackgroundService
{
    private readonly IErrorStore _errorStore;
    private readonly IErrorAnalyzer _errorAnalyzer;
    private readonly ILogger<ErrorAnalysisBackgroundService> _logger;

    public ErrorAnalysisBackgroundService(
        IErrorStore errorStore,
        IErrorAnalyzer errorAnalyzer,
        ILogger<ErrorAnalysisBackgroundService> logger)
    {
        _errorStore = errorStore;
        _errorAnalyzer = errorAnalyzer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ErrorAnalysisBackgroundService is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var errorId = await _errorStore.DequeueForAnalysisAsync(stoppingToken);
                
                if (errorId is null)
                {
                    continue; // Cancelled or no items
                }

                await ProcessErrorAsync(errorId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in background service");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // Backoff on error
            }
        }

        _logger.LogInformation("ErrorAnalysisBackgroundService is stopping");
    }

    private async Task ProcessErrorAsync(string errorId, CancellationToken ct)
    {
        _logger.LogInformation("Processing error {ErrorId}", errorId);

        var errorEvent = await _errorStore.GetByIdAsync(errorId, ct);
        
        if (errorEvent is null)
        {
            _logger.LogWarning("Error {ErrorId} not found in store", errorId);
            return;
        }

        try
        {
            // Update status to Processing
            errorEvent.Status = AnalysisStatus.Processing;
            await _errorStore.UpdateAsync(errorEvent, ct);
            _logger.LogInformation("Error {ErrorId} status updated to Processing", errorId);

            // Perform analysis
            var result = await _errorAnalyzer.AnalyzeAsync(errorEvent, ct);

            // Update with result
            errorEvent.Result = result;
            errorEvent.Status = AnalysisStatus.Completed;
            errorEvent.CompletedAt = DateTimeOffset.UtcNow;
            await _errorStore.UpdateAsync(errorEvent, ct);

            _logger.LogInformation(
                "Error {ErrorId} analysis completed. Classification: {Classification}",
                errorId, result.Classification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze error {ErrorId}", errorId);

            // Update status to Failed
            errorEvent.Status = AnalysisStatus.Failed;
            errorEvent.CompletedAt = DateTimeOffset.UtcNow;
            await _errorStore.UpdateAsync(errorEvent, ct);
        }
    }
}
```

### Registro no DI

```csharp
// Program.cs
builder.Services.AddHostedService<ErrorAnalysisBackgroundService>();
```

### Logs Estruturados

O serviço utiliza os seguintes templates de log:

| Nível | Evento | Template |
|-------|--------|----------|
| Information | Início | "ErrorAnalysisBackgroundService is starting" |
| Information | Processando | "Processing error {ErrorId}" |
| Information | Status | "Error {ErrorId} status updated to Processing" |
| Information | Completo | "Error {ErrorId} analysis completed. Classification: {Classification}" |
| Warning | Não encontrado | "Error {ErrorId} not found in store" |
| Error | Falha | "Failed to analyze error {ErrorId}" |
| Information | Parada | "ErrorAnalysisBackgroundService is stopping" |

## Critérios de Sucesso

- [ ] Serviço inicia automaticamente com a aplicação
- [ ] Consome fila e processa erros sequencialmente
- [ ] Status atualizado corretamente (Pending → Processing → Completed/Failed)
- [ ] Erros tratados e logados adequadamente
- [ ] Graceful shutdown funciona corretamente
- [ ] Backoff implementado para erros inesperados
- [ ] Logs estruturados para observabilidade

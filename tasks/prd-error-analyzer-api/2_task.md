---
status: pending
parallelizable: true
blocked_by: ["1.0"]
---

<task_context>
<domain>infra/storage</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>concurrent_collections</dependencies>
<unblocks>["4.0", "7.0"]</unblocks>
</task_context>

# Tarefa 2.0: Implementar InMemoryErrorStore (Infra)

## Visão Geral

Implementar o armazenamento em memória usando `ConcurrentDictionary` para persistir eventos de erro e uma fila para processamento assíncrono. Esta implementação é adequada para o POC e pode ser substituída por persistência real no futuro.

<requirements>
- Implementar `IErrorStore` usando `ConcurrentDictionary`
- Implementar fila de processamento usando `Channel<string>`
- Garantir thread-safety em todas as operações
- Seguir padrões de async/await
</requirements>

## Subtarefas

- [ ] 2.1 Criar classe `InMemoryErrorStore` em `4-Infra/Store/`
- [ ] 2.2 Implementar `ConcurrentDictionary<string, ErrorEvent>` para storage
- [ ] 2.3 Implementar `Channel<string>` para fila de processamento
- [ ] 2.4 Implementar método `SaveAsync`
- [ ] 2.5 Implementar método `GetByIdAsync`
- [ ] 2.6 Implementar método `UpdateAsync`
- [ ] 2.7 Implementar método `EnqueueForAnalysisAsync`
- [ ] 2.8 Implementar método `DequeueForAnalysisAsync`
- [ ] 2.9 Registrar como Singleton no DI container

## Sequenciamento

- **Bloqueado por:** 1.0 (Domain interfaces)
- **Desbloqueia:** 4.0 (Handlers), 7.0 (Background Service)
- **Paralelizável:** Sim (pode rodar em paralelo com 3.0, 6.0, 8.0)

## Detalhes de Implementação

### InMemoryErrorStore.cs

```csharp
// 4-Infra/CopilotErrorAnalyzer.Infra/Store/InMemoryErrorStore.cs
using System.Collections.Concurrent;
using System.Threading.Channels;
using CopilotErrorAnalyzer.Domain.Entidade;
using CopilotErrorAnalyzer.Domain.Interface;

namespace CopilotErrorAnalyzer.Infra.Store;

public class InMemoryErrorStore : IErrorStore
{
    private readonly ConcurrentDictionary<string, ErrorEvent> _store = new();
    private readonly Channel<string> _processingQueue;

    public InMemoryErrorStore()
    {
        _processingQueue = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public Task<string> SaveAsync(ErrorEvent errorEvent, CancellationToken ct)
    {
        _store[errorEvent.Id] = errorEvent;
        return Task.FromResult(errorEvent.Id);
    }

    public Task<ErrorEvent?> GetByIdAsync(string id, CancellationToken ct)
    {
        _store.TryGetValue(id, out var errorEvent);
        return Task.FromResult(errorEvent);
    }

    public Task UpdateAsync(ErrorEvent errorEvent, CancellationToken ct)
    {
        _store[errorEvent.Id] = errorEvent;
        return Task.CompletedTask;
    }

    public async Task EnqueueForAnalysisAsync(string id, CancellationToken ct)
    {
        await _processingQueue.Writer.WriteAsync(id, ct);
    }

    public async Task<string?> DequeueForAnalysisAsync(CancellationToken ct)
    {
        try
        {
            return await _processingQueue.Reader.ReadAsync(ct);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }
}
```

### Registro no DI (Program.cs)

```csharp
// Registrar como Singleton para manter estado em memória
builder.Services.AddSingleton<IErrorStore, InMemoryErrorStore>();
```

## Critérios de Sucesso

- [ ] Implementação compila sem erros
- [ ] Operações são thread-safe
- [ ] Channel configurado para single reader (Background Service)
- [ ] Métodos assíncronos seguem convenções
- [ ] Teste unitário básico valida save/get
- [ ] Registrado como Singleton no DI

## Limitações Conhecidas

- Dados são perdidos ao reiniciar a aplicação (esperado para POC)
- Sem limite de tamanho do dicionário (pode crescer indefinidamente)
- Sem TTL para limpeza de entradas antigas

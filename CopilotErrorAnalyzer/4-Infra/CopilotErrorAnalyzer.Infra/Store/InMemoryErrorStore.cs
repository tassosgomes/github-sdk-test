using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CopilotErrorAnalyzer.Domain.Entities;
using CopilotErrorAnalyzer.Domain.Interfaces;

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
        ct.ThrowIfCancellationRequested();
        _store[errorEvent.Id] = errorEvent;
        return Task.FromResult(errorEvent.Id);
    }

    public Task<ErrorEvent?> GetByIdAsync(string id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _store.TryGetValue(id, out var errorEvent);
        return Task.FromResult(errorEvent);
    }

    public Task UpdateAsync(ErrorEvent errorEvent, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _store[errorEvent.Id] = errorEvent;
        return Task.CompletedTask;
    }

    public async Task EnqueueForAnalysisAsync(string id, CancellationToken ct)
    {
        await _processingQueue.Writer.WriteAsync(id, ct).ConfigureAwait(false);
    }

    public async Task<string?> DequeueForAnalysisAsync(CancellationToken ct)
    {
        try
        {
            return await _processingQueue.Reader.ReadAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }
}

using System.Threading;
using System.Threading.Tasks;
using CopilotErrorAnalyzer.Domain.Entities;

namespace CopilotErrorAnalyzer.Domain.Interfaces;

public interface IErrorStore
{
    Task<string> SaveAsync(ErrorEvent errorEvent, CancellationToken ct);
    Task<ErrorEvent?> GetByIdAsync(string id, CancellationToken ct);
    Task UpdateAsync(ErrorEvent errorEvent, CancellationToken ct);
    Task EnqueueForAnalysisAsync(string id, CancellationToken ct);
    Task<string?> DequeueForAnalysisAsync(CancellationToken ct);
}

using System.Threading;
using System.Threading.Tasks;
using CopilotErrorAnalyzer.Domain.Entities;

namespace CopilotErrorAnalyzer.Domain.Interfaces;

public interface IErrorAnalyzer
{
    Task<AnalysisResult> AnalyzeAsync(ErrorEvent errorEvent, CancellationToken ct);
}

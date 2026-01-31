using System.Threading;
using System.Threading.Tasks;
using CopilotErrorAnalyzer.Application.Dto;

namespace CopilotErrorAnalyzer.Application.Interface;

public interface IAnalyzeErrorCommandHandler
{
    Task<AnalyzeErrorResponse> HandleAsync(AnalyzeErrorRequest request, CancellationToken ct);
}

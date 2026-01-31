using System.Threading;
using System.Threading.Tasks;
using CopilotErrorAnalyzer.Application.Dto;

namespace CopilotErrorAnalyzer.Application.Interface;

public interface IGetAnalysisQueryHandler
{
    Task<ErrorAnalysisResponse?> HandleAsync(string id, CancellationToken ct);
}

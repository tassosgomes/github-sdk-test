using CopilotErrorAnalyzer.Domain.Entities;

namespace CopilotErrorAnalyzer.Application.Interface;

public interface IReportGenerator
{
    string GenerateMarkdown(ErrorEvent errorEvent, AnalysisResult result);
}

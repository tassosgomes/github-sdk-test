---
status: pending
parallelizable: false
blocked_by: ["4.0"]
---

<task_context>
<domain>api/controller</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>handlers, report_generator</dependencies>
<unblocks>["7.0", "9.0"]</unblocks>
</task_context>

# Tarefa 5.0: Implementar ErrorsController (API)

## Visão Geral

Implementar o controller REST que expõe os endpoints da API conforme definido no PRD e Tech Spec. O controller delega a lógica para os handlers e retorna respostas HTTP apropriadas.

<requirements>
- Implementar endpoint POST `/api/errors/analyze`
- Implementar endpoint GET `/api/errors/{id}`
- Implementar endpoint GET `/api/errors/{id}/report`
- Retornar códigos HTTP semânticos (200, 202, 400, 404)
- Usar ProblemDetails para erros (RFC 9457)
</requirements>

## Subtarefas

- [ ] 5.1 Criar classe `ErrorsController` com atributos de rota
- [ ] 5.2 Implementar construtor com injeção de dependência
- [ ] 5.3 Implementar endpoint `POST /api/errors/analyze`
- [ ] 5.4 Implementar endpoint `GET /api/errors/{id}`
- [ ] 5.5 Implementar endpoint `GET /api/errors/{id}/report`
- [ ] 5.6 Configurar documentação Swagger nos endpoints
- [ ] 5.7 Adicionar validação de modelo automática

## Sequenciamento

- **Bloqueado por:** 4.0 (Handlers)
- **Desbloqueia:** 7.0 (Background Service), 9.0 (Testes)
- **Paralelizável:** Não

## Detalhes de Implementação

### ErrorsController

```csharp
// 1-Services/CopilotErrorAnalyzer.API/Controllers/ErrorsController.cs
using CopilotErrorAnalyzer.Application.Dto;
using CopilotErrorAnalyzer.Application.Interface;
using CopilotErrorAnalyzer.Domain.Interface;
using Microsoft.AspNetCore.Mvc;

namespace CopilotErrorAnalyzer.API.Controllers;

[ApiController]
[Route("api/errors")]
[Produces("application/json")]
public class ErrorsController : ControllerBase
{
    private readonly IAnalyzeErrorCommandHandler _analyzeHandler;
    private readonly IGetAnalysisQueryHandler _getHandler;
    private readonly IReportGenerator _reportGenerator;
    private readonly IErrorStore _errorStore;

    public ErrorsController(
        IAnalyzeErrorCommandHandler analyzeHandler,
        IGetAnalysisQueryHandler getHandler,
        IReportGenerator reportGenerator,
        IErrorStore errorStore)
    {
        _analyzeHandler = analyzeHandler;
        _getHandler = getHandler;
        _reportGenerator = reportGenerator;
        _errorStore = errorStore;
    }

    /// <summary>
    /// Submete um erro para análise automatizada
    /// </summary>
    /// <param name="request">Dados do erro a ser analisado</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>ID da análise e URL para consulta</returns>
    [HttpPost("analyze")]
    [ProducesResponseType(typeof(AnalyzeErrorResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AnalyzeError(
        [FromBody] AnalyzeErrorRequest request,
        CancellationToken ct)
    {
        var result = await _analyzeHandler.HandleAsync(request, ct);
        
        return AcceptedAtAction(
            actionName: nameof(GetAnalysis),
            routeValues: new { id = result.Id },
            value: result);
    }

    /// <summary>
    /// Consulta o status e resultado de uma análise
    /// </summary>
    /// <param name="id">ID da análise</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Status e resultado da análise (se disponível)</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ErrorAnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAnalysis(string id, CancellationToken ct)
    {
        var result = await _getHandler.HandleAsync(id, ct);
        
        if (result is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Analysis not found",
                Detail = $"No analysis found with ID '{id}'",
                Status = StatusCodes.Status404NotFound
            });
        }
        
        return Ok(result);
    }

    /// <summary>
    /// Obtém o relatório da análise em formato Markdown
    /// </summary>
    /// <param name="id">ID da análise</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Relatório em Markdown</returns>
    [HttpGet("{id}/report")]
    [Produces("text/markdown")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReport(string id, CancellationToken ct)
    {
        var errorEvent = await _errorStore.GetByIdAsync(id, ct);
        
        if (errorEvent is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Analysis not found",
                Detail = $"No analysis found with ID '{id}'",
                Status = StatusCodes.Status404NotFound
            });
        }
        
        if (errorEvent.Result is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Analysis not completed",
                Detail = $"Analysis with ID '{id}' is still in status '{errorEvent.Status}'",
                Status = StatusCodes.Status404NotFound
            });
        }
        
        var report = _reportGenerator.GenerateMarkdown(errorEvent, errorEvent.Result);
        return Content(report, "text/markdown");
    }
}
```

### Configuração do Program.cs

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "CopilotErrorAnalyzer API", Version = "v1" });
});

// Configure ProblemDetails
builder.Services.AddProblemDetails();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
```

## Critérios de Sucesso

- [ ] Endpoint POST retorna HTTP 202 Accepted com ID
- [ ] Endpoint GET retorna HTTP 200 com dados ou HTTP 404
- [ ] Endpoint de report retorna HTTP 200 com text/markdown ou HTTP 404
- [ ] Validação de modelo retorna HTTP 400 com ValidationProblemDetails
- [ ] ProblemDetails segue RFC 9457
- [ ] Swagger documenta todos os endpoints
- [ ] Testes de integração passam

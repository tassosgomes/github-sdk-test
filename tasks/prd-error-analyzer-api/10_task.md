---
status: pending
parallelizable: false
blocked_by: ["9.0"]
---

<task_context>
<domain>documentation</domain>
<type>documentation</type>
<scope>configuration</scope>
<complexity>low</complexity>
<dependencies>all_components</dependencies>
<unblocks>[]</unblocks>
</task_context>

# Tarefa 10.0: Configurar Documentação e Swagger

## Visão Geral

Configurar documentação da API via Swagger/OpenAPI e criar README com instruções de uso. Esta tarefa finaliza o projeto e garante que a API seja facilmente consumível por desenvolvedores.

<requirements>
- Configurar Swagger com descrições detalhadas
- Documentar todos os endpoints com exemplos
- Criar README com instruções de setup e uso
- Incluir exemplos de requests e responses
</requirements>

## Subtarefas

- [ ] 10.1 Configurar Swagger/OpenAPI no Program.cs
- [ ] 10.2 Adicionar XML comments aos endpoints
- [ ] 10.3 Configurar exemplos de request/response
- [ ] 10.4 Criar README.md do projeto
- [ ] 10.5 Documentar requisitos de ambiente
- [ ] 10.6 Criar exemplos de uso com curl

## Sequenciamento

- **Bloqueado por:** 9.0 (Testes completos)
- **Desbloqueia:** Nenhuma (tarefa final)
- **Paralelizável:** Não

## Detalhes de Implementação

### Configuração do Swagger

```csharp
// Program.cs
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// ... other services

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "CopilotErrorAnalyzer API",
        Description = "API REST para análise automatizada de erros usando GitHub Copilot SDK",
        Contact = new OpenApiContact
        {
            Name = "Equipe de Desenvolvimento",
            Email = "dev@example.com"
        }
    });

    // Enable XML comments
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "CopilotErrorAnalyzer API v1");
    options.RoutePrefix = string.Empty; // Swagger na raiz
});
```

### Habilitar XML Documentation

```xml
<!-- CopilotErrorAnalyzer.API.csproj -->
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

### XML Comments no Controller

```csharp
/// <summary>
/// Controller para análise automatizada de erros
/// </summary>
[ApiController]
[Route("api/errors")]
public class ErrorsController : ControllerBase
{
    /// <summary>
    /// Submete um erro para análise automatizada
    /// </summary>
    /// <remarks>
    /// Exemplo de request:
    /// 
    ///     POST /api/errors/analyze
    ///     {
    ///         "source": "kubernetes",
    ///         "message": "CrashLoopBackOff",
    ///         "timestamp": "2026-01-30T14:30:00Z",
    ///         "kubernetesContext": {
    ///             "namespace": "default",
    ///             "podName": "my-app-7d9f8b6c5-x2k4m"
    ///         }
    ///     }
    /// 
    /// </remarks>
    /// <param name="request">Dados do erro a ser analisado</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>ID da análise e URL para consulta de status</returns>
    /// <response code="202">Erro aceito para análise</response>
    /// <response code="400">Payload inválido</response>
    [HttpPost("analyze")]
    [ProducesResponseType(typeof(AnalyzeErrorResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AnalyzeError(...)
```

### README.md

```markdown
# CopilotErrorAnalyzer API

API REST em .NET 8 que utiliza o GitHub Copilot SDK para análise automatizada de erros de aplicação e Kubernetes.

## Requisitos

- .NET 8.0 SDK
- GitHub Copilot CLI instalado
- Subscription GitHub Copilot ativa

## Setup

### 1. Instalar GitHub Copilot CLI

```bash
# Verificar instalação
copilot --version

# Autenticar
copilot auth login
```

### 2. Executar a API

```bash
cd CopilotErrorAnalyzer
dotnet run --project 1-Services/CopilotErrorAnalyzer.API
```

A API estará disponível em `https://localhost:5001` e o Swagger em `https://localhost:5001/`.

## Endpoints

### POST /api/errors/analyze

Submete um erro para análise.

**Request:**
```json
{
  "source": "kubernetes",
  "message": "CrashLoopBackOff",
  "timestamp": "2026-01-30T14:30:00Z",
  "kubernetesContext": {
    "namespace": "default",
    "podName": "my-app-7d9f8b6c5-x2k4m",
    "containerName": "app"
  }
}
```

**Response (202 Accepted):**
```json
{
  "id": "abc123-def456",
  "status": "Pending",
  "statusUrl": "/api/errors/abc123-def456"
}
```

### GET /api/errors/{id}

Consulta status e resultado da análise.

**Response (200 OK):**
```json
{
  "id": "abc123-def456",
  "status": "Completed",
  "createdAt": "2026-01-30T14:30:00Z",
  "completedAt": "2026-01-30T14:30:15Z",
  "result": {
    "classification": "KubernetesConfig",
    "rootCause": "Variável de ambiente DATABASE_URL não definida",
    "summary": "Pod em CrashLoopBackOff por falta de configuração",
    "suggestions": [
      "Adicionar DATABASE_URL ao ConfigMap",
      "Verificar se o Secret existe no namespace"
    ]
  }
}
```

### GET /api/errors/{id}/report

Obtém relatório em Markdown.

**Response (200 OK, text/markdown):**
```markdown
# Relatório de Análise de Erro

## Metadados
...
```

## Exemplos com curl

```bash
# Submeter erro
curl -X POST https://localhost:5001/api/errors/analyze \
  -H "Content-Type: application/json" \
  -d '{"source":"application","message":"NullReferenceException","timestamp":"2026-01-30T14:30:00Z"}'

# Consultar status
curl https://localhost:5001/api/errors/{id}

# Obter relatório
curl https://localhost:5001/api/errors/{id}/report
```

## Testes

```bash
dotnet test
```

## Arquitetura

```
CopilotErrorAnalyzer/
├── 1-Services/     # API Controllers
├── 2-Application/  # Handlers, DTOs, Services
├── 3-Domain/       # Entidades, Interfaces
├── 4-Infra/        # Implementações (Store, Copilot SDK)
└── 5-Tests/        # Testes unitários
```

## Limitações

- Dados são armazenados em memória (perdidos ao reiniciar)
- Sem autenticação/autorização
- Sem rate limiting
```

## Critérios de Sucesso

- [ ] Swagger acessível e documentando todos os endpoints
- [ ] XML comments presentes nos endpoints
- [ ] README com instruções claras de setup
- [ ] Exemplos de uso com curl funcionando
- [ ] Requisitos de ambiente documentados
- [ ] Limitações conhecidas documentadas

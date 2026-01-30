---
status: pending
parallelizable: false
blocked_by: ["5.0", "7.0", "8.0"]
---

<task_context>
<domain>testing</domain>
<type>testing</type>
<scope>core_feature</scope>
<complexity>medium</complexity>
<dependencies>all_components</dependencies>
<unblocks>["10.0"]</unblocks>
</task_context>

# Tarefa 9.0: Implementar Testes Unitários

## Visão Geral

Implementar testes unitários para os componentes principais da aplicação usando xUnit, Moq e FluentAssertions conforme padrões .NET do projeto. O foco é testar lógica de negócio, handlers e serviços de forma isolada.

<requirements>
- Usar xUnit como framework de testes
- Usar Moq para mocks
- Usar FluentAssertions para assertions
- Seguir padrão Arrange-Act-Assert
- Cobertura mínima de 80% nos handlers e services
</requirements>

## Subtarefas

- [ ] 9.1 Configurar projeto de testes com pacotes necessários
- [ ] 9.2 Criar testes para `AnalyzeErrorCommandHandler`
- [ ] 9.3 Criar testes para `GetAnalysisQueryHandler`
- [ ] 9.4 Criar testes para `ReportGeneratorService`
- [ ] 9.5 Criar testes para `InMemoryErrorStore`
- [ ] 9.6 Criar testes para `CopilotErrorAnalyzerService.ParseResponse`
- [ ] 9.7 Validar cobertura de código

## Sequenciamento

- **Bloqueado por:** 5.0, 7.0, 8.0 (componentes a testar)
- **Desbloqueia:** 10.0 (Documentação)
- **Paralelizável:** Não

## Detalhes de Implementação

### Configuração do Projeto de Testes

```xml
<!-- 5-Tests/CopilotErrorAnalyzer.UnitTests/CopilotErrorAnalyzer.UnitTests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
    <PackageReference Include="Moq" Version="4.*" />
    <PackageReference Include="FluentAssertions" Version="6.*" />
    <PackageReference Include="coverlet.collector" Version="6.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\2-Application\CopilotErrorAnalyzer.Application\CopilotErrorAnalyzer.Application.csproj" />
    <ProjectReference Include="..\..\4-Infra\CopilotErrorAnalyzer.Infra\CopilotErrorAnalyzer.Infra.csproj" />
  </ItemGroup>
</Project>
```

### Testes do AnalyzeErrorCommandHandler

```csharp
// 5-Tests/CopilotErrorAnalyzer.UnitTests/Application/AnalyzeErrorCommandHandlerTests.cs
using CopilotErrorAnalyzer.Application.Command;
using CopilotErrorAnalyzer.Application.Dto;
using CopilotErrorAnalyzer.Domain.Entidade;
using CopilotErrorAnalyzer.Domain.Interface;
using FluentAssertions;
using Moq;

namespace CopilotErrorAnalyzer.UnitTests.Application;

public class AnalyzeErrorCommandHandlerTests
{
    private readonly Mock<IErrorStore> _errorStoreMock;
    private readonly AnalyzeErrorCommandHandler _handler;

    public AnalyzeErrorCommandHandlerTests()
    {
        _errorStoreMock = new Mock<IErrorStore>();
        _handler = new AnalyzeErrorCommandHandler(_errorStoreMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidRequest_ShouldSaveAndEnqueue()
    {
        // Arrange
        var request = new AnalyzeErrorRequest(
            Source: "application",
            Message: "NullReferenceException",
            Timestamp: DateTimeOffset.UtcNow,
            StackTrace: null,
            KubernetesContext: null,
            ApplicationContext: null);

        _errorStoreMock
            .Setup(s => s.SaveAsync(It.IsAny<ErrorEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-id-123");

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        result.Id.Should().Be("test-id-123");
        result.Status.Should().Be("Pending");
        result.StatusUrl.Should().Be("/api/errors/test-id-123");

        _errorStoreMock.Verify(
            s => s.SaveAsync(It.Is<ErrorEvent>(e => 
                e.Source == "application" && 
                e.Message == "NullReferenceException"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _errorStoreMock.Verify(
            s => s.EnqueueForAnalysisAsync("test-id-123", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithKubernetesContext_ShouldMapCorrectly()
    {
        // Arrange
        var request = new AnalyzeErrorRequest(
            Source: "kubernetes",
            Message: "CrashLoopBackOff",
            Timestamp: DateTimeOffset.UtcNow,
            StackTrace: null,
            KubernetesContext: new KubernetesContextDto(
                Namespace: "default",
                PodName: "my-pod",
                ContainerName: "app",
                NodeName: "node-1",
                Labels: new Dictionary<string, string> { ["app"] = "test" },
                ManifestYaml: null),
            ApplicationContext: null);

        _errorStoreMock
            .Setup(s => s.SaveAsync(It.IsAny<ErrorEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("k8s-error-id");

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        _errorStoreMock.Verify(
            s => s.SaveAsync(It.Is<ErrorEvent>(e =>
                e.KubernetesContext != null &&
                e.KubernetesContext.Namespace == "default" &&
                e.KubernetesContext.PodName == "my-pod"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
```

### Testes do GetAnalysisQueryHandler

```csharp
// 5-Tests/CopilotErrorAnalyzer.UnitTests/Application/GetAnalysisQueryHandlerTests.cs
using CopilotErrorAnalyzer.Application.Query;
using CopilotErrorAnalyzer.Domain.Entidade;
using CopilotErrorAnalyzer.Domain.Interface;
using FluentAssertions;
using Moq;

namespace CopilotErrorAnalyzer.UnitTests.Application;

public class GetAnalysisQueryHandlerTests
{
    private readonly Mock<IErrorStore> _errorStoreMock;
    private readonly GetAnalysisQueryHandler _handler;

    public GetAnalysisQueryHandlerTests()
    {
        _errorStoreMock = new Mock<IErrorStore>();
        _handler = new GetAnalysisQueryHandler(_errorStoreMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WithExistingId_ShouldReturnResponse()
    {
        // Arrange
        var errorEvent = new ErrorEvent
        {
            Id = "existing-id",
            Source = "application",
            Message = "Test error",
            Timestamp = DateTimeOffset.UtcNow,
            Status = AnalysisStatus.Completed,
            Result = new AnalysisResult
            {
                Classification = ErrorClassification.ApplicationBug,
                RootCause = "Test root cause",
                Summary = "Test summary",
                Suggestions = new List<string> { "Fix the bug" }
            }
        };

        _errorStoreMock
            .Setup(s => s.GetByIdAsync("existing-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(errorEvent);

        // Act
        var result = await _handler.HandleAsync("existing-id", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("existing-id");
        result.Status.Should().Be("Completed");
        result.Result.Should().NotBeNull();
        result.Result!.Classification.Should().Be("ApplicationBug");
    }

    [Fact]
    public async Task HandleAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        _errorStoreMock
            .Setup(s => s.GetByIdAsync("non-existing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErrorEvent?)null);

        // Act
        var result = await _handler.HandleAsync("non-existing", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}
```

### Testes do ReportGeneratorService

```csharp
// 5-Tests/CopilotErrorAnalyzer.UnitTests/Application/ReportGeneratorServiceTests.cs
using CopilotErrorAnalyzer.Application.Service;
using CopilotErrorAnalyzer.Domain.Entidade;
using FluentAssertions;

namespace CopilotErrorAnalyzer.UnitTests.Application;

public class ReportGeneratorServiceTests
{
    private readonly ReportGeneratorService _service;

    public ReportGeneratorServiceTests()
    {
        _service = new ReportGeneratorService();
    }

    [Fact]
    public void GenerateMarkdown_ShouldContainAllSections()
    {
        // Arrange
        var errorEvent = new ErrorEvent
        {
            Id = "test-id",
            Source = "application",
            Message = "NullReferenceException",
            Timestamp = DateTimeOffset.UtcNow,
            CompletedAt = DateTimeOffset.UtcNow
        };

        var result = new AnalysisResult
        {
            Classification = ErrorClassification.ApplicationBug,
            RootCause = "Null reference",
            Summary = "Bug found",
            Suggestions = new List<string> { "Add null check" }
        };

        // Act
        var markdown = _service.GenerateMarkdown(errorEvent, result);

        // Assert
        markdown.Should().Contain("# Relatório de Análise de Erro");
        markdown.Should().Contain("## Metadados");
        markdown.Should().Contain("## Resumo");
        markdown.Should().Contain("## Classificação");
        markdown.Should().Contain("## Causa Raiz");
        markdown.Should().Contain("## Sugestões de Correção");
        markdown.Should().Contain("test-id");
        markdown.Should().Contain("🐛");
    }

    [Theory]
    [InlineData(ErrorClassification.KubernetesConfig, "☸️")]
    [InlineData(ErrorClassification.ApplicationBug, "🐛")]
    [InlineData(ErrorClassification.Unknown, "❓")]
    public void GenerateMarkdown_ShouldUseCorrectEmoji(ErrorClassification classification, string expectedEmoji)
    {
        // Arrange
        var errorEvent = new ErrorEvent
        {
            Id = "test",
            Source = "test",
            Message = "test",
            Timestamp = DateTimeOffset.UtcNow
        };

        var result = new AnalysisResult
        {
            Classification = classification,
            RootCause = "test",
            Summary = "test",
            Suggestions = new List<string>()
        };

        // Act
        var markdown = _service.GenerateMarkdown(errorEvent, result);

        // Assert
        markdown.Should().Contain(expectedEmoji);
    }
}
```

### Testes do InMemoryErrorStore

```csharp
// 5-Tests/CopilotErrorAnalyzer.UnitTests/Infra/InMemoryErrorStoreTests.cs
using CopilotErrorAnalyzer.Domain.Entidade;
using CopilotErrorAnalyzer.Infra.Store;
using FluentAssertions;

namespace CopilotErrorAnalyzer.UnitTests.Infra;

public class InMemoryErrorStoreTests
{
    [Fact]
    public async Task SaveAndGet_ShouldWorkCorrectly()
    {
        // Arrange
        var store = new InMemoryErrorStore();
        var errorEvent = new ErrorEvent
        {
            Source = "test",
            Message = "test message",
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        var id = await store.SaveAsync(errorEvent, CancellationToken.None);
        var retrieved = await store.GetByIdAsync(id, CancellationToken.None);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Message.Should().Be("test message");
    }

    [Fact]
    public async Task EnqueueAndDequeue_ShouldWorkCorrectly()
    {
        // Arrange
        var store = new InMemoryErrorStore();

        // Act
        await store.EnqueueForAnalysisAsync("id-1", CancellationToken.None);
        await store.EnqueueForAnalysisAsync("id-2", CancellationToken.None);

        var first = await store.DequeueForAnalysisAsync(CancellationToken.None);
        var second = await store.DequeueForAnalysisAsync(CancellationToken.None);

        // Assert
        first.Should().Be("id-1");
        second.Should().Be("id-2");
    }
}
```

## Comandos de Execução

```bash
# Executar todos os testes
cd CopilotErrorAnalyzer
dotnet test

# Executar com cobertura
dotnet test --collect:"XPlat Code Coverage"

# Executar testes específicos
dotnet test --filter "FullyQualifiedName~AnalyzeErrorCommandHandlerTests"
```

## Critérios de Sucesso

- [ ] Todos os testes passam
- [ ] Cobertura ≥80% nos handlers e services
- [ ] Padrão AAA (Arrange-Act-Assert) seguido
- [ ] Mocks usados para isolar dependências
- [ ] Edge cases testados (null, empty, invalid)
- [ ] Testes são determinísticos e independentes

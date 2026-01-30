---
status: pending
parallelizable: true
blocked_by: ["1.0"]
---

<task_context>
<domain>application/service</domain>
<type>implementation</type>
<scope>core_feature</scope>
<complexity>low</complexity>
<dependencies>domain_entities</dependencies>
<unblocks>["5.0", "9.0"]</unblocks>
</task_context>

# Tarefa 8.0: Implementar ReportGeneratorService

## Visão Geral

Implementar o serviço que gera relatórios em formato Markdown a partir dos resultados de análise. O relatório deve ser estruturado, legível e incluir todas as informações relevantes para troubleshooting.

<requirements>
- Implementar `IReportGenerator`
- Gerar Markdown válido com seções hierárquicas
- Incluir metadados, classificação, causa raiz e sugestões
- Formatar de forma legível e compartilhável
</requirements>

## Subtarefas

- [ ] 8.1 Criar classe `ReportGeneratorService`
- [ ] 8.2 Implementar método `GenerateMarkdown`
- [ ] 8.3 Criar seção de metadados
- [ ] 8.4 Criar seção de resumo
- [ ] 8.5 Criar seção de classificação e causa raiz
- [ ] 8.6 Criar seção de sugestões
- [ ] 8.7 Criar seção de contexto original (opcional)
- [ ] 8.8 Registrar no DI container

## Sequenciamento

- **Bloqueado por:** 1.0 (Domain entities)
- **Desbloqueia:** 5.0 (Controller endpoint de report), 9.0 (Testes)
- **Paralelizável:** Sim (pode rodar em paralelo com 2.0, 3.0, 6.0)

## Detalhes de Implementação

### ReportGeneratorService

```csharp
// 2-Application/Service/ReportGeneratorService.cs
using System.Text;
using CopilotErrorAnalyzer.Application.Interface;
using CopilotErrorAnalyzer.Domain.Entidade;

namespace CopilotErrorAnalyzer.Application.Service;

public class ReportGeneratorService : IReportGenerator
{
    public string GenerateMarkdown(ErrorEvent errorEvent, AnalysisResult result)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("# Relatório de Análise de Erro");
        sb.AppendLine();

        // Metadata
        sb.AppendLine("## Metadados");
        sb.AppendLine();
        sb.AppendLine($"| Campo | Valor |");
        sb.AppendLine($"|-------|-------|");
        sb.AppendLine($"| **ID da Análise** | `{errorEvent.Id}` |");
        sb.AppendLine($"| **Fonte** | {errorEvent.Source} |");
        sb.AppendLine($"| **Timestamp do Erro** | {errorEvent.Timestamp:yyyy-MM-dd HH:mm:ss UTC} |");
        sb.AppendLine($"| **Análise Concluída** | {errorEvent.CompletedAt:yyyy-MM-dd HH:mm:ss UTC} |");
        sb.AppendLine();

        // Summary
        sb.AppendLine("## Resumo");
        sb.AppendLine();
        sb.AppendLine($"> {result.Summary}");
        sb.AppendLine();

        // Classification
        sb.AppendLine("## Classificação");
        sb.AppendLine();
        sb.AppendLine($"**Tipo:** {FormatClassification(result.Classification)}");
        sb.AppendLine();
        sb.AppendLine(GetClassificationEmoji(result.Classification));
        sb.AppendLine();

        // Root Cause
        sb.AppendLine("## Causa Raiz");
        sb.AppendLine();
        sb.AppendLine(result.RootCause);
        sb.AppendLine();

        // Suggestions
        sb.AppendLine("## Sugestões de Correção");
        sb.AppendLine();
        if (result.Suggestions.Count > 0)
        {
            for (int i = 0; i < result.Suggestions.Count; i++)
            {
                sb.AppendLine($"{i + 1}. {result.Suggestions[i]}");
            }
        }
        else
        {
            sb.AppendLine("_Nenhuma sugestão disponível._");
        }
        sb.AppendLine();

        // Original Error
        sb.AppendLine("## Erro Original");
        sb.AppendLine();
        sb.AppendLine("### Mensagem");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine(errorEvent.Message);
        sb.AppendLine("```");
        sb.AppendLine();

        if (errorEvent.StackTrace is not null)
        {
            sb.AppendLine("### Stack Trace");
            sb.AppendLine();
            sb.AppendLine("```");
            sb.AppendLine(errorEvent.StackTrace);
            sb.AppendLine("```");
            sb.AppendLine();
        }

        // Context (if available)
        if (errorEvent.KubernetesContext is not null)
        {
            sb.AppendLine("### Contexto Kubernetes");
            sb.AppendLine();
            AppendKubernetesContext(sb, errorEvent.KubernetesContext);
        }

        if (errorEvent.ApplicationContext is not null)
        {
            sb.AppendLine("### Contexto da Aplicação");
            sb.AppendLine();
            AppendApplicationContext(sb, errorEvent.ApplicationContext);
        }

        // Footer
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"_Relatório gerado automaticamente por CopilotErrorAnalyzer_");

        return sb.ToString();
    }

    private static string FormatClassification(ErrorClassification classification) =>
        classification switch
        {
            ErrorClassification.KubernetesConfig => "Configuração Kubernetes",
            ErrorClassification.ApplicationBug => "Bug de Aplicação",
            ErrorClassification.Unknown => "Desconhecido",
            _ => "Desconhecido"
        };

    private static string GetClassificationEmoji(ErrorClassification classification) =>
        classification switch
        {
            ErrorClassification.KubernetesConfig => "☸️ Este erro está relacionado à configuração do Kubernetes.",
            ErrorClassification.ApplicationBug => "🐛 Este erro está relacionado a um bug no código da aplicação.",
            ErrorClassification.Unknown => "❓ Não foi possível determinar a origem do erro.",
            _ => "❓ Não foi possível determinar a origem do erro."
        };

    private static void AppendKubernetesContext(StringBuilder sb, KubernetesContext ctx)
    {
        sb.AppendLine($"| Campo | Valor |");
        sb.AppendLine($"|-------|-------|");
        if (ctx.Namespace is not null) sb.AppendLine($"| Namespace | `{ctx.Namespace}` |");
        if (ctx.PodName is not null) sb.AppendLine($"| Pod | `{ctx.PodName}` |");
        if (ctx.ContainerName is not null) sb.AppendLine($"| Container | `{ctx.ContainerName}` |");
        if (ctx.NodeName is not null) sb.AppendLine($"| Node | `{ctx.NodeName}` |");
        sb.AppendLine();

        if (ctx.ManifestYaml is not null)
        {
            sb.AppendLine("**Manifest:**");
            sb.AppendLine();
            sb.AppendLine("```yaml");
            sb.AppendLine(ctx.ManifestYaml);
            sb.AppendLine("```");
            sb.AppendLine();
        }
    }

    private static void AppendApplicationContext(StringBuilder sb, ApplicationContext ctx)
    {
        sb.AppendLine($"| Campo | Valor |");
        sb.AppendLine($"|-------|-------|");
        if (ctx.ApplicationName is not null) sb.AppendLine($"| Aplicação | {ctx.ApplicationName} |");
        if (ctx.Version is not null) sb.AppendLine($"| Versão | {ctx.Version} |");
        if (ctx.Environment is not null) sb.AppendLine($"| Ambiente | {ctx.Environment} |");
        sb.AppendLine();
    }
}
```

### Registro no DI

```csharp
// Program.cs
builder.Services.AddSingleton<IReportGenerator, ReportGeneratorService>();
```

### Exemplo de Relatório Gerado

```markdown
# Relatório de Análise de Erro

## Metadados

| Campo | Valor |
|-------|-------|
| **ID da Análise** | `abc123-def456` |
| **Fonte** | kubernetes |
| **Timestamp do Erro** | 2026-01-30 14:30:00 UTC |
| **Análise Concluída** | 2026-01-30 14:30:15 UTC |

## Resumo

> Pod em CrashLoopBackOff devido a variável de ambiente faltante.

## Classificação

**Tipo:** Configuração Kubernetes

☸️ Este erro está relacionado à configuração do Kubernetes.

## Causa Raiz

A variável de ambiente DATABASE_URL não está definida no deployment, 
causando falha na inicialização da aplicação.

## Sugestões de Correção

1. Adicionar a variável DATABASE_URL ao ConfigMap ou Secret
2. Atualizar o deployment para referenciar a variável
3. Verificar se o Secret existe no namespace correto

...
```

## Critérios de Sucesso

- [ ] Markdown gerado é válido e bem formatado
- [ ] Todas as seções obrigatórias presentes
- [ ] Emojis indicam tipo de classificação visualmente
- [ ] Contexto opcional renderizado quando presente
- [ ] Relatório legível e compartilhável
- [ ] Testes unitários validam formato

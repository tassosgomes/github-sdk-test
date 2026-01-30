# PRD: API de Análise de Erros com GitHub Copilot SDK

## Visão Geral

API REST em .NET que recebe eventos de erro (logs de aplicação ou erros de Kubernetes) via webhook e utiliza o GitHub Copilot SDK para realizar troubleshooting automatizado. A API analisa a causa raiz do problema — diferenciando entre falhas de configuração no Kubernetes e bugs no código da aplicação — e gera um relatório em Markdown com diagnóstico e sugestões de correção.

Este projeto serve como hello-world para explorar as capacidades do GitHub Copilot SDK em .NET, mas será estruturado para evolução futura em ambiente de produção.

## Objetivos

| Objetivo | Métrica de Sucesso |
|----------|-------------------|
| Validar integração com GitHub Copilot SDK em .NET | SDK funcionando e gerando análises |
| Classificar erros automaticamente | ≥80% de classificação correta (K8s config vs bug de código) em testes manuais |
| Gerar relatórios úteis | Relatório contém: causa raiz, classificação e sugestão de correção |
| Estrutura pronta para produção | Código seguindo padrões de arquitetura limpa, testável e extensível |

## Histórias de Usuário

### US-01: Envio de erro via webhook
**Como** um sistema de monitoramento (ex: Prometheus, Alertmanager, Kubernetes),  
**Eu quero** enviar eventos de erro para a API via webhook  
**Para que** os erros sejam analisados automaticamente.

### US-02: Análise de erro de aplicação
**Como** um desenvolvedor,  
**Eu quero** que a API identifique se um erro é um bug no código  
**Para que** eu receba orientações de correção no código-fonte.

### US-03: Análise de erro de Kubernetes
**Como** um engenheiro de plataforma,  
**Eu quero** que a API identifique falhas de configuração no Kubernetes  
**Para que** eu receba orientações sobre ajustes em manifests/configurações.

### US-04: Relatório de diagnóstico
**Como** um usuário da API,  
**Eu quero** receber um relatório em Markdown com o diagnóstico completo  
**Para que** eu possa compartilhar e documentar a análise.

## Funcionalidades Principais

### F-01: Endpoint de Recebimento de Erros (Webhook)

**O que faz:** Recebe eventos de erro via HTTP POST.

**Por que é importante:** Ponto de entrada único para integração com sistemas de monitoramento.

**Como funciona:** Controller ASP.NET Core recebe payload JSON, valida estrutura e enfileira para análise.

**Requisitos Funcionais:**
- RF-01.1: Endpoint POST `/api/errors/analyze` aceita payload JSON
- RF-01.2: Validar campos obrigatórios: `source`, `message`, `timestamp`
- RF-01.3: Aceitar campos opcionais: `stackTrace`, `kubernetesContext`, `applicationContext`
- RF-01.4: Retornar HTTP 202 (Accepted) com ID de análise para processamento
- RF-01.5: Retornar HTTP 400 para payloads inválidos

### F-02: Motor de Análise com Copilot SDK

**O que faz:** Utiliza o GitHub Copilot SDK para analisar o erro e identificar causa raiz.

**Por que é importante:** Capacidade central do produto — inteligência de troubleshooting.

**Como funciona:** Cria sessão no Copilot SDK, envia prompt estruturado com contexto do erro, processa resposta.

**Requisitos Funcionais:**
- RF-02.1: Criar sessão no Copilot SDK com modelo configurável
- RF-02.2: Construir prompt com contexto completo do erro
- RF-02.3: Classificar erro em categorias: `KUBERNETES_CONFIG`, `APPLICATION_BUG`, `UNKNOWN`
- RF-02.4: Extrair causa raiz provável da análise
- RF-02.5: Gerar sugestões de correção baseadas na classificação

### F-03: Geração de Relatório Markdown

**O que faz:** Gera relatório estruturado em Markdown com o diagnóstico.

**Por que é importante:** Formato legível e compartilhável para documentação.

**Como funciona:** Template de Markdown preenchido com dados da análise.

**Requisitos Funcionais:**
- RF-03.1: Gerar relatório contendo: resumo, classificação, causa raiz, sugestões
- RF-03.2: Incluir metadados: timestamp da análise, ID do erro original
- RF-03.3: Formatar em Markdown válido com seções hierárquicas
- RF-03.4: Endpoint GET `/api/errors/{id}/report` retorna relatório em Markdown

## Experiência do Usuário

### Persona Primária: Desenvolvedor/SRE

**Necessidades:**
- Integração simples via HTTP
- Resposta rápida (segundos, não minutos)
- Relatório claro e acionável

### Fluxo Principal

```
[Sistema Externo] → POST /api/errors/analyze (JSON)
                          ↓
                  [API valida e aceita]
                          ↓
               [Copilot SDK analisa erro]
                          ↓
              [Gera relatório Markdown]
                          ↓
[Usuário] ← GET /api/errors/{id}/report (Markdown)
```

### Requisitos de API

- Respostas em JSON para endpoints de dados
- Resposta em texto/markdown para endpoint de relatório
- Códigos HTTP semânticos (200, 202, 400, 404, 500)
- Headers CORS configuráveis para integração com dashboards

## Restrições Técnicas de Alto Nível

| Categoria | Restrição |
|-----------|-----------|
| **Runtime** | .NET 8.0 ou superior |
| **Framework** | ASP.NET Core Web API com Controllers |
| **Dependência Externa** | GitHub Copilot CLI instalado no ambiente de execução |
| **Autenticação Copilot** | Requer subscription GitHub Copilot ativa |
| **Formato de Dados** | JSON para entrada, Markdown para relatório |
| **Armazenamento** | Em memória (sem persistência nesta fase) |

## Não-Objetivos (Fora de Escopo)

| Item | Justificativa |
|------|---------------|
| Persistência em banco de dados | Será adicionada em versão futura |
| Autenticação/Autorização na API | Foco atual é validar integração com SDK |
| Interface web/dashboard | API-first, UI pode ser construída depois |
| Integração direta com GitHub Issues | Fora do escopo do hello-world |
| Análise de múltiplos erros em batch | Processamento individual por requisição |
| Histórico de análises | Requer persistência (fora de escopo) |
| Retry automático de análises falhas | Simplificação para POC |

## Decisões Técnicas Definidas

| Item | Decisão |
|------|---------|
| Modelo Copilot | `claude-sonnet-4.5` |
| Timeout de análise | Sem limite inicial (aguarda conclusão) |
| Limite de tamanho de log | Sem limite inicial |

## Questões em Aberto

| # | Questão | Impacto |
|---|---------|---------|
| Q-01 | Formato específico de erro de Kubernetes esperado? | Design do payload |
| Q-02 | Necessidade de rate limiting na API? | Custo de uso do Copilot |

---

**Documento criado em:** 30/01/2026  
**Versão:** 1.1  
**Status:** Aprovado para Tech Spec

# Integração com n8n - Fera do Prompt API

## Visão Geral
O backend da API Fera do Prompt está integrado com o n8n para processamento de prompts via webhooks.

## Arquitetura

### Fluxo de Execução
1. **Cliente** → Envia requisição POST para `/api/prompts/execute`
2. **Controller** → Valida dados e chama `PromptService.ExecutePromptAsync()`
3. **Service** →
   - Busca o prompt no banco de dados
   - Determina qual webhook usar (test/prod)
   - Envia payload para n8n
   - Recebe resposta com output processado
   - Salva histórico no banco
4. **Response** → Retorna `PromptResponseViewModel` com output

## ViewModels Criados

### PromptCreateViewModel
```csharp
{
  "title": "string",     // max 200 caracteres
  "body": "string",      // max 5000 caracteres (template do prompt)
  "model": "string"      // max 50 caracteres (padrão: "gpt-4o")
}
```

### PromptRunViewModel
```csharp
{
  "promptId": int,       // ID do prompt a ser executado
  "input": "string",     // max 2000 caracteres (input do usuário)
  "model": "string"      // max 50 caracteres (padrão: "gpt-4o")
}
```

### PromptResponseViewModel
```csharp
{
  "promptId": int,
  "input": "string",
  "output": "string",    // Resposta processada do n8n
  "modelUsed": "string",
  "executedAt": "datetime"
}
```

## Configuração de Webhooks

### Variáveis de Ambiente
No arquivo `.env.local` do backend:

```bash
WEBHOOK_PRODUCTION_URL=https://n8n-n8n.h8tqhp.easypanel.host/webhook/11842b76-07b4-4799-a870-ec4f31f47503
WEBHOOK_TEST_URL=https://n8n-n8n.h8tqhp.easypanel.host/webhook-test/11842b76-07b4-4799-a870-ec4f31f47503
```

### Seleção de Webhook
O sistema escolhe automaticamente qual webhook usar baseado no ambiente:

- **Development**: usa `WEBHOOK_TEST_URL`
- **Production/Staging**: usa `WEBHOOK_PRODUCTION_URL`

## Payload Enviado ao n8n

```json
{
  "promptId": 123,
  "model": "gpt-4o",
  "input": "entrada do usuário",
  "promptBody": "template do prompt salvo no banco"
}
```

## Resposta Esperada do n8n

```json
{
  "output": "texto processado pela IA"
}
```

## Service Layer

### IPromptService
Interface com os seguintes métodos:
- `CreatePromptAsync(PromptCreateViewModel)` - Cria novo prompt
- `ExecutePromptAsync(PromptRunViewModel)` - Executa prompt via n8n
- `GetAllPromptsAsync()` - Lista todos os prompts
- `GetByIdAsync(int)` - Busca prompt específico com histórico
- `DeletePromptAsync(int)` - Remove prompt

### PromptService
Implementação que:
- ✅ Usa `IHttpClientFactory` para chamadas HTTP
- ✅ Timeout de 120 segundos para n8n
- ✅ Logging estruturado (sem expor secrets)
- ✅ Tratamento de erros robusto
- ✅ Salva histórico automaticamente
- ✅ 100% async/await (sem `.Result` ou `.Wait()`)

## Extensions

### ServiceExtensions
Métodos de configuração:
- `AddApplicationServices()` - Registra DbContext, Services e HttpClient
- `AddN8nConfiguration()` - Configura URLs dos webhooks

Uso no `Program.cs`:
```csharp
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddN8nConfiguration(builder.Configuration);
```

## Histórico de Execução

Toda execução é automaticamente salva na tabela `PromptHistories`:

```csharp
{
  "id": int,
  "promptId": int,
  "input": "string",
  "output": "string",
  "modelUsed": "string",
  "executedAt": "datetime"
}
```

## Segurança

### ✅ Boas Práticas Implementadas
- Webhooks configurados via variáveis de ambiente
- Nunca loga URLs completas ou payloads sensíveis
- Timeout configurado para evitar travamento
- Validação de input via Data Annotations
- HttpClient gerenciado via Factory (evita socket exhaustion)

### ⚠️ GitHub Secrets (Produção)
Configurar no repositório:
- `WEBHOOK_PRODUCTION_URL`
- `WEBHOOK_TEST_URL`

## Próximos Passos

1. **Criar Controller** `PromptsController.cs` com endpoints:
   - `POST /api/prompts` - Criar prompt
   - `POST /api/prompts/execute` - Executar prompt
   - `GET /api/prompts` - Listar prompts
   - `GET /api/prompts/{id}` - Buscar por ID
   - `DELETE /api/prompts/{id}` - Deletar prompt

2. **Configurar n8n Workflow** para receber e processar os payloads

3. **Testes** - Criar testes unitários para `PromptService`

## Exemplo de Uso

```bash
# Criar prompt
POST /api/prompts
{
  "title": "Corretor de Texto",
  "body": "Corrija o seguinte texto: {input}",
  "model": "gpt-4o"
}

# Executar prompt
POST /api/prompts/execute
{
  "promptId": 1,
  "input": "texto com erros ortograficos",
  "model": "gpt-4o"
}

# Resposta
{
  "promptId": 1,
  "input": "texto com erros ortograficos",
  "output": "texto com erros ortográficos",
  "modelUsed": "gpt-4o",
  "executedAt": "2025-11-07T10:30:00Z"
}
```

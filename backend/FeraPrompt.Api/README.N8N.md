# ?? Integração n8n - Fera do Prompt API

## ?? Visão Geral

A API Fera do Prompt está integrada com o **n8n** (plataforma de automação de workflows) através de webhooks para processar prompts em modelos de IA (GPT-4, GPT-5, Claude, etc).

---

## ?? URLs do Webhook

### **Produção (prompt-cowboy)**
```
https://n8n-n8tqhp.easypanel.host/webhook/prompt-cowboy
```

### **Teste (prompt-cowboy)**
```
https://n8n-n8tqhp.easypanel.host/webhook-test/prompt-cowboy
```

---

## ?? Fluxo de Execução

```
1. Frontend/Cliente
   ? POST /api/prompts/execute
2. PromptsController.Execute()
   ?
3. PromptService.ExecutePromptAsync()
   ? Busca Prompt no DB
   ? Determina ambiente (Dev/Prod)
   ? Monta payload
   ? POST para n8n webhook
4. n8n Workflow
   ? Processa com IA (GPT/Claude)
   ? Retorna output
5. PromptService
   ? Salva no PromptHistory
   ? Retorna PromptResponseViewModel
6. Cliente recebe resposta
```

---

## ?? Payload Enviado ao n8n

```json
{
  "promptId": 1,
  "model": "gpt-4",
  "input": "Olá, mundo!",
  "promptBody": "Traduza para {language}: {text}"
}
```

### **Campos:**
- **promptId** (int): ID do prompt no banco de dados
- **model** (string): Modelo de IA a usar (gpt-4, gpt-5, claude-sonnet)
- **input** (string): Texto de entrada do usuário
- **promptBody** (string): Template do prompt com placeholders

---

## ?? Resposta Esperada do n8n

```json
{
  "output": "Hello, world!"
}
```

### **Campos:**
- **output** (string): Texto processado pela IA

---

## ?? Configuração

### **1. Variáveis de Ambiente**

**Desenvolvimento (`.env.local`):**
```env
WEBHOOK_PRODUCTION_URL=https://n8n-n8tqhp.easypanel.host/webhook/prompt-cowboy
WEBHOOK_TEST_URL=https://n8n-n8tqhp.easypanel.host/webhook-test/prompt-cowboy
```

**Produção (GitHub Secrets):**
```
WEBHOOK_PRODUCTION_URL=https://n8n-n8tqhp.easypanel.host/webhook/prompt-cowboy
WEBHOOK_TEST_URL=https://n8n-n8tqhp.easypanel.host/webhook-test/prompt-cowboy
```

### **2. appsettings.json**

```json
{
  "N8n": {
    "WEBHOOK_PRODUCTION_URL": "https://n8n-n8tqhp.easypanel.host/webhook/prompt-cowboy",
    "WEBHOOK_TEST_URL": "https://n8n-n8tqhp.easypanel.host/webhook-test/prompt-cowboy"
  }
}
```

### **3. Lógica de Seleção de Ambiente**

```csharp
// PromptService.cs
var webhookUrl = _environment.Equals("Development", StringComparison.OrdinalIgnoreCase)
    ? _configuration["N8n:WEBHOOK_TEST_URL"]
    : _configuration["N8n:WEBHOOK_PRODUCTION_URL"];
```

**Ambientes:**
- **Development** ? usa `WEBHOOK_TEST_URL`
- **Production/Staging** ? usa `WEBHOOK_PRODUCTION_URL`

---

## ?? Como Testar

### **1. Via Swagger**

1. Acesse `https://localhost:7080/swagger`
2. Expanda **POST /api/prompts/execute**
3. Clique em **Try it out**
4. Edite o JSON:

```json
{
  "promptId": 1,
  "input": "Traduza 'Hello World' para português",
  "model": "gpt-4"
}
```

5. Clique em **Execute**
6. Veja a resposta:

```json
{
  "promptId": 1,
  "input": "Traduza 'Hello World' para português",
  "output": "Olá Mundo",
  "modelUsed": "gpt-4",
  "executedAt": "2025-11-08T14:30:00Z"
}
```

---

### **2. Via Tests.http**

```http
### Executar Prompt
POST https://localhost:7080/api/prompts/execute
Content-Type: application/json

{
  "promptId": 1,
  "input": "Traduza 'Good morning' para espanhol",
  "model": "gpt-4"
}
```

---

### **3. Via cURL**

```bash
curl -X POST https://localhost:7080/api/prompts/execute \
  -H "Content-Type: application/json" \
  -d '{
    "promptId": 1,
    "input": "Traduza 'Good morning' para francês",
    "model": "gpt-4"
  }'
```

---

## ?? Histórico de Execuções

Cada execução é salva na tabela `PromptHistories`:

```sql
SELECT 
    ph.Id,
    ph.PromptId,
    p.Title AS PromptTitle,
    ph.Input,
    ph.Output,
    ph.ModelUsed,
    ph.ExecutedAt
FROM PromptHistories ph
INNER JOIN Prompts p ON ph.PromptId = p.Id
ORDER BY ph.ExecutedAt DESC;
```

---

## ??? Segurança

### ? **Boas Práticas**

1. **Timeout:** 120 segundos configurado
```csharp
httpClient.Timeout = TimeSpan.FromSeconds(120);
```

2. **Logs sem expor secrets:**
```csharp
_logger.LogInformation("Enviando requisição para n8n. Environment: {Environment}", _environment);
// Nunca logar a URL completa com tokens
```

3. **Tratamento de erros:**
```csharp
if (!response.IsSuccessStatusCode)
{
    throw new HttpRequestException($"Erro ao executar prompt no n8n: {response.StatusCode}");
}
```

4. **Validação de resposta:**
```csharp
if (n8nResponse == null || string.IsNullOrEmpty(n8nResponse.Output))
{
    throw new InvalidOperationException("Resposta inválida do n8n");
}
```

---

## ?? Troubleshooting

### **Erro: "URL do webhook n8n não configurada"**

**Solução:**
1. Verifique `.env.local` ou `appsettings.Development.json`
2. Certifique-se que as variáveis estão definidas:
   - `WEBHOOK_PRODUCTION_URL`
   - `WEBHOOK_TEST_URL`

---

### **Erro: "Erro ao executar prompt no n8n: 404"**

**Causa:** Webhook não existe no n8n.

**Solução:**
1. Verifique se o workflow `prompt-cowboy` está ativo no n8n
2. Confirme a URL: `https://n8n-n8tqhp.easypanel.host/webhook/prompt-cowboy`

---

### **Erro: "Timeout ao chamar n8n"**

**Causa:** n8n demorou mais de 120 segundos.

**Solução:**
1. Aumente o timeout em `PromptService.cs`:
```csharp
httpClient.Timeout = TimeSpan.FromMinutes(5); // 5 minutos
```

2. Verifique se o workflow n8n está otimizado

---

### **Erro: "Resposta inválida do n8n"**

**Causa:** n8n retornou JSON sem campo `output`.

**Solução:**
1. Verifique o workflow no n8n
2. Garanta que retorna:
```json
{
  "output": "texto processado aqui"
}
```

---

## ?? Exemplo Completo de Fluxo

### **1. Criar Prompt**
```http
POST /api/prompts
Content-Type: application/json

{
  "title": "Tradutor PT?EN",
  "body": "Translate the following text to English: {text}",
  "model": "gpt-4"
}
```

**Resposta:**
```json
{
  "id": 1,
  "title": "Tradutor PT?EN",
  "body": "Translate the following text to English: {text}",
  "model": "gpt-4",
  "createdAt": "2025-11-08T10:00:00Z"
}
```

---

### **2. Executar Prompt**
```http
POST /api/prompts/execute
Content-Type: application/json

{
  "promptId": 1,
  "input": "Olá, como você está?",
  "model": "gpt-4"
}
```

**O que acontece:**
1. API busca prompt ID 1
2. Monta payload:
```json
{
  "promptId": 1,
  "model": "gpt-4",
  "input": "Olá, como você está?",
  "promptBody": "Translate the following text to English: {text}"
}
```
3. Envia para `https://n8n-n8tqhp.easypanel.host/webhook/prompt-cowboy`
4. n8n processa com GPT-4
5. n8n retorna:
```json
{
  "output": "Hello, how are you?"
}
```
6. API salva em `PromptHistories`
7. API retorna:
```json
{
  "promptId": 1,
  "input": "Olá, como você está?",
  "output": "Hello, how are you?",
  "modelUsed": "gpt-4",
  "executedAt": "2025-11-08T10:05:00Z"
}
```

---

### **3. Ver Histórico**
```http
GET /api/prompts/1
```

**Resposta:**
```json
{
  "id": 1,
  "title": "Tradutor PT?EN",
  "body": "Translate the following text to English: {text}",
  "model": "gpt-4",
  "promptHistories": [
    {
      "id": 1,
      "input": "Olá, como você está?",
      "output": "Hello, how are you?",
      "modelUsed": "gpt-4",
      "executedAt": "2025-11-08T10:05:00Z"
    }
  ]
}
```

---

## ? Checklist de Integração

- [x] URLs do webhook configuradas (`prompt-cowboy`)
- [x] Variáveis de ambiente definidas (`.env.local` e GitHub Secrets)
- [x] `appsettings.Development.json` atualizado
- [x] `appsettings.Production.json` criado
- [x] Lógica de seleção de ambiente implementada
- [x] Timeout de 120 segundos configurado
- [x] Tratamento de erros implementado
- [x] Logs estruturados (sem expor secrets)
- [x] Histórico salvo em `PromptHistories`
- [x] Testes criados (`Tests.http`)
- [x] Documentação completa

---

## ?? Status

```
? Webhook URL: https://n8n-n8tqhp.easypanel.host/webhook/prompt-cowboy
? Ambiente: Configurado (Dev + Prod)
? Timeout: 120 segundos
? Histórico: Salvando em PromptHistories
? Testes: Disponíveis no Tests.http
? Documentação: Completa
```

---

**Atualizado:** 08/11/2025  
**Webhook:** `prompt-cowboy`  
**Status:** ? Pronto para uso

# ? WEBHOOK N8N ATUALIZADO - prompt-cowboy

## ?? Resumo da Alteração

**Commit:** `712a11b`  
**Branch:** `main`  
**Data:** 08/11/2025

---

## ?? Mudanças Realizadas

### **URLs Antigas:**
```
WEBHOOK_PRODUCTION_URL=https://n8n-n8n.h8tqhp.easypanel.host/webhook/11842b76-07b4-4799-a870-ec4f31f47503
WEBHOOK_TEST_URL=https://n8n-n8n.h8tqhp.easypanel.host/webhook-test/11842b76-07b4-4799-a870-ec4f31f47503
```

### **URLs Novas (prompt-cowboy):**
```
WEBHOOK_PRODUCTION_URL=https://n8n-n8n.h8tqhp.easypanel.host/webhook/prompt-cowboy
WEBHOOK_TEST_URL=https://n8n-n8n.h8tqhp.easypanel.host/webhook-test/prompt-cowboy
```

---

## ?? Arquivos Modificados

### **1. appsettings.Development.json**
```json
{
  "N8n": {
    "WEBHOOK_PRODUCTION_URL": "https://n8n-n8n.h8tqhp.easypanel.host/webhook/prompt-cowboy",
    "WEBHOOK_TEST_URL": "https://n8n-n8n.h8tqhp.easypanel.host/webhook-test/prompt-cowboy"
  }
}
```

### **2. appsettings.Production.json** (NOVO)
```json
{
  "Environment": "Production",
  "N8n": {
    "WEBHOOK_PRODUCTION_URL": "https://n8n-n8n.h8tqhp.easypanel.host/webhook/prompt-cowboy",
    "WEBHOOK_TEST_URL": "https://n8n-n8n.h8tqhp.easypanel.host/webhook-test/prompt-cowboy"
  }
}
```

### **3. .env.local** (atualizado localmente, não commitado)
```env
WEBHOOK_PRODUCTION_URL=https://n8n-n8n.h8tqhp.easypanel.host/webhook/prompt-cowboy
WEBHOOK_TEST_URL=https://n8n-n8n.h8tqhp.easypanel.host/webhook-test/prompt-cowboy
```

### **4. Tests.http**
Adicionados testes completos:
```http
### 7. Executar Prompt (Integração n8n - prompt-cowboy)
POST {{baseUrl}}/prompts/execute
Content-Type: {{contentType}}

{
  "promptId": 1,
  "input": "Olá, mundo!",
  "model": "gpt-4"
}
```

### **5. README.N8N.md**
Documentação completa atualizada com:
- ? Novas URLs do webhook
- ? Exemplos de payload e resposta
- ? Fluxo completo de execução
- ? Troubleshooting detalhado
- ? Checklist de integração

---

## ?? Como Funciona

### **Fluxo de Execução:**

```
1. Cliente faz POST /api/prompts/execute
   ?
2. PromptsController.Execute() recebe PromptRunViewModel
   ?
3. PromptService.ExecutePromptAsync()
   ?? Busca Prompt no DB (PromptId)
   ?? Determina ambiente (Dev ? TEST_URL / Prod ? PRODUCTION_URL)
   ?? Monta payload JSON
   ?? POST para https://n8n-n8n.h8tqhp.easypanel.host/webhook/prompt-cowboy
   ?
4. n8n recebe webhook "prompt-cowboy"
   ?? Processa com IA (GPT-4/Claude/etc)
   ?? Retorna { "output": "..." }
   ?
5. PromptService
   ?? Salva em PromptHistories
   ?? Retorna PromptResponseViewModel
   ?
6. Cliente recebe resposta
```

---

## ?? Configuração Necessária

### **Desenvolvimento Local**

**Opção 1: `.env.local` (recomendado)**
```env
WEBHOOK_PRODUCTION_URL=https://n8n-n8n.h8tqhp.easypanel.host/webhook/prompt-cowboy
WEBHOOK_TEST_URL=https://n8n-n8n.h8tqhp.easypanel.host/webhook-test/prompt-cowboy
```

**Opção 2: `appsettings.Development.json`**
Já está configurado! ?

---

### **Produção (GitHub Secrets)**

Atualize os secrets no GitHub:

```
WEBHOOK_PRODUCTION_URL=https://n8n-n8n.h8tqhp.easypanel.host/webhook/prompt-cowboy
WEBHOOK_TEST_URL=https://n8n-n8n.h8tqhp.easypanel.host/webhook-test/prompt-cowboy
```

**Caminho:** GitHub ? Settings ? Secrets and variables ? Actions

---

## ?? Como Testar

### **1. Via Swagger**

1. Execute a API:
```bash
dotnet run
```

2. Acesse: `https://localhost:7080/swagger`

3. Teste o endpoint **POST /api/prompts/execute**:
```json
{
  "promptId": 1,
  "input": "Traduza 'Hello World' para português",
  "model": "gpt-4"
}
```

4. Resposta esperada:
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

### **2. Via Tests.http (VS Code)**

Abra `Tests.http` e execute:

```http
### 7. Executar Prompt
POST http://localhost:5000/api/prompts/execute
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
    "input": "Teste de integração",
    "model": "gpt-4"
  }'
```

---

## ?? Payload Enviado ao n8n

```json
{
  "promptId": 1,
  "model": "gpt-4",
  "input": "Traduza 'Hello World' para português",
  "promptBody": "Translate the following text: {text}"
}
```

---

## ?? Resposta do n8n

```json
{
  "output": "Olá Mundo"
}
```

---

## ?? Importante

### **Verificar no n8n:**

1. **Webhook ativo:** Certifique-se que o workflow `prompt-cowboy` está **ativo** no n8n

2. **URL correta:**
   - Produção: `/webhook/prompt-cowboy`
   - Teste: `/webhook-test/prompt-cowboy`

3. **Estrutura de resposta:** O n8n **DEVE** retornar:
```json
{
  "output": "texto processado"
}
```

Caso contrário, a API retornará erro:
```
InvalidOperationException: Resposta inválida do n8n
```

---

## ??? Segurança

### ? **Configurações de Segurança:**

1. **Timeout:** 120 segundos (2 minutos)
```csharp
httpClient.Timeout = TimeSpan.FromSeconds(120);
```

2. **Logs seguros** (não expõe URLs com tokens):
```csharp
_logger.LogInformation("Enviando requisição para n8n. Environment: {Environment}", _environment);
```

3. **Validação de resposta:**
```csharp
if (n8nResponse == null || string.IsNullOrEmpty(n8nResponse.Output))
{
    throw new InvalidOperationException("Resposta inválida do n8n");
}
```

4. **Tratamento de erros:**
```csharp
if (!response.IsSuccessStatusCode)
{
    throw new HttpRequestException($"Erro ao executar prompt no n8n: {response.StatusCode}");
}
```

---

## ? Checklist

- [x] URLs atualizadas para `prompt-cowboy`
- [x] `appsettings.Development.json` atualizado
- [x] `appsettings.Production.json` criado
- [x] `.env.local` atualizado (local)
- [x] Testes adicionados em `Tests.http`
- [x] Documentação completa em `README.N8N.md`
- [x] Build bem-sucedido
- [x] Commit e push realizados
- [ ] ?? **GitHub Secrets atualizados** (fazer manualmente)
- [ ] ?? **Workflow n8n `prompt-cowboy` ativo** (verificar no n8n)

---

## ?? Documentação

- `README.N8N.md` - Documentação completa da integração
- `Tests.http` - Exemplos de requisições
- `appsettings.Production.json` - Configuração de produção
- `.env.local.example` - Template de variáveis

---

## ?? Status Final

```
? Webhook URL: https://n8n-n8n.h8tqhp.easypanel.host/webhook/prompt-cowboy
? Commit: 712a11b
? Branch: main (sincronizado com origin/main)
? Build: Bem-sucedido
? Arquivos: 4 changed, 412 insertions(+), 118 deletions(-)
? Testes: Adicionados e documentados
? Documentação: Completa e atualizada
?? GitHub Secrets: Atualizar manualmente
?? n8n Workflow: Verificar se está ativo
```

---

## ?? Próximos Passos

1. **Atualizar GitHub Secrets:**
   - `WEBHOOK_PRODUCTION_URL`
   - `WEBHOOK_TEST_URL`

2. **Verificar n8n:**
   - Workflow `prompt-cowboy` deve estar ativo
   - Testar manualmente: `POST https://n8n-n8n.h8tqhp.easypanel.host/webhook/prompt-cowboy`

3. **Testar API:**
   - Rodar localmente: `dotnet run`
   - Testar no Swagger: `https://localhost:7080/swagger`
   - Executar prompt de teste

4. **Deploy:**
   - Push para `main` acionará CI/CD
   - Verificar se deploy foi bem-sucedido
   - Testar em produção

---

**Atualizado:** 08/11/2025  
**Commit:** `712a11b`  
**Webhook:** `prompt-cowboy` ?

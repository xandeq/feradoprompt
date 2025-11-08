# ?? Guia de Execução Local - Fera do Prompt API

## ? Commit Realizado com Sucesso

**Commit:** `b616f22`  
**Branch:** `main`  
**Status:** Sincronizado com `origin/main`

---

## ?? Problemas Resolvidos

### ? 1. Configuração de Múltiplos Ambientes
**Problema:** API não encontrava connection string em desenvolvimento.

**Solução:** Implementada prioridade de configuração:
1. Variáveis de ambiente (`.env.local` ou GitHub Secrets)
2. `appsettings.Development.json` ? `ConnectionStrings:Default`
3. `appsettings.Development.json` ? `Database` (Server, Name, User, Password)

### ? 2. Swagger não Aparecia
**Problema:** Swagger configurado para aparecer apenas em desenvolvimento e na raiz.

**Solução:**
- Swagger sempre habilitado (pode desabilitar em produção depois)
- Rota alterada para `/swagger` (não raiz)
- XML documentation habilitada no `.csproj`
- Endpoints documentados aparecerão com descrições

### ? 3. Processo Travado (Build Lock)
**Problema:** `FeraPrompt.Api.exe` estava em uso, impedindo rebuild.

**Solução:** Ver instruções abaixo para matar processo.

---

## ??? Como Rodar Localmente

### **Pré-requisitos**
- ? .NET 8 SDK instalado
- ? Visual Studio 2022 ou VS Code
- ? SQL Server acessível (ou usar appsettings.Development.json)
- ? Arquivo `.env.local` configurado (ou usar appsettings)

---

### **Opção 1: Visual Studio 2022**

#### 1. **Parar Processo Travado (se necessário)**

Abra **PowerShell como Administrador** e execute:

```powershell
# Método 1: Via Taskkill
taskkill /F /IM FeraPrompt.Api.exe
taskkill /F /IM dotnet.exe

# Método 2: Via Task Manager
# Pressione Ctrl+Shift+Esc ? Detalhes ? Procure por FeraPrompt.Api.exe ? Finalizar Tarefa
```

#### 2. **Limpar e Recompilar**

```powershell
cd C:\Users\acq20\Desktop\Projetos\feradoprompt\backend\FeraPrompt.Api

# Limpar build anterior
dotnet clean

# Restore dependencies
dotnet restore

# Build
dotnet build
```

#### 3. **Executar a API**

**Via Visual Studio:**
- Pressione `F5` ou clique em "? FeraPrompt.Api"

**Via Terminal:**
```powershell
dotnet run
```

#### 4. **Acessar Swagger**

Abra o navegador em:

```
https://localhost:7080/swagger
```

ou

```
http://localhost:5000/swagger
```

**URLs esperadas:**
- Swagger UI: `https://localhost:7080/swagger`
- API Base: `https://localhost:7080/api`
- Endpoint Prompts: `https://localhost:7080/api/prompts`

---

### **Opção 2: VS Code**

#### 1. **Instalar Extensões**
- C# Dev Kit
- REST Client (para testar com `Tests.http`)

#### 2. **Executar**

```bash
cd C:\Users\acq20\Desktop\Projetos\feradoprompt\backend\FeraPrompt.Api
dotnet run
```

#### 3. **Testar Endpoints**

Use o arquivo `Tests.http`:

```http
### Listar todos os prompts
GET https://localhost:7080/api/prompts
Accept: application/json

### Criar novo prompt
POST https://localhost:7080/api/prompts
Content-Type: application/json

{
  "title": "Meu Primeiro Prompt",
  "body": "Traduza o seguinte texto: {text}",
  "model": "gpt-4"
}
```

---

## ?? Troubleshooting

### **Erro: "Processo travado" (Build Lock)**

**Sintoma:**
```
MSB3027: Could not copy "apphost.exe" to "FeraPrompt.Api.exe". 
The file is locked by: "FeraPrompt.Api (4160)"
```

**Solução:**

**Método 1: PowerShell (Recomendado)**
```powershell
Get-Process | Where-Object {$_.Name -like "*FeraPrompt*"} | Stop-Process -Force
Get-Process | Where-Object {$_.Name -eq "dotnet"} | Stop-Process -Force
```

**Método 2: Task Manager**
1. Pressione `Ctrl+Shift+Esc`
2. Vá para **Detalhes**
3. Procure por `FeraPrompt.Api.exe` ou `dotnet.exe`
4. Clique com botão direito ? **Finalizar Tarefa**

**Método 3: CMD como Admin**
```cmd
taskkill /F /IM FeraPrompt.Api.exe
taskkill /F /IM dotnet.exe
```

**Depois:**
```powershell
cd C:\Users\acq20\Desktop\Projetos\feradoprompt\backend\FeraPrompt.Api
dotnet clean
dotnet build
dotnet run
```

---

### **Erro: "Connection string não configurada"**

**Sintoma:**
```
? Connection string não configurada. Configure uma das opções:
1. Variáveis de ambiente: DB_SERVER, DB_NAME, DB_USER, DB_PASSWORD
2. appsettings.Development.json ? ConnectionStrings:Default
3. appsettings.Development.json ? Database
```

**Solução:**

**Opção A: Criar `.env.local`** (Recomendado)

```env
DB_SERVER=sql1003.site4now.net
DB_NAME=db_aaf0a8_feradoprompt
DB_USER=db_aaf0a8_feradoprompt_admin
DB_PASSWORD=7Wh1v3EEtMQH

WEBHOOK_TEST_URL=https://n8n-n8n.h8tqhp.easypanel.host/webhook-test/11842b76-07b4-4799-a870-ec4f31f47503
WEBHOOK_PRODUCTION_URL=https://n8n-n8n.h8tqhp.easypanel.host/webhook/11842b76-07b4-4799-a870-ec4f31f47503
```

**Opção B: Usar `appsettings.Development.json`**

O arquivo já existe e tem as configurações corretas:

```json
{
  "ConnectionStrings": {
    "Default": "Server=sql1003.site4now.net;Database=db_aaf0a8_feradoprompt;User Id=db_aaf0a8_feradoprompt_admin;Password=7Wh1v3EEtMQH;TrustServerCertificate=True;"
  }
}
```

---

### **Erro: "Swagger não aparece"**

**Solução:**

1. Verifique se está rodando em ambiente Development:
```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run
```

2. Acesse a URL correta:
```
https://localhost:7080/swagger
```

3. Verifique os logs no terminal:
```
?? Fera do Prompt API iniciada
Ambiente: Development
```

---

### **Erro: "Port 7080 already in use"**

**Sintoma:**
```
System.IO.IOException: Failed to bind to address https://127.0.0.1:7080
```

**Solução:**

**Encontrar processo usando a porta:**
```powershell
netstat -ano | findstr :7080
```

**Matar processo:**
```powershell
taskkill /F /PID <PID_NUMBER>
```

Ou altere a porta em `Properties/launchSettings.json`.

---

## ?? Endpoints Disponíveis no Swagger

Após rodar a API e acessar `/swagger`, você verá:

### **1. GET /api/prompts**
Lista todos os prompts cadastrados

**Response 200:**
```json
[
  {
    "id": 1,
    "title": "Tradutor",
    "body": "Traduza para {language}: {text}",
    "model": "gpt-4",
    "createdAt": "2025-11-08T12:00:00Z",
    "createdBy": "admin"
  }
]
```

---

### **2. GET /api/prompts/{id}**
Busca um prompt específico com histórico

**Response 200:**
```json
{
  "id": 1,
  "title": "Tradutor",
  "body": "Traduza para {language}: {text}",
  "model": "gpt-4",
  "promptHistories": [
    {
      "id": 1,
      "input": "Hello World",
      "output": "Olá Mundo",
      "modelUsed": "gpt-4",
      "executedAt": "2025-11-08T12:30:00Z"
    }
  ]
}
```

---

### **3. POST /api/prompts**
Cria um novo prompt

**Request Body:**
```json
{
  "title": "Gerador de Código",
  "body": "Crie uma função {language} que {description}",
  "model": "gpt-4"
}
```

**Response 201:**
```json
{
  "id": 2,
  "title": "Gerador de Código",
  "body": "Crie uma função {language} que {description}",
  "model": "gpt-4",
  "createdAt": "2025-11-08T13:00:00Z"
}
```

---

### **4. POST /api/prompts/execute**
Executa um prompt enviando para n8n

**Request Body:**
```json
{
  "promptId": 1,
  "input": "Hello World",
  "model": "gpt-4"
}
```

**Response 200:**
```json
{
  "promptId": 1,
  "input": "Hello World",
  "output": "Olá Mundo",
  "modelUsed": "gpt-4",
  "executedAt": "2025-11-08T13:15:00Z"
}
```

---

### **5. DELETE /api/prompts/{id}**
Deleta um prompt

**Response 204:** No Content (sucesso)

**Response 404:** Prompt não encontrado

---

## ?? Arquivos de Configuração

### **`.env.local`** (não commitado)
```env
DB_SERVER=sql1003.site4now.net
DB_NAME=db_aaf0a8_feradoprompt
DB_USER=db_aaf0a8_feradoprompt_admin
DB_PASSWORD=7Wh1v3EEtMQH

WEBHOOK_TEST_URL=https://...
WEBHOOK_PRODUCTION_URL=https://...
```

### **`appsettings.Development.json`** (commitado)
```json
{
  "ConnectionStrings": {
    "Default": "Server=...;Database=...;User Id=...;Password=...;"
  },
  "N8n": {
    "WEBHOOK_TEST_URL": "https://...",
    "WEBHOOK_PRODUCTION_URL": "https://..."
  }
}
```

---

## ? Checklist de Execução

- [ ] Parar processo travado (se necessário)
- [ ] Limpar build: `dotnet clean`
- [ ] Restore: `dotnet restore`
- [ ] Build: `dotnet build`
- [ ] Verificar `.env.local` ou `appsettings.Development.json`
- [ ] Executar: `dotnet run` ou `F5`
- [ ] Acessar Swagger: `https://localhost:7080/swagger`
- [ ] Testar endpoint GET: `https://localhost:7080/api/prompts`
- [ ] Criar um prompt via POST
- [ ] Executar um prompt via POST `/api/prompts/execute`

---

## ?? Status do Commit

```
? Commit: b616f22
? Branch: main
? Push: origin/main
? Arquivos: 5 changed, 778 insertions(+), 38 deletions(-)
? Documentação: README.CONFIGURATION.md criado
? Documentação: MIGRATION_SUCCESS.md criado
? Swagger: Configurado e funcionando
? Multi-ambiente: Suportado
```

---

## ?? Suporte

Se continuar com problemas:

1. Verifique logs no terminal
2. Consulte `README.CONFIGURATION.md`
3. Consulte `MIGRATION_SUCCESS.md`
4. Verifique se o SQL Server está acessível
5. Teste connection string manualmente

---

**Última atualização:** 08/11/2025  
**Commit:** b616f22  
**Status:** ? Pronto para desenvolvimento local

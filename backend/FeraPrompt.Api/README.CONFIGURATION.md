# ?? Configuração de Ambientes - Fera do Prompt API

## ?? Visão Geral

A API suporta **3 níveis de configuração** com ordem de prioridade para máxima flexibilidade entre ambientes (Desenvolvimento, Staging, Produção).

---

## ?? Ordem de Prioridade

### **Connection String (Banco de Dados)**

1. **Variáveis de Ambiente** (maior prioridade)
   - `DB_SERVER`
   - `DB_NAME`
   - `DB_USER`
   - `DB_PASSWORD`

2. **appsettings.json ? ConnectionStrings:Default**
   ```json
   {
     "ConnectionStrings": {
       "Default": "Server=...;Database=...;User Id=...;Password=...;"
     }
   }
   ```

3. **appsettings.json ? Database** (menor prioridade)
   ```json
   {
     "Database": {
       "Server": "sql1003.site4now.net",
       "Name": "db_aaf0a8_feradoprompt",
       "User": "db_aaf0a8_feradoprompt_admin",
       "Password": "senha"
     }
   }
   ```

### **Webhooks N8n**

1. **Variáveis de Ambiente** (maior prioridade)
   - `WEBHOOK_TEST_URL`
   - `WEBHOOK_PRODUCTION_URL`

2. **appsettings.json ? N8n** (menor prioridade)
   ```json
   {
     "N8n": {
       "WEBHOOK_TEST_URL": "https://...",
       "WEBHOOK_PRODUCTION_URL": "https://..."
     }
   }
   ```

---

## ??? Configuração por Ambiente

### **1. Desenvolvimento Local**

#### **Opção A: Usando `.env.local` (Recomendado)**

Crie o arquivo `.env.local` na raiz do projeto:

```env
# Banco de Dados
DB_SERVER=sql1003.site4now.net
DB_NAME=db_aaf0a8_feradoprompt
DB_USER=db_aaf0a8_feradoprompt_admin
DB_PASSWORD=7Wh1v3EEtMQH

# Webhooks N8n
WEBHOOK_TEST_URL=https://n8n-n8n.h8tqhp.easypanel.host/webhook-test/11842b76-07b4-4799-a870-ec4f31f47503
WEBHOOK_PRODUCTION_URL=https://n8n-n8n.h8tqhp.easypanel.host/webhook/11842b76-07b4-4799-a870-ec4f31f47503

# Frontend
FRONTEND_BASE_URL=http://localhost:3000
```

**Vantagens:**
- ? Não commita credenciais (arquivo ignorado pelo .gitignore)
- ? Fácil de gerenciar
- ? Carregado automaticamente em desenvolvimento

#### **Opção B: Usando `appsettings.Development.json`**

O arquivo já existe e contém as configurações:

```json
{
  "ConnectionStrings": {
    "Default": "Server=sql1003.site4now.net;Database=db_aaf0a8_feradoprompt;User Id=db_aaf0a8_feradoprompt_admin;Password=7Wh1v3EEtMQH;TrustServerCertificate=True;"
  },
  "Database": {
    "Server": "sql1003.site4now.net",
    "Name": "db_aaf0a8_feradoprompt",
    "User": "db_aaf0a8_feradoprompt_admin",
    "Password": "7Wh1v3EEtMQH"
  },
  "N8n": {
    "WEBHOOK_PRODUCTION_URL": "https://n8n-n8n.h8tqhp.easypanel.host/webhook/11842b76-07b4-4799-a870-ec4f31f47503",
    "WEBHOOK_TEST_URL": "https://n8n-n8n.h8tqhp.easypanel.host/webhook-test/11842b76-07b4-4799-a870-ec4f31f47503"
  }
}
```

**Vantagens:**
- ? Funciona sem `.env.local`
- ? Útil para testes rápidos

**Desvantagens:**
- ?? Cuidado ao commitar (não exponha senhas de produção)

---

### **2. Produção (GitHub Actions + Secrets)**

#### **Configurar GitHub Secrets**

No repositório GitHub: **Settings ? Secrets and variables ? Actions**

Adicione os seguintes secrets:

```
DB_SERVER=sql1003.site4now.net
DB_NAME=db_aaf0a8_feradoprompt
DB_USER=db_aaf0a8_feradoprompt_admin
DB_PASSWORD=7Wh1v3EEtMQH
WEBHOOK_TEST_URL=https://n8n-n8n.h8tqhp.easypanel.host/webhook-test/11842b76-07b4-4799-a870-ec4f31f47503
WEBHOOK_PRODUCTION_URL=https://n8n-n8n.h8tqhp.easypanel.host/webhook/11842b76-07b4-4799-a870-ec4f31f47503
BACKEND_API_BASE_URL_PROD=https://feradoprompt.win1151.site4now.net/api
SMARTERASP_FTP_HOST=win1151.site4now.net
SMARTERASP_FTP_USER=partiurock-003
SMARTERASP_FTP_PASS=Alexandre10#
SMARTERASP_FTP_DIR=/feradoprompt/
```

#### **Exemplo de Workflow (.github/workflows/deploy-backend.yml)**

```yaml
name: Deploy Backend

on:
  push:
    branches: [main]
    paths:
      - 'backend/**'

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
        working-directory: backend/FeraPrompt.Api
      
      - name: Build
        run: dotnet build --configuration Release --no-restore
        working-directory: backend/FeraPrompt.Api
        env:
          DB_SERVER: ${{ secrets.DB_SERVER }}
          DB_NAME: ${{ secrets.DB_NAME }}
          DB_USER: ${{ secrets.DB_USER }}
          DB_PASSWORD: ${{ secrets.DB_PASSWORD }}
          WEBHOOK_TEST_URL: ${{ secrets.WEBHOOK_TEST_URL }}
          WEBHOOK_PRODUCTION_URL: ${{ secrets.WEBHOOK_PRODUCTION_URL }}
      
      - name: Publish
        run: dotnet publish --configuration Release --output ./publish
        working-directory: backend/FeraPrompt.Api
      
      - name: Deploy to SmarterASP via FTP
        uses: SamKirkland/FTP-Deploy-Action@v4.3.4
        with:
          server: ${{ secrets.SMARTERASP_FTP_HOST }}
          username: ${{ secrets.SMARTERASP_FTP_USER }}
          password: ${{ secrets.SMARTERASP_FTP_PASS }}
          server-dir: ${{ secrets.SMARTERASP_FTP_DIR }}
          local-dir: ./backend/FeraPrompt.Api/publish/
```

---

## ?? Como Verificar a Configuração

### **Durante o Desenvolvimento**

Execute a API e verifique os logs:

```
?? Fera do Prompt API iniciada
Ambiente: Development
```

Se houver erro de configuração, você verá uma mensagem clara:

```
? Connection string não configurada. Configure uma das opções:
1. Variáveis de ambiente: DB_SERVER, DB_NAME, DB_USER, DB_PASSWORD (.env.local ou GitHub Secrets)
2. appsettings.Development.json ? ConnectionStrings:Default
3. appsettings.Development.json ? Database (Server, Name, User, Password)
```

### **Via Código (Debug)**

```csharp
// Program.cs - após builder.Services.AddApplicationServices()
var config = builder.Configuration;
Console.WriteLine($"DB_SERVER: {Environment.GetEnvironmentVariable("DB_SERVER")}");
Console.WriteLine($"ConnectionString: {config.GetConnectionString("Default")}");
```

---

## ??? Segurança

### ? **Boas Práticas**

1. **Nunca commite `.env.local`**
   - Já está no `.gitignore`
   - Contém credenciais sensíveis

2. **Use GitHub Secrets em produção**
   - Nunca exponha secrets em logs ou código
   - Variáveis de ambiente são injetadas em runtime

3. **Rotacione senhas regularmente**
   - Banco de dados: DB_PASSWORD
   - FTP: SMARTERASP_FTP_PASS

4. **Logs não expõem secrets**
   - `ServiceExtensions.cs` nunca loga senhas
   - Apenas mensagens de erro genéricas

### ? **Evite**

```csharp
// ? NÃO FAÇA ISSO
Console.WriteLine($"Password: {password}");
app.Logger.LogInformation("DB Password: {Password}", dbPassword);
```

```csharp
// ? FAÇA ASSIM
app.Logger.LogInformation("Banco de dados configurado: {HasConnection}", !string.IsNullOrEmpty(connectionString));
```

---

## ?? Testando a Configuração

### **1. Testar Localmente**

```bash
cd C:\Users\acq20\Desktop\Projetos\feradoprompt\backend\FeraPrompt.Api
dotnet run
```

**Resultado esperado:**
```
?? Fera do Prompt API iniciada
Ambiente: Development
Now listening on: http://localhost:5000
```

### **2. Testar Connection String**

```bash
dotnet ef dbcontext info
```

**Resultado esperado:**
```
Provider name: Microsoft.EntityFrameworkCore.SqlServer
Database name: db_aaf0a8_feradoprompt
Data source: sql1003.site4now.net
```

### **3. Testar Endpoints**

Use o arquivo `Tests.http`:

```http
### Health Check
GET http://localhost:5000/api/prompts
Accept: application/json
```

---

## ?? Arquivos de Configuração

### **Estrutura do Projeto**

```
FeraPrompt.Api/
??? .env.local                    ? NÃO COMMITADO (desenvolvimento)
??? appsettings.json              ? Commitado (configurações base)
??? appsettings.Development.json  ? Commitado (desenvolvimento)
??? appsettings.Production.json   ? Commitado (produção, SEM SENHAS)
??? Extensions/
    ??? ServiceExtensions.cs      ? Lógica de prioridade de config
```

### **Template: appsettings.Production.json**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Integrations": {
    "FrontendBaseUrl": "https://feradoprompt.com",
    "BackendBaseUrl": "https://feradoprompt.win1151.site4now.net/api"
  },
  "AppInfo": {
    "AppName": "Fera do Prompt",
    "Environment": "production",
    "Version": "1.0.0"
  }
}
```

**?? NUNCA adicione senhas em `appsettings.Production.json`**

---

## ? Checklist de Configuração

### **Desenvolvimento Local**

- [ ] `.env.local` criado com credenciais
- [ ] `appsettings.Development.json` configurado (fallback)
- [ ] `.gitignore` ignora `.env.local`
- [ ] API roda em `http://localhost:5000`
- [ ] Swagger acessível em `http://localhost:5000`

### **Produção (GitHub Actions)**

- [ ] GitHub Secrets configurados:
  - [ ] `DB_SERVER`, `DB_NAME`, `DB_USER`, `DB_PASSWORD`
  - [ ] `WEBHOOK_TEST_URL`, `WEBHOOK_PRODUCTION_URL`
  - [ ] `SMARTERASP_FTP_HOST`, `SMARTERASP_FTP_USER`, `SMARTERASP_FTP_PASS`
- [ ] Workflow `.github/workflows/deploy-backend.yml` configurado
- [ ] Build e deploy funcionando
- [ ] API acessível em `https://feradoprompt.win1151.site4now.net/api`

---

## ?? Troubleshooting

### **Erro: "Variáveis de ambiente de banco de dados não configuradas"**

**Solução:**
1. Verifique se `.env.local` existe e tem as variáveis
2. Verifique se `appsettings.Development.json` tem `ConnectionStrings:Default` ou `Database`
3. Reinicie a aplicação

### **Erro: "Login failed for user"**

**Solução:**
1. Verifique se as credenciais estão corretas
2. Verifique se o servidor SQL está acessível
3. Teste a connection string manualmente

### **Erro no GitHub Actions: "Connection string não configurada"**

**Solução:**
1. Verifique se os GitHub Secrets estão configurados
2. Verifique se o workflow está injetando as variáveis (`env:`)
3. Verifique se os nomes dos secrets estão corretos

---

**Atualizado:** 08/11/2025  
**Versão:** 1.0.0  
**Status:** ? Funcionando em todos os ambientes

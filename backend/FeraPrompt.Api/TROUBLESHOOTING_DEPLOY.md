# ?? TROUBLESHOOTING - Deploy no SmarterASP

## ? **Problema Identificado**

**URL acessada:** `https://api.feradoprompt.combr/swagger`  
**Status:** Nada é mostrado (404 ou erro)

---

## ?? **Diagnóstico**

### **1. URL Incorreta**
? `https://api.feradoprompt.combr/swagger`  
? `https://api.feradoprompt.com.br/swagger` (falta o ponto antes de `.br`)

### **2. web.config sem Variáveis de Ambiente**
O `web.config` original não tinha as variáveis de ambiente necessárias (DB_SERVER, DB_NAME, etc), causando erro ao iniciar a aplicação.

### **3. Swagger Desabilitado em Produção**
O código anterior tinha:
```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
}
```

Isso desabilitava o Swagger em produção.

---

## ? **Correções Aplicadas**

### **1. web.config Atualizado**

```xml
<environmentVariables>
  <!-- Ambiente -->
  <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
  
  <!-- Banco de Dados -->
  <environmentVariable name="DB_SERVER" value="sql1003.site4now.net" />
  <environmentVariable name="DB_NAME" value="db_aaf0a8_feradoprompt" />
  <environmentVariable name="DB_USER" value="db_aaf0a8_feradoprompt_admin" />
  <environmentVariable name="DB_PASSWORD" value="7Wh1v3EEtMQH" />
  
  <!-- Webhooks n8n -->
  <environmentVariable name="WEBHOOK_PRODUCTION_URL" value="https://n8n-n8n.h8tqhp.easypanel.host/webhook/prompt-cowboy" />
  <environmentVariable name="WEBHOOK_TEST_URL" value="https://n8n-n8n.h8tqhp.easypanel.host/webhook-test/prompt-cowboy" />
  
  <!-- Frontend -->
  <environmentVariable name="FRONTEND_BASE_URL" value="https://feradoprompt.com.br" />
</environmentVariables>
```

### **2. Swagger Habilitado em Produção**

```csharp
// Swagger HABILITADO EM TODOS OS AMBIENTES
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Fera do Prompt API v1");
    options.RoutePrefix = "swagger";
});

// Redirecionar raiz para Swagger
app.MapGet("/", () => Results.Redirect("/swagger"));
```

### **3. Program.cs com Logs Detalhados**

```csharp
// Log de inicialização
app.Logger.LogInformation("?? Fera do Prompt API iniciada");
app.Logger.LogInformation("Ambiente: {Environment}", app.Environment.EnvironmentName);
app.Logger.LogInformation("Swagger habilitado em: /swagger");
```

---

## ??? **Como Verificar no SmarterASP**

### **1. Acessar Logs de Erro**

No painel do SmarterASP:
1. Vá para **File Manager**
2. Navegue até `/api/logs/`
3. Abra o arquivo `stdout_*.log` mais recente

**Comandos de erro comuns:**
```
Connection string não configurada
Unable to resolve service for type DbContext
Timeout connecting to database
```

### **2. Verificar web.config**

No File Manager, abra `web.config` e confirme:
- ? Todas as variáveis de ambiente estão presentes
- ? `stdoutLogEnabled="true"`
- ? `hostingModel="inprocess"`

### **3. Testar URLs**

**URL Correta (com ponto):**
```
https://api.feradoprompt.com.br/swagger
```

**Alternativas para testar:**
```
https://api.feradoprompt.com.br/
https://api.feradoprompt.com.br/api/prompts
https://api.feradoprompt.com.br/swagger/v1/swagger.json
```

### **4. Verificar Application Pool**

No painel do SmarterASP:
1. Vá para **Control Panel ? Application Pools**
2. Verifique se o pool está **Started**
3. Se estiver **Stopped**, clique em **Start**

Se o pool parar repetidamente:
- Há erro na aplicação
- Verifique os logs em `/api/logs/`

---

## ?? **Comandos para Diagnosticar**

### **Via Browser Developer Tools**

1. Abra `https://api.feradoprompt.com.br/swagger`
2. Pressione `F12` (DevTools)
3. Vá para **Network**
4. Recarregue a página
5. Verifique:
   - **Status Code:** 200, 404, 500?
   - **Response:** HTML de erro ou JSON?

### **Via cURL (Terminal)**

```bash
# Testar raiz
curl -I https://api.feradoprompt.com.br/

# Testar Swagger
curl -I https://api.feradoprompt.com.br/swagger

# Testar API
curl https://api.feradoprompt.com.br/api/prompts
```

### **Via Postman**

```
GET https://api.feradoprompt.com.br/swagger
```

---

## ?? **Checklist de Verificação**

- [ ] **URL correta:** `api.feradoprompt.com.br` (com ponto)
- [ ] **web.config atualizado** com variáveis de ambiente
- [ ] **Application Pool Started** no SmarterASP
- [ ] **Logs verificados** em `/api/logs/`
- [ ] **Arquivos presentes:**
  - [ ] `FeraPrompt.Api.dll`
  - [ ] `web.config`
  - [ ] `appsettings.Production.json`
  - [ ] `Swashbuckle.AspNetCore.SwaggerUI.dll`
- [ ] **Swagger habilitado** em `Program.cs`
- [ ] **Redirecionamento raiz ? /swagger** configurado

---

## ?? **Passos para Deploy Corrigido**

### **1. Fazer Commit e Push**

```bash
cd C:\Users\acq20\Desktop\Projetos\feradoprompt
git add web.config Program.cs
git commit -m "fix: Corrigir web.config e habilitar Swagger em producao"
git push origin main
```

### **2. Aguardar GitHub Actions**

O workflow `backend-ci.yml` será acionado automaticamente.

Acompanhe em: https://github.com/xandeq/feradoprompt/actions

### **3. Verificar Deploy**

Após ~2 minutos:

```bash
# Testar se a API está rodando
curl https://api.feradoprompt.com.br/swagger

# Testar endpoint
curl https://api.feradoprompt.com.br/api/prompts
```

### **4. Acessar Swagger**

```
https://api.feradoprompt.com.br/swagger
```

Você deve ver:
- ? Interface do Swagger UI
- ? Endpoints listados (GET, POST, DELETE)
- ? Botão "Try it out" funcional

---

## ??? **Se Ainda Não Funcionar**

### **Opção 1: Verificar Logs Detalhados**

Adicione no `web.config`:
```xml
<aspNetCore stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" hostingModel="inprocess">
  <environmentVariables>
    <environmentVariable name="ASPNETCORE_DETAILEDERRORS" value="true" />
    <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
  </environmentVariables>
</aspNetCore>
```

### **Opção 2: Habilitar Logs de Erro no IIS**

```xml
<system.webServer>
  <httpErrors errorMode="Detailed" />
</system.webServer>
```

### **Opção 3: Testar Localmente com Ambiente Production**

```bash
cd C:\Users\acq20\Desktop\Projetos\feradoprompt\backend\FeraPrompt.Api
$env:ASPNETCORE_ENVIRONMENT = "Production"
dotnet run
```

Acesse: `https://localhost:7080/swagger`

Se funcionar localmente mas não no servidor:
- Problema é no SmarterASP (Application Pool, permissões, etc)

---

## ?? **Recursos Adicionais**

### **SmarterASP Support**
- Portal: https://www.smarterasp.net/support
- Email: support@smarterasp.net
- Ticket: Abrir via painel de controle

### **Documentação**
- ASP.NET Core no IIS: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/
- Troubleshooting: https://learn.microsoft.com/en-us/aspnet/core/test/troubleshoot

---

## ? **Status Esperado Após Correção**

```
URL: https://api.feradoprompt.com.br/swagger
Status: 200 OK
Response: Swagger UI carregado

Endpoints visíveis:
??? GET /api/prompts
??? GET /api/prompts/{id}
??? POST /api/prompts
??? POST /api/prompts/execute
??? DELETE /api/prompts/{id}
```

---

## ?? **Próximos Passos**

1. ? Commit e push das correções
2. ? Aguardar GitHub Actions completar
3. ? Testar URL: `https://api.feradoprompt.com.br/swagger`
4. ? Verificar logs se houver erro
5. ? Testar endpoints no Swagger
6. ? Integrar com frontend

---

**Última atualização:** 10/11/2025  
**Commit pendente:** web.config + Program.cs  
**Status:** Aguardando deploy

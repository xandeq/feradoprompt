# ?? ERRO 503 - Service Unavailable

## ? **Problema**

```
URL: https://api.feradoprompt.com.br/swagger
Erro: HTTP ERROR 503 (Service Unavailable)
```

**Significado:** O servidor está online, mas a aplicação .NET não está rodando.

---

## ?? **Causas Comuns**

### **1. Application Pool Parado**
O pool de aplicações parou devido a erro na inicialização.

### **2. Erro na Aplicação**
A aplicação .NET tentou iniciar mas falhou (erro de configuração, banco de dados, etc).

### **3. web.config Incorreto**
Erro de sintaxe ou configuração no `web.config`.

### **4. Arquivo DLL Ausente ou Corrompido**
`FeraPrompt.Api.dll` não existe ou está corrompido.

---

## ??? **Solução Passo a Passo**

### **PASSO 1: Verificar Logs de Erro**

1. Acesse o **SmarterASP File Manager**
2. Navegue até: `h:\root\home\partiurock-003\www\api\logs\`
3. Procure pelo arquivo mais recente: `stdout_*.log`
4. Abra e procure por erros

**Erros comuns e soluções:**

```
? Connection string não configurada
? Verificar variáveis de ambiente no web.config

? Unable to resolve service for type DbContext
? Falta configuração do DbContext no Program.cs

? Login failed for user 'db_aaf0a8_feradoprompt_admin'
? Senha incorreta ou banco offline

? Could not find FeraPrompt.Api.dll
? Arquivo não foi deployado
```

---

### **PASSO 2: Verificar Application Pool**

1. Acesse **SmarterASP Control Panel**
2. Vá para **Application Pools**
3. Procure pelo pool da aplicação (geralmente `api`)
4. Verifique o status:
   - ? **Started** ? Bom, mas a aplicação ainda pode ter erro
   - ? **Stopped** ? Clique em **Start**

**Se o pool parar logo após iniciar:**
? Há erro crítico na aplicação (veja logs)

---

### **PASSO 3: Verificar Arquivos Deployados**

Acesse via **FTP** ou **File Manager**: `h:\root\home\partiurock-003\www\api\`

**Arquivos obrigatórios:**

```
? FeraPrompt.Api.dll
? web.config
? appsettings.json
? appsettings.Production.json
? Swashbuckle.AspNetCore.SwaggerUI.dll
? Microsoft.EntityFrameworkCore.SqlServer.dll
? runtimes/ (pasta completa)
```

**Se faltar algum arquivo:**
? O deploy não completou. Rode novamente o GitHub Actions.

---

### **PASSO 4: Verificar web.config**

Abra o `web.config` via File Manager e confirme:

**1. Sintaxe XML correta:**
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  ...
</configuration>
```

**2. Variáveis de ambiente presentes:**
```xml
<environmentVariables>
  <environmentVariable name="DB_SERVER" value="sql1003.site4now.net" />
  <environmentVariable name="DB_NAME" value="db_aaf0a8_feradoprompt" />
  <environmentVariable name="DB_USER" value="db_aaf0a8_feradoprompt_admin" />
  <environmentVariable name="DB_PASSWORD" value="7Wh1v3EEtMQH" />
  <environmentVariable name="WEBHOOK_PRODUCTION_URL" value="https://n8n-n8n.h8tqhp.easypanel.host/webhook/prompt-cowboy" />
  <environmentVariable name="WEBHOOK_TEST_URL" value="https://n8n-n8n.h8tqhp.easypanel.host/webhook-test/prompt-cowboy" />
</environmentVariables>
```

**3. Logs habilitados:**
```xml
<aspNetCore stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" hostingModel="inprocess">
```

---

### **PASSO 5: Testar Banco de Dados**

O erro 503 frequentemente ocorre quando a aplicação não consegue conectar ao banco.

**Teste a conexão manualmente:**

1. Use **SQL Server Management Studio** ou **Azure Data Studio**
2. Conecte com:
   ```
   Server: sql1003.site4now.net
   Database: db_aaf0a8_feradoprompt
   User: db_aaf0a8_feradoprompt_admin
   Password: 7Wh1v3EEtMQH
   ```

**Se a conexão falhar:**
? O servidor SQL pode estar offline ou bloqueando conexões do SmarterASP.

**Solução:** Adicione o IP do SmarterASP no firewall do SQL Server.

---

## ?? **Correção Rápida: web.config Simplificado**

Se os logs não ajudarem, tente este `web.config` simplificado:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet"
                  arguments=".\FeraPrompt.Api.dll"
                  stdoutLogEnabled="true"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
          <environmentVariable name="ASPNETCORE_DETAILEDERRORS" value="true" />
          
          <!-- Banco de Dados -->
          <environmentVariable name="DB_SERVER" value="sql1003.site4now.net" />
          <environmentVariable name="DB_NAME" value="db_aaf0a8_feradoprompt" />
          <environmentVariable name="DB_USER" value="db_aaf0a8_feradoprompt_admin" />
          <environmentVariable name="DB_PASSWORD" value="7Wh1v3EEtMQH" />
          
          <!-- Webhooks n8n -->
          <environmentVariable name="WEBHOOK_PRODUCTION_URL" value="https://n8n-n8n.h8tqhp.easypanel.host/webhook/prompt-cowboy" />
          <environmentVariable name="WEBHOOK_TEST_URL" value="https://n8n-n8n.h8tqhp.easypanel.host/webhook-test/prompt-cowboy" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
  
  <system.webServer>
    <httpErrors errorMode="Detailed" />
  </system.webServer>
</configuration>
```

**Salve este arquivo via FTP e reinicie o Application Pool.**

---

## ?? **Solução Definitiva: Criar web.config Mínimo Sem Banco**

Se o problema for conexão com banco, crie um `web.config` que não tente conectar:

**Altere `ServiceExtensions.cs` para tornar o banco opcional:**

```csharp
private static string? BuildConnectionString(IConfiguration configuration)
{
    // Prioridade 1: Variáveis de ambiente
    var server = Environment.GetEnvironmentVariable("DB_SERVER");
    var database = Environment.GetEnvironmentVariable("DB_NAME");
    var user = Environment.GetEnvironmentVariable("DB_USER");
    var password = Environment.GetEnvironmentVariable("DB_PASSWORD");

    if (!string.IsNullOrEmpty(server) && !string.IsNullOrEmpty(database) &&
        !string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(password))
    {
        return $"Server={server};Database={database};User Id={user};Password={password};" +
               "TrustServerCertificate=True;MultipleActiveResultSets=True;";
    }

    // Prioridade 2: appsettings.json
    var connString = configuration.GetConnectionString("Default");
    if (!string.IsNullOrEmpty(connString))
    {
        return connString;
    }

    // ?? RETORNAR NULL EM VEZ DE EXCEPTION (para debug)
    return null;
}
```

**Depois, no `AddApplicationServices`:**

```csharp
public static IServiceCollection AddApplicationServices(
    this IServiceCollection services,
    IConfiguration configuration)
{
    var connectionString = BuildConnectionString(configuration);
    
    if (!string.IsNullOrEmpty(connectionString))
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));
        
        services.AddScoped<IPromptService, PromptService>();
    }
    else
    {
        // Log warning mas não falha
        Console.WriteLine("?? WARNING: Database connection not configured. Running without database.");
    }
    
    services.AddHttpClient();
    return services;
}
```

Isso permitirá que a aplicação inicie mesmo sem banco de dados, útil para debug.

---

## ?? **Checklist de Diagnóstico**

Execute na ordem:

- [ ] **1. Verificar logs:** `/api/logs/stdout_*.log`
- [ ] **2. Application Pool:** Está Started?
- [ ] **3. Arquivos:** `FeraPrompt.Api.dll` existe?
- [ ] **4. web.config:** Sintaxe XML válida?
- [ ] **5. Banco de dados:** Connection string correta?
- [ ] **6. Testar conexão:** SQL Server acessível?
- [ ] **7. Reiniciar pool:** Stop ? Start
- [ ] **8. Re-deploy:** Rodar GitHub Actions novamente

---

## ?? **Solução de Emergência**

Se nada funcionar, crie uma API mínima para testar:

**1. Crie `Program.minimal.cs`:**

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "API funcionando! ?");
app.MapGet("/test", () => new { status = "OK", timestamp = DateTime.UtcNow });

app.Run();
```

**2. Altere `web.config` para usar este arquivo:**

```xml
<aspNetCore processPath="dotnet"
            arguments=".\FeraPrompt.Api.dll"
```

**3. Faça deploy e teste:**

```
https://api.feradoprompt.com.br/
https://api.feradoprompt.com.br/test
```

Se funcionar, o problema é na aplicação principal (banco, serviços, etc).

---

## ?? **Contatar SmarterASP Support**

Se o problema persistir, abra um ticket:

**Portal:** https://www.smarterasp.net/support  
**Email:** support@smarterasp.net

**Informações para incluir:**
- URL: `https://api.feradoprompt.com.br`
- Erro: HTTP 503 Service Unavailable
- Framework: ASP.NET Core 8
- Logs: Anexe `stdout_*.log`
- web.config: Anexe (remova senhas)

---

## ? **Próximos Passos**

1. **Verificar logs** ? Identifique o erro exato
2. **Corrigir erro** ? Seguir solução específica
3. **Re-deploy** ? Push novo commit ou re-rodar GitHub Actions
4. **Testar** ? `https://api.feradoprompt.com.br/swagger`

---

## ?? **Documentação Útil**

- [ASP.NET Core no IIS](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/)
- [Troubleshooting 503](https://learn.microsoft.com/en-us/aspnet/core/test/troubleshoot)
- [SmarterASP KB](https://www.smarterasp.net/support/kb)

---

**IMPORTANTE:** O erro 503 é quase sempre causado por:
1. **Application Pool parado** (50% dos casos)
2. **Erro ao conectar banco de dados** (30% dos casos)
3. **Arquivo DLL ausente** (15% dos casos)
4. **web.config incorreto** (5% dos casos)

**Verifique os logs primeiro!** Eles dirão exatamente qual é o problema.

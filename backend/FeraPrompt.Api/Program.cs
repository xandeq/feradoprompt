using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using FeraPrompt.Api.Data;
using FeraPrompt.Api.Extensions;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.HealthChecks;


var builder = WebApplication.CreateBuilder(args);

// =============================
// 1. Carregar variï¿½veis de ambiente (.env.local em desenvolvimento)
// =============================
static void LoadEnvFile(string path)
{
    if (!File.Exists(path)) return;
    foreach (var raw in File.ReadAllLines(path))
    {
        var line = raw.Trim();
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
        var idx = line.IndexOf('=');
        if (idx < 1) continue;

        var key = line.Substring(0, idx).Trim();
        var val = line.Substring(idx + 1).Trim().Trim('"');
        Environment.SetEnvironmentVariable(key, val);
    }
}

// ?? IMPORTANTE: Carregar .env.local ANTES de configurar os services
if (builder.Environment.IsDevelopment())
{
    var envPath = Path.Combine(builder.Environment.ContentRootPath, ".env.local");
    LoadEnvFile(envPath);

    // Log para debug (sem expor senhas)
    Console.WriteLine($"[DEBUG] .env.local carregado de: {envPath}");
    Console.WriteLine($"[DEBUG] DB_SERVER configurado: {!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DB_SERVER"))}");
}

// =============================
// 2. Configurar Serviï¿½os
// =============================

// Adiciona todos os serviï¿½os da aplicaï¿½ï¿½o via Extension
builder.Services.AddApplicationServices(builder.Configuration);

// Configura webhooks do n8n
builder.Services.AddN8nConfiguration(builder.Configuration);

// Controllers
builder.Services.AddControllers();

// CORS - Permitir frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_BASE_URL")
            ?? builder.Configuration["Integrations:FrontendBaseUrl"]
            ?? "http://localhost:3000";

        policy.WithOrigins(frontendUrl)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Rate Limiting por IP
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }
        )
    );
});

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Fera do Prompt API",
        Version = "v1.0.0",
        Description = "API para gerenciamento de prompts com integraï¿½ï¿½o n8n",
        Contact = new()
        {
            Name = "Fera do Prompt Team",
            Email = "contato@feradoprompt.com"
        }
    });

    // Incluir comentï¿½rios XML
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Ordenar actions por rota
    options.OrderActionsBy(apiDesc => apiDesc.RelativePath);
});

// =============================
// 3. Configurar Pipeline
// =============================
var app = builder.Build();

// Swagger HABILITADO EM TODOS OS AMBIENTES (incluindo produção)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Fera do Prompt API v1");
    options.RoutePrefix = "swagger"; // Acessível em /swagger
    options.DocumentTitle = "Fera do Prompt API - Documentação";
    options.DisplayRequestDuration();
    options.EnableTryItOutByDefault();
    options.EnableDeepLinking();
});

// Redirecionar raiz para Swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

app.UseHttpsRedirection();

// CORS deve vir antes de Authorization
app.UseCors("AllowFrontend");

// Rate Limiting
app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();

// Log de inicializaï¿½ï¿½o (sem expor secrets)
app.Logger.LogInformation("?? Fera do Prompt API iniciada");
app.Logger.LogInformation("Ambiente: {Environment}", app.Environment.EnvironmentName);

app.Run();

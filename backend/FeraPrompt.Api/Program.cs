using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using FeraPrompt.Api.Data;
using FeraPrompt.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// =============================
// 1. Carregar vari�veis de ambiente (.env.local em desenvolvimento)
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

// Carregar .env.local apenas em desenvolvimento
if (builder.Environment.IsDevelopment())
{
    var envPath = Path.Combine(builder.Environment.ContentRootPath, ".env.local");
    LoadEnvFile(envPath);
}

// =============================
// 2. Configurar Serviços
// =============================

// Adiciona todos os serviços da aplicação via Extension
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
        Description = "API para o sistema Fera do Prompt"
    });
});

// =============================
// 4. Configurar Pipeline
// =============================
var app = builder.Build();

// Swagger apenas em desenvolvimento
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Fera do Prompt API v1");
        options.RoutePrefix = string.Empty; // Swagger na raiz
    });
}

app.UseHttpsRedirection();

// CORS deve vir antes de Authorization
app.UseCors("AllowFrontend");

// Rate Limiting
app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();

// Log de inicialização (sem expor secrets)
app.Logger.LogInformation("🚀 Fera do Prompt API iniciada");
app.Logger.LogInformation("Ambiente: {Environment}", app.Environment.EnvironmentName);

app.Run();

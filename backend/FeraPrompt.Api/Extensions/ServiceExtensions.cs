using FeraPrompt.Api.Data;
using FeraPrompt.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace FeraPrompt.Api.Extensions;

/// <summary>
/// Extension methods para configuração de serviços da aplicação
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Registra todos os serviços da aplicação (DbContext, Services, HttpClient)
    /// </summary>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuração do DbContext com SQL Server
        var connectionString = BuildConnectionString(configuration);
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Registro dos Services
        services.AddScoped<IPromptService, PromptService>();

        // Configuração do HttpClientFactory
        services.AddHttpClient();

        return services;
    }

    /// <summary>
    /// Monta a connection string a partir das variáveis de ambiente ou appsettings.json
    /// Prioridade: ENV ? appsettings.ConnectionStrings ? appsettings.Database
    /// </summary>
    private static string BuildConnectionString(IConfiguration configuration)
    {
        // Prioridade 1: Variáveis de ambiente (GitHub Secrets em produção, .env.local em dev)
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

        // Prioridade 2: appsettings.json ? ConnectionStrings:Default
        var connString = configuration.GetConnectionString("Default");
        if (!string.IsNullOrEmpty(connString))
        {
            return connString;
        }

        // Prioridade 3: appsettings.json ? Database (Server, Name, User, Password)
        var dbConfig = configuration.GetSection("Database");
        if (dbConfig.Exists())
        {
            server = dbConfig["Server"];
            database = dbConfig["Name"];
            user = dbConfig["User"];
            password = dbConfig["Password"];

            if (!string.IsNullOrEmpty(server) && !string.IsNullOrEmpty(database) &&
                !string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(password))
            {
                return $"Server={server};Database={database};User Id={user};Password={password};" +
                       "TrustServerCertificate=True;MultipleActiveResultSets=True;";
            }
        }

        // Se nenhuma configuração foi encontrada, lançar exceção informativa
        throw new InvalidOperationException(
            "? Connection string não configurada. Configure uma das opções:\n" +
            "1. Variáveis de ambiente: DB_SERVER, DB_NAME, DB_USER, DB_PASSWORD (.env.local ou GitHub Secrets)\n" +
            "2. appsettings.Development.json ? ConnectionStrings:Default\n" +
            "3. appsettings.Development.json ? Database (Server, Name, User, Password)"
        );
    }

    /// <summary>
    /// Configura as URLs dos webhooks do n8n
    /// Prioridade: ENV ? appsettings.N8n
    /// </summary>
    public static IServiceCollection AddN8nConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Prioridade 1: Variáveis de ambiente
        var testUrl = Environment.GetEnvironmentVariable("WEBHOOK_TEST_URL");
        var prodUrl = Environment.GetEnvironmentVariable("WEBHOOK_PRODUCTION_URL");

        // Prioridade 2: appsettings.json ? N8n
        if (string.IsNullOrEmpty(testUrl) || string.IsNullOrEmpty(prodUrl))
        {
            var n8nConfig = configuration.GetSection("N8n");
            if (n8nConfig.Exists())
            {
                testUrl ??= n8nConfig["WEBHOOK_TEST_URL"];
                prodUrl ??= n8nConfig["WEBHOOK_PRODUCTION_URL"];
            }
        }

        // Validação
        if (string.IsNullOrEmpty(testUrl) || string.IsNullOrEmpty(prodUrl))
        {
            throw new InvalidOperationException(
                "? URLs do webhook n8n não configuradas. Configure uma das opções:\n" +
                "1. Variáveis de ambiente: WEBHOOK_TEST_URL, WEBHOOK_PRODUCTION_URL (.env.local ou GitHub Secrets)\n" +
                "2. appsettings.Development.json ? N8n (WEBHOOK_TEST_URL, WEBHOOK_PRODUCTION_URL)"
            );
        }

        // Adiciona as configurações no formato esperado pelo service
        configuration["N8n:WEBHOOK_TEST_URL"] = testUrl;
        configuration["N8n:WEBHOOK_PRODUCTION_URL"] = prodUrl;

        return services;
    }
}

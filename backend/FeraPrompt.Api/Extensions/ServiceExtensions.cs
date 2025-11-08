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
    /// Monta a connection string a partir das variáveis de ambiente
    /// </summary>
    private static string BuildConnectionString(IConfiguration configuration)
    {
        var server = configuration["DB_SERVER"];
        var database = configuration["DB_NAME"];
        var user = configuration["DB_USER"];
        var password = configuration["DB_PASSWORD"];

        if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(database) ||
            string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException(
                "Variáveis de ambiente de banco de dados não configuradas. " +
                "Verifique: DB_SERVER, DB_NAME, DB_USER, DB_PASSWORD");
        }

        return $"Server={server};Database={database};User Id={user};Password={password};" +
               "TrustServerCertificate=True;MultipleActiveResultSets=True;";
    }

    /// <summary>
    /// Configura as URLs dos webhooks do n8n
    /// </summary>
    public static IServiceCollection AddN8nConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var testUrl = configuration["WEBHOOK_TEST_URL"];
        var prodUrl = configuration["WEBHOOK_PRODUCTION_URL"];

        if (string.IsNullOrEmpty(testUrl) || string.IsNullOrEmpty(prodUrl))
        {
            throw new InvalidOperationException(
                "URLs do webhook n8n não configuradas. " +
                "Verifique: WEBHOOK_TEST_URL, WEBHOOK_PRODUCTION_URL");
        }

        // Adiciona as configurações no formato esperado pelo service
        configuration["N8n:WEBHOOK_TEST_URL"] = testUrl;
        configuration["N8n:WEBHOOK_PRODUCTION_URL"] = prodUrl;

        return services;
    }
}

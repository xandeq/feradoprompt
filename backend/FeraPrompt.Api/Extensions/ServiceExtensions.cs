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
        try
        {
            var connectionString = BuildConnectionString(configuration);
            
            if (!string.IsNullOrEmpty(connectionString))
            {
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(connectionString));

                // Registro dos Services
                services.AddScoped<IPromptService, PromptService>();
                
                Console.WriteLine("? Database connection configured successfully");
            }
            else
            {
                Console.WriteLine("?? WARNING: Database connection not configured. Running in limited mode.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? ERROR configuring database: {ex.Message}");
            Console.WriteLine("?? Application will start but database operations will fail.");
        }

        // Configuração do HttpClientFactory (sempre necessário)
        services.AddHttpClient();

        return services;
    }

    /// <summary>
    /// Monta a connection string a partir das variáveis de ambiente ou appsettings.json
    /// Prioridade: ENV ? appsettings.ConnectionStrings ? appsettings.Database
    /// </summary>
    private static string? BuildConnectionString(IConfiguration configuration)
    {
        // Prioridade 1: Variáveis de ambiente (GitHub Secrets em produção, .env.local em dev)
        var server = Environment.GetEnvironmentVariable("DB_SERVER");
        var database = Environment.GetEnvironmentVariable("DB_NAME");
        var user = Environment.GetEnvironmentVariable("DB_USER");
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD");

        if (!string.IsNullOrEmpty(server) && !string.IsNullOrEmpty(database) &&
            !string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(password))
        {
            Console.WriteLine($"? Using database from environment variables: {server}/{database}");
            return $"Server={server};Database={database};User Id={user};Password={password};" +
                   "TrustServerCertificate=True;MultipleActiveResultSets=True;Connect Timeout=30;";
        }

        // Prioridade 2: appsettings.json ? ConnectionStrings:Default
        var connString = configuration.GetConnectionString("Default");
        if (!string.IsNullOrEmpty(connString))
        {
            Console.WriteLine("? Using database from appsettings ConnectionStrings:Default");
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
                Console.WriteLine($"? Using database from appsettings Database section: {server}/{database}");
                return $"Server={server};Database={database};User Id={user};Password={password};" +
                       "TrustServerCertificate=True;MultipleActiveResultSets=True;Connect Timeout=30;";
            }
        }

        // Retorna null em vez de exception (permite app iniciar sem banco)
        Console.WriteLine("?? No database configuration found");
        return null;
    }

    /// <summary>
    /// Configura as URLs dos webhooks do n8n
    /// Prioridade: ENV ? appsettings.N8n
    /// </summary>
    public static IServiceCollection AddN8nConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        try
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
                Console.WriteLine("?? WARNING: n8n webhook URLs not configured");
                return services;
            }

            // Adiciona as configurações
            configuration["N8n:WEBHOOK_TEST_URL"] = testUrl;
            configuration["N8n:WEBHOOK_PRODUCTION_URL"] = prodUrl;
            
            Console.WriteLine("? n8n webhooks configured successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? ERROR configuring n8n: {ex.Message}");
        }

        return services;
    }
}

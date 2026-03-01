using FeraPrompt.Api.Data;
using FeraPrompt.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace FeraPrompt.Api.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        try
        {
            var connectionString = BuildConnectionString(configuration);

            if (!string.IsNullOrEmpty(connectionString))
            {
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(connectionString));

                services.AddScoped<IPromptService, PromptService>();
                Console.WriteLine("Database connection configured");
            }
            else
            {
                Console.WriteLine("Database connection missing. API will run in limited mode.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database setup error: {ex.Message}");
            Console.WriteLine("API started, but database operations may fail.");
        }

        // This service does not require DB and should always be available.
        services.AddScoped<IPromptGeneratorService, OpenRouterPromptGeneratorService>();
        services.AddHttpClient();

        return services;
    }

    private static string? BuildConnectionString(IConfiguration configuration)
    {
        var server = Environment.GetEnvironmentVariable("DB_SERVER");
        var database = Environment.GetEnvironmentVariable("DB_NAME");
        var user = Environment.GetEnvironmentVariable("DB_USER");
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD");

        if (!string.IsNullOrEmpty(server) &&
            !string.IsNullOrEmpty(database) &&
            !string.IsNullOrEmpty(user) &&
            !string.IsNullOrEmpty(password))
        {
            return $"Server={server};Database={database};User Id={user};Password={password};" +
                   "TrustServerCertificate=True;MultipleActiveResultSets=True;Connect Timeout=30;";
        }

        var connString = configuration.GetConnectionString("Default");
        if (!string.IsNullOrEmpty(connString))
        {
            return connString;
        }

        var dbConfig = configuration.GetSection("Database");
        if (dbConfig.Exists())
        {
            server = dbConfig["Server"];
            database = dbConfig["Name"];
            user = dbConfig["User"];
            password = dbConfig["Password"];

            if (!string.IsNullOrEmpty(server) &&
                !string.IsNullOrEmpty(database) &&
                !string.IsNullOrEmpty(user) &&
                !string.IsNullOrEmpty(password))
            {
                return $"Server={server};Database={database};User Id={user};Password={password};" +
                       "TrustServerCertificate=True;MultipleActiveResultSets=True;Connect Timeout=30;";
            }
        }

        return null;
    }

    public static IServiceCollection AddN8nConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        try
        {
            var testUrl = Environment.GetEnvironmentVariable("WEBHOOK_TEST_URL");
            var prodUrl = Environment.GetEnvironmentVariable("WEBHOOK_PRODUCTION_URL");

            if (string.IsNullOrEmpty(testUrl) || string.IsNullOrEmpty(prodUrl))
            {
                var n8nConfig = configuration.GetSection("N8n");
                if (n8nConfig.Exists())
                {
                    testUrl ??= n8nConfig["WEBHOOK_TEST_URL"];
                    prodUrl ??= n8nConfig["WEBHOOK_PRODUCTION_URL"];
                }
            }

            if (!string.IsNullOrEmpty(testUrl) && !string.IsNullOrEmpty(prodUrl))
            {
                configuration["N8n:WEBHOOK_TEST_URL"] = testUrl;
                configuration["N8n:WEBHOOK_PRODUCTION_URL"] = prodUrl;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"n8n setup error: {ex.Message}");
        }

        return services;
    }
}

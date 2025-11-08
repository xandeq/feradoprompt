using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FeraPrompt.Api.Data;

/// <summary>
/// Factory para criar o DbContext em tempo de design (migrations, scaffold, etc)
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Carregar .env.local se existir
        LoadEnvFile(".env.local");

        // Configuração
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Connection String
        var connectionString = GetConnectionString(configuration);

        // DbContextOptions
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }

    private static void LoadEnvFile(string path)
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

    private static string GetConnectionString(IConfiguration configuration)
    {
        // Prioridade 1: Variáveis de ambiente
        var dbServer = Environment.GetEnvironmentVariable("DB_SERVER");
        var dbName = Environment.GetEnvironmentVariable("DB_NAME");
        var dbUser = Environment.GetEnvironmentVariable("DB_USER");
        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

        if (!string.IsNullOrEmpty(dbServer) && !string.IsNullOrEmpty(dbName) &&
            !string.IsNullOrEmpty(dbUser) && !string.IsNullOrEmpty(dbPassword))
        {
            return $"Server={dbServer};Database={dbName};User Id={dbUser};Password={dbPassword};TrustServerCertificate=True;";
        }

        // Prioridade 2: appsettings.json
        var connString = configuration.GetConnectionString("Default");
        if (!string.IsNullOrEmpty(connString))
        {
            return connString;
        }

        // Prioridade 3: Construir da seção Database
        var dbConfig = configuration.GetSection("Database");
        if (dbConfig.Exists())
        {
            var server = dbConfig["Server"];
            var name = dbConfig["Name"];
            var user = dbConfig["User"];
            var password = dbConfig["Password"];

            if (!string.IsNullOrEmpty(server) && !string.IsNullOrEmpty(name) &&
                !string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(password))
            {
                return $"Server={server};Database={name};User Id={user};Password={password};TrustServerCertificate=True;";
            }
        }

        throw new InvalidOperationException(
            "? Connection string não configurada. Configure uma das opções:\n" +
            "1. Variáveis de ambiente: DB_SERVER, DB_NAME, DB_USER, DB_PASSWORD\n" +
            "2. appsettings.Development.json -> ConnectionStrings:Default\n" +
            "3. appsettings.Development.json -> Database (Server, Name, User, Password)"
        );
    }
}

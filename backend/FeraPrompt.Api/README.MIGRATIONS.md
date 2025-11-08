# ??? Entity Framework Core - Guia de Migrations

## ?? Estrutura Criada

### Entidades (Models)
- ? **Prompt** - Templates de prompts com título, corpo e modelo de IA
- ? **PromptHistory** - Histórico de execuções de prompts
- ? **User** - Usuários do sistema

### Relacionamentos
- `Prompt` 1:N `PromptHistory` (um prompt pode ter vários históricos)
- Cascade delete habilitado (deletar prompt deleta seus históricos)

### Índices Criados
- `Prompt.CreatedAt`, `Prompt.Model` - Performance em queries
- `PromptHistory.PromptId`, `PromptHistory.ExecutedAt` - Performance em joins
- `User.Username` (UNIQUE), `User.Email` (UNIQUE) - Integridade

## ?? Comandos de Migration

### 1. Criar a Migration Inicial
```bash
cd C:\Users\acq20\Desktop\Projetos\feradoprompt\backend\FeraPrompt.Api
dotnet ef migrations add InitialCreate
```

### 2. Aplicar Migration ao Banco
```bash
dotnet ef database update
```

### 3. Remover Última Migration (se necessário)
```bash
dotnet ef migrations remove
```

### 4. Ver Status das Migrations
```bash
dotnet ef migrations list
```

### 5. Gerar Script SQL (sem aplicar)
```bash
dotnet ef migrations script > migration.sql
```

## ?? Troubleshooting

### Erro: "Unable to create an object of type 'ApplicationDbContext'"
**Solução:** Certifique-se que as variáveis de ambiente estão configuradas ou que `appsettings.Development.json` tem a connection string.

### Erro: "Login failed for user"
**Solução:** Verifique se as credenciais no `.env.local` ou GitHub Secrets estão corretas.

### Erro: "Cannot insert duplicate key"
**Solução:** Já existe um registro com Username ou Email duplicado.

## ?? Tabelas Criadas

Após executar `dotnet ef database update`, serão criadas:

```
db_aaf0a8_feradoprompt
??? Prompts
?   ??? Id (PK, INT, IDENTITY)
?   ??? Title (NVARCHAR(200), NOT NULL)
?   ??? Body (NVARCHAR(MAX), NOT NULL)
?   ??? Model (NVARCHAR(50), NOT NULL)
?   ??? CreatedAt (DATETIME2, DEFAULT GETUTCDATE())
?   ??? CreatedBy (NVARCHAR(100), NULL)
?
??? PromptHistories
?   ??? Id (PK, INT, IDENTITY)
?   ??? PromptId (FK ? Prompts.Id, NOT NULL)
?   ??? Input (NVARCHAR(MAX), NOT NULL)
?   ??? Output (NVARCHAR(MAX), NOT NULL)
?   ??? ModelUsed (NVARCHAR(50), NOT NULL)
?   ??? ExecutedAt (DATETIME2, DEFAULT GETUTCDATE())
?
??? Users
?   ??? Id (PK, INT, IDENTITY)
?   ??? Username (NVARCHAR(100), NOT NULL, UNIQUE)
?   ??? Email (NVARCHAR(255), NOT NULL, UNIQUE)
?   ??? CreatedAt (DATETIME2, DEFAULT GETUTCDATE())
?
??? __EFMigrationsHistory (tabela de controle do EF)
```

## ?? Próximos Passos

1. Executar a migration inicial
2. Criar Repositories para cada entidade
3. Criar Services com regras de negócio
4. Criar Controllers (endpoints REST)
5. Adicionar autenticação JWT
6. Implementar validações e DTOs

## ?? Exemplo de Uso (Repository Pattern)

```csharp
// Services/PromptService.cs
public class PromptService
{
    private readonly ApplicationDbContext _context;

    public async Task<Prompt> CreatePromptAsync(Prompt prompt)
    {
        _context.Prompts.Add(prompt);
        await _context.SaveChangesAsync();
        return prompt;
    }

    public async Task<List<Prompt>> GetAllPromptsAsync()
    {
        return await _context.Prompts
            .Include(p => p.PromptHistories)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }
}
```

## ?? Importante

- ? **Sempre use async/await** - Nunca `.Result` ou `.Wait()`
- ? **Use Include/ThenInclude** - Evite N+1 queries
- ? **Transactions quando necessário** - `using var transaction = await _context.Database.BeginTransactionAsync()`
- ? **Validações antes de salvar** - Use FluentValidation ou Data Annotations
- ? **DTOs para API** - Nunca exponha entidades diretamente nos endpoints

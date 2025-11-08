# ??? Entity Framework Core - Guia de Migrations

## ? **MIGRATIONS APLICADAS COM SUCESSO!**

### ?? Migration Inicial
- **Nome:** `InitialCreate`
- **Data/Hora:** `20251108123813` (08/11/2025 09:38)
- **Status:** ? Aplicada ao banco de dados
- **Tabelas Criadas:** `Prompts`, `PromptHistories`, `Users`, `__EFMigrationsHistory`

---

## ?? **Solução Implementada para Design-Time**

### Problema Identificado
```
? Unable to create a 'DbContext' of type 'RuntimeType'
? Variáveis de ambiente de banco de dados não configuradas
```

### Solução Aplicada
Criado `ApplicationDbContextFactory.cs` que implementa `IDesignTimeDbContextFactory<ApplicationDbContext>`.

Este factory permite que o EF Tools crie o DbContext em tempo de design (para migrations) com as seguintes prioridades:

1. **Variáveis de ambiente** via `.env.local`
2. **appsettings.Development.json** ? `ConnectionStrings:Default`
3. **appsettings.Development.json** ? `Database` (Server, Name, User, Password)

---

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

---

## ?? Comandos de Migration

### 1. Criar uma Nova Migration
```bash
cd C:\Users\acq20\Desktop\Projetos\feradoprompt\backend\FeraPrompt.Api
dotnet ef migrations add NomeDaMigration
```

### 2. Aplicar Migrations ao Banco
```bash
dotnet ef database update
```

### 3. Aplicar Migration Específica
```bash
dotnet ef database update NomeDaMigration
```

### 4. Remover Última Migration (NÃO aplicada)
```bash
dotnet ef migrations remove
```

### 5. Reverter para Migration Anterior
```bash
dotnet ef database update NomeDaMigrationAnterior
```

### 6. Ver Status das Migrations
```bash
dotnet ef migrations list
```

### 7. Gerar Script SQL (sem aplicar)
```bash
dotnet ef migrations script > migration.sql
```

### 8. Gerar Script de Migration Específica
```bash
dotnet ef migrations script FromMigration ToMigration
```

---

## ?? Troubleshooting

### ? Erro RESOLVIDO: "Unable to create DbContext"
**Problema:** EF Tools não conseguia criar DbContext em tempo de design.

**Solução:** Criado `ApplicationDbContextFactory.cs` com `IDesignTimeDbContextFactory`.

**Como funciona:**
1. Carrega `.env.local` se existir
2. Carrega `appsettings.Development.json`
3. Busca connection string em ordem de prioridade
4. Cria DbContext com connection string válida

### Erro: "Login failed for user"
**Solução:** Verifique se as credenciais no `.env.local` ou `appsettings.Development.json` estão corretas.

### Erro: "Cannot insert duplicate key"
**Solução:** Já existe um registro com Username ou Email duplicado.

### Erro: "The Entity Framework tools version is older"
**Nota:** Aviso informativo. Considere atualizar:
```bash
dotnet tool update --global dotnet-ef
```

---

## ?? Tabelas Criadas no Banco

Após executar `dotnet ef database update`:

```
db_aaf0a8_feradoprompt (SQL Server)
?
??? ?? Tables
?   ??? Prompts
?   ?   ??? Id (PK, INT, IDENTITY)
?   ?   ??? Title (NVARCHAR(200), NOT NULL)
?   ?   ??? Body (NVARCHAR(MAX), NOT NULL)
?   ?   ??? Model (NVARCHAR(50), NOT NULL)
?   ?   ??? CreatedAt (DATETIME2, DEFAULT GETUTCDATE())
?   ?   ??? CreatedBy (NVARCHAR(100), NULL)
?   ?
?   ??? PromptHistories
?   ?   ??? Id (PK, INT, IDENTITY)
?   ?   ??? PromptId (FK ? Prompts.Id, NOT NULL)
?   ?   ??? Input (NVARCHAR(MAX), NOT NULL)
?   ?   ??? Output (NVARCHAR(MAX), NOT NULL)
?   ?   ??? ModelUsed (NVARCHAR(50), NOT NULL)
?   ?   ??? ExecutedAt (DATETIME2, DEFAULT GETUTCDATE())
?   ?
?   ??? Users
?   ?   ??? Id (PK, INT, IDENTITY)
?   ?   ??? Username (NVARCHAR(100), NOT NULL, UNIQUE)
?   ?   ??? Email (NVARCHAR(255), NOT NULL, UNIQUE)
?   ?   ??? CreatedAt (DATETIME2, DEFAULT GETUTCDATE())
?   ?
?   ??? __EFMigrationsHistory (controle do EF)
?       ??? MigrationId (PK, NVARCHAR(150))
?       ??? ProductVersion (NVARCHAR(32))
?
??? ?? Indexes
    ??? IX_Prompts_CreatedAt
    ??? IX_Prompts_Model
    ??? IX_PromptHistories_PromptId
    ??? IX_PromptHistories_ExecutedAt
    ??? IX_Users_Username (UNIQUE)
    ??? IX_Users_Email (UNIQUE)
```

---

## ?? Verificar Migrations Aplicadas

### No Banco de Dados (SQL Server)
```sql
SELECT * FROM __EFMigrationsHistory
ORDER BY MigrationId DESC;
```

**Resultado esperado:**
| MigrationId | ProductVersion |
|-------------|----------------|
| 20251108123813_InitialCreate | 9.0.10 |

### Via CLI
```bash
dotnet ef migrations list
```

---

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

---

## ?? Importante

- ? **Sempre use async/await** - Nunca `.Result` ou `.Wait()`
- ? **Use Include/ThenInclude** - Evite N+1 queries
- ? **Transactions quando necessário** - `using var transaction = await _context.Database.BeginTransactionAsync()`
- ? **Validações antes de salvar** - Use FluentValidation ou Data Annotations
- ? **DTOs para API** - Nunca exponha entidades diretamente nos endpoints
- ? **Backup antes de migrations em produção**

---

## ?? Status Atual

```
? ApplicationDbContext criado
? Entidades (Prompt, PromptHistory, User) criadas
? Relacionamentos configurados
? Índices criados
? ApplicationDbContextFactory implementado
? Migration InitialCreate criada (20251108123813)
? Migration aplicada ao banco de dados
? Tabelas criadas com sucesso
? Build bem-sucedido
? Pronto para desenvolvimento!
```

---

## ?? Próximas Etapas

1. ? ~~Criar Migration Inicial~~ - **CONCLUÍDO**
2. ? ~~Aplicar ao Banco de Dados~~ - **CONCLUÍDO**
3. ?? Testar endpoints da API
4. ?? Criar seeds de dados (opcional)
5. ?? Implementar autenticação JWT
6. ?? Adicionar validações e DTOs
7. ?? Configurar CI/CD para migrations automáticas

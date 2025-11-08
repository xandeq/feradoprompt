# Configuração do Banco de Dados - Fera do Prompt API

## ?? Visão Geral

A API está configurada para usar **SQL Server** via Entity Framework Core com suporte a variáveis de ambiente para ambientes de produção (via GitHub Secrets).

## ?? Configuração

### Variáveis de Ambiente Necessárias

A aplicação suporta duas formas de configuração:

#### 1. **Produção (via GitHub Secrets)**
As seguintes variáveis devem estar configuradas:

```
DB_SERVER=sql1003.site4now.net
DB_NAME=db_aaf0a8_feradoprompt
DB_USER=db_aaf0a8_feradoprompt_admin
DB_PASSWORD=7Wh1v3EEtMQH
FRONTEND_BASE_URL=https://seu-frontend.com
```

#### 2. **Desenvolvimento Local**
Crie um arquivo `.env.local` na raiz do projeto (copie de `.env.local.example`):

```bash
cp .env.local.example .env.local
```

Edite conforme necessário. O arquivo `.env.local` é automaticamente ignorado pelo Git.

### Ordem de Prioridade

1. **Variáveis de Ambiente** (DB_SERVER, DB_NAME, DB_USER, DB_PASSWORD)
2. **appsettings.Development.json** ? ConnectionStrings:Default
3. **Erro** se nenhuma configuração for encontrada

## ?? Como Usar

### Desenvolvimento Local

1. Configure o `.env.local` ou use as configurações do `appsettings.Development.json`
2. Execute a aplicação:
   ```bash
   dotnet run
   ```

### Migrations

Para criar uma migration:
```bash
dotnet ef migrations add NomeDaMigration
```

Para aplicar migrations:
```bash
dotnet ef database update
```

## ?? Segurança

- ? **Nunca** commite arquivos `.env.local`
- ? Secrets são configurados via GitHub Secrets em produção
- ? Logs **não** expõem connection strings ou passwords
- ? `TrustServerCertificate=True` está habilitado (necessário para SmarterASP)

## ?? Funcionalidades Configuradas

- ? **Entity Framework Core** com SQL Server
- ? **CORS** configurado para o frontend
- ? **Rate Limiting** por IP (100 requisições/minuto)
- ? **Swagger** habilitado em desenvolvimento
- ? **Async/Await** em todo o código (sem `.Result` ou `.Wait()`)

## ??? Estrutura

```
FeraPrompt.Api/
??? Data/
?   ??? ApplicationDbContext.cs    # DbContext do EF Core
??? Program.cs                      # Configuração principal
??? appsettings.json               # Configurações base
??? appsettings.Development.json   # Configurações de desenvolvimento
??? .env.local.example             # Template de variáveis de ambiente
??? .env.local                     # Suas variáveis locais (não commitado)
```

## ?? Próximos Passos

1. Criar entidades no namespace `FeraPrompt.Api.Models`
2. Adicionar `DbSet<T>` no `ApplicationDbContext`
3. Criar migrations com `dotnet ef migrations add InitialCreate`
4. Aplicar migrations com `dotnet ef database update`
5. Criar Controllers, Services e Repositories seguindo o padrão do projeto

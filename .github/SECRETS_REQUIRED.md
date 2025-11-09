# 🔐 GitHub Secrets - Configuração Necessária

Este documento lista TODOS os secrets necessários para os workflows de CI/CD funcionarem corretamente.

## ✅ Secrets Existentes (Configurados)

- ✅ `DB_SERVER` - Servidor do banco de dados SQL
- ✅ `DB_NAME` - Nome do banco de dados
- ✅ `DB_USER` - Usuário do banco de dados
- ✅ `DB_PASSWORD` - Senha do banco de dados
- ✅ `SMARTERASP_FTP_HOST` - Host FTP da SmarterASP
- ✅ `SMARTERASP_FTP_USER` - Usuário FTP
- ✅ `SMARTERASP_FTP_PASS` - Senha FTP
- ✅ `SMARTERASP_FTP_DIR` - Diretório raiz no FTP (ex: `/www`)
- ✅ `WEBHOOK_PRODUCTION_URL` - URL do webhook n8n produção
- ✅ `WEBHOOK_TEST_URL` - URL do webhook n8n teste

## ❌ Secrets FALTANDO (Adicionar Agora)

### 🚨 CRÍTICO - Frontend

```
BACKEND_API_BASE_URL_PROD
```

**Descrição**: URL base da API backend em produção  
**Exemplo**: `https://api.feradoprompt.com` ou `https://feradoprompt.com/api`  
**Usado em**: `frontend-ci.yml` → variável de ambiente `NEXT_PUBLIC_API_BASE_URL`

---

## 📋 Como Adicionar o Secret Faltante

1. Acesse: https://github.com/xandeq/feradoprompt/settings/secrets/actions
2. Clique em **"New repository secret"**
3. Adicione:

### Secret: `BACKEND_API_BASE_URL_PROD`

**Name**: `BACKEND_API_BASE_URL_PROD`  
**Value**: URL da sua API em produção

**Opções**:

**Opção A - Subdomínio** (Recomendado)
```
https://api.feradoprompt.com
```

**Opção B - Subpath**
```
https://feradoprompt.com/api
```

**Opção C - SmarterASP direto** (se não tiver domínio custom)
```
https://seusite.somee.com/api
```

---

## 🔧 Estrutura de Diretórios no FTP

Baseado no secret `SMARTERASP_FTP_DIR`, a estrutura será:

```
{SMARTERASP_FTP_DIR}/
├── frontend/          # Arquivos do Next.js (static export)
│   ├── index.html
│   ├── _next/
│   └── ...
└── backend/           # Arquivos do .NET 8 API
    ├── FeraPrompt.Api.dll
    ├── appsettings.json
    ├── web.config
    └── ...
```

**Exemplo se `SMARTERASP_FTP_DIR=/www`**:
- Frontend: `/www/frontend/`
- Backend: `/www/backend/`

---

## 📊 Checklist de Validação

Antes de fazer push, verifique:

- [ ] ✅ 10 secrets configurados (todos da lista "Existentes")
- [ ] ✅ `BACKEND_API_BASE_URL_PROD` adicionado
- [ ] ✅ `SMARTERASP_FTP_DIR` termina SEM barra (ex: `/www` não `/www/`)
- [ ] ✅ Domínio/subdomínio apontando para SmarterASP
- [ ] ✅ SSL configurado (HTTPS) se usar domínio custom

---

## 🚀 Após Configurar

1. Faça commit das alterações
2. Push para `main`
3. Monitore as Actions em: https://github.com/xandeq/feradoprompt/actions
4. Verifique os logs de build/deploy

---

## 🆘 Troubleshooting

### Erro: "Secret not found"
- Verifique o nome EXATO do secret (case-sensitive)
- Aguarde 30s após criar o secret antes de rodar o workflow

### Deploy FTP falha
- Verifique se `SMARTERASP_FTP_DIR` está correto
- Confirme que o diretório existe no servidor FTP
- Teste credenciais FTP com cliente (FileZilla, WinSCP)

### Build Next.js falha
- Verifique se `BACKEND_API_BASE_URL_PROD` está configurado
- Confirme que não há erros de TypeScript no código
- Veja logs detalhados na aba Actions

---

**Última atualização**: 2025-11-09

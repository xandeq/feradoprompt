# Contexto do Monorepo
- frontend: Next.js 16 (TypeScript). Usar fetch e SWR, evitar libs pesadas.
- backend: .NET 8 Web API com EF Core, CORS, RateLimiter. Sem estado no servidor.
- mobileapp: Expo/React Native. Ler API base via app.config.ts (extra.apiBaseUrl).

# Padrões de Código
- Backend: Controllers finos, Services com regras, Repositories com EF. Tudo async/await. Nada de .Result/.Wait().
- Frontend: Components funcionais, hooks, CSS modular. Evitar re-renders (memo/useMemo/useCallback).
- Mobile: Components leves, requisições no layer services.

# Segurança/Secrets
- Jamais imprimir segredos em logs.
- Conexão SQL via ENV: DB_SERVER, DB_NAME, DB_USER, DB_PASSWORD.
- Next: usar apenas NEXT_PUBLIC_* para variáveis públicas.
- Nunca comitar .env.*.local.

# Qualidade
- Gerar testes unitários para Services/Repos.
- Evitar N+1 no EF (Include/ThenInclude).
- Rate limiting por IP habilitado.

# Infra
- CI separada por pasta. Deploy via FTP Actions usando secrets.
- API base configurável: dev, staging, prod.

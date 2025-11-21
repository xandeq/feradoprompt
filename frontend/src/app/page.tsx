"use client";

import { apiRequest } from "@/lib/apiClient";
import { useState } from "react";

type Prompt = {
  id: number;
  title: string;
  body: string;
  model: string;
  createdAt?: string;
  updatedAt?: string;
};

type CreatePromptPayload = {
  title: string;
  body: string;
  model: string;
};

type ExecutePromptPayload = {
  promptId: number;
  input: string;
};

const createPromptPayload: CreatePromptPayload = {
  title: "Teste via Frontend",
  body: "Corpo do prompt de teste",
  model: "gpt-4",
};

const executePromptPayload: ExecutePromptPayload = {
  promptId: 1,
  input: "me dê um roteiro de viagem",
};

async function fetchPromptsExample() {
  return apiRequest<Prompt[]>("GET", "/api/Prompts");
}

async function createPromptExample() {
  return apiRequest<Prompt>("POST", "/api/Prompts", createPromptPayload);
}

async function executePromptExample() {
  return apiRequest<unknown>(
    "POST",
    "/api/Prompts/execute",
    executePromptPayload,
  );
}

export default function Home() {
  const [isLoading, setIsLoading] = useState(false);
  const [result, setResult] = useState<string>("");
  const [error, setError] = useState<string>("");

  const runExample = async (action: () => Promise<unknown>) => {
    setIsLoading(true);
    setResult("");
    setError("");

    try {
      const response = await action();
      setResult(JSON.stringify(response, null, 2));
    } catch (err) {
      if (err instanceof Error) {
        setError(err.message);
      } else {
        setError("Erro desconhecido ao executar a requisição.");
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-zinc-50 font-sans text-zinc-950 dark:bg-black dark:text-zinc-50">
      <main className="mx-auto flex min-h-screen w-full max-w-4xl flex-col gap-10 px-6 py-16">
        <header className="space-y-4">
          <h1 className="text-3xl font-semibold tracking-tight">
            Testes rápidos com a API feradoprompt
          </h1>
          <p className="text-lg text-zinc-600 dark:text-zinc-400">
            Utilize a função utilitária <code>apiRequest</code> para validar os
            endpoints públicos da API feradoprompt diretamente do frontend.
          </p>
        </header>

        <section className="space-y-6 rounded-2xl border border-zinc-200 bg-white p-6 shadow-sm dark:border-zinc-800 dark:bg-zinc-900">
          <h2 className="text-2xl font-medium tracking-tight">
            Função utilitária
          </h2>
          <p className="text-sm text-zinc-600 dark:text-zinc-400">
            A função <code>apiRequest</code> usa{" "}
            <code>fetch</code> com o cabeçalho{" "}
            <code>Content-Type: application/json</code> e monta a URL final a
            partir da variável de ambiente{" "}
            <code>NEXT_PUBLIC_API_BASE_URL</code> (com fallback para{" "}
            <code>https://api.feradoprompt.com.br</code>).
          </p>
          <pre className="overflow-x-auto rounded-xl bg-zinc-950 p-4 text-sm text-zinc-100">
            <code>{`import { apiRequest } from "@/lib/apiClient";

async function fetchPromptsExample() {
  return apiRequest<Prompt[]>("GET", "/api/Prompts");
}

async function createPromptExample() {
  return apiRequest<Prompt>("POST", "/api/Prompts", createPromptPayload);
}

async function executePromptExample() {
  return apiRequest("POST", "/api/Prompts/execute", executePromptPayload);
}`}</code>
          </pre>
        </section>

        <section className="space-y-6 rounded-2xl border border-zinc-200 bg-white p-6 shadow-sm dark:border-zinc-800 dark:bg-zinc-900">
          <h2 className="text-2xl font-medium tracking-tight">
            Exemplos práticos
          </h2>
          <div className="grid gap-4 md:grid-cols-3">
            <button
              type="button"
              onClick={() => runExample(fetchPromptsExample)}
              className="rounded-xl bg-zinc-900 px-4 py-3 text-sm font-semibold text-white transition hover:bg-zinc-700 disabled:pointer-events-none disabled:opacity-70 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-zinc-300"
              disabled={isLoading}
            >
              GET /api/Prompts
            </button>
            <button
              type="button"
              onClick={() => runExample(createPromptExample)}
              className="rounded-xl bg-zinc-900 px-4 py-3 text-sm font-semibold text-white transition hover:bg-zinc-700 disabled:pointer-events-none disabled:opacity-70 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-zinc-300"
              disabled={isLoading}
            >
              POST /api/Prompts
            </button>
            <button
              type="button"
              onClick={() => runExample(executePromptExample)}
              className="rounded-xl bg-zinc-900 px-4 py-3 text-sm font-semibold text-white transition hover:bg-zinc-700 disabled:pointer-events-none disabled:opacity-70 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-zinc-300"
              disabled={isLoading}
            >
              POST /api/Prompts/execute
            </button>
          </div>

          <div className="space-y-4 text-sm">
            <div>
              <h3 className="mb-2 font-semibold uppercase tracking-wide text-zinc-500">
                Body - POST /api/Prompts
              </h3>
              <pre className="overflow-x-auto rounded-xl bg-zinc-100 p-4 text-zinc-900 dark:bg-zinc-950 dark:text-zinc-100">
                <code>{JSON.stringify(createPromptPayload, null, 2)}</code>
              </pre>
            </div>
            <div>
              <h3 className="mb-2 font-semibold uppercase tracking-wide text-zinc-500">
                Body - POST /api/Prompts/execute
              </h3>
              <pre className="overflow-x-auto rounded-xl bg-zinc-100 p-4 text-zinc-900 dark:bg-zinc-950 dark:text-zinc-100">
                <code>{JSON.stringify(executePromptPayload, null, 2)}</code>
              </pre>
            </div>
          </div>
        </section>

        <section className="space-y-4 rounded-2xl border border-zinc-200 bg-white p-6 shadow-sm dark:border-zinc-800 dark:bg-zinc-900">
          <h2 className="text-2xl font-medium tracking-tight">
            Resultado da requisição
          </h2>
          {isLoading && (
            <p className="text-sm text-zinc-500">Executando requisição...</p>
          )}
          {error && (
            <pre className="overflow-x-auto rounded-xl border border-red-300 bg-red-50 p-4 text-sm text-red-700 dark:border-red-700 dark:bg-red-950 dark:text-red-200">
              <code>{error}</code>
            </pre>
          )}
          {result && (
            <pre className="overflow-x-auto rounded-xl bg-zinc-100 p-4 text-sm text-zinc-900 dark:bg-zinc-950 dark:text-zinc-100">
              <code>{result}</code>
            </pre>
          )}
          {!isLoading && !error && !result && (
            <p className="text-sm text-zinc-500">
              Selecione uma das ações acima para visualizar a resposta.
            </p>
          )}
        </section>
      </main>
    </div>
  );
}

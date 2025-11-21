"use client";

import { apiRequest } from "@/lib/apiClient";
import { Sparkles } from "lucide-react";
import { useState } from "react";
import ReactMarkdown from "react-markdown";

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
  model: string;
};

type PromptResponse = {
  promptId: number;
  input: string;
  output: string;
  modelUsed: string;
  executedAt: string;
};

const createPromptPayload: CreatePromptPayload = {
  title: "Teste via Frontend",
  body: "Corpo do prompt de teste",
  model: "gpt-4",
};

export default function Home() {
  const [isLoading, setIsLoading] = useState(false);
  const [result, setResult] = useState<string>("");
  const [error, setError] = useState<string>("");
  const [isHtmlOutput, setIsHtmlOutput] = useState(false);
  const [input, setInput] = useState("me dê um roteiro de viagem");

  const handleGeneratePrompt = async () => {
    setIsLoading(true);
    setResult("");
    setError("");
    setIsHtmlOutput(false);

    try {
      const payload: ExecutePromptPayload = {
        promptId: 1,
        input: input,
        model: "gpt-4",
      };

      const response = await apiRequest<PromptResponse>(
        "POST",
        "/api/Prompts/execute",
        payload,
      );

      if (typeof response === 'object' && response !== null && 'output' in response) {
         setResult(response.output);
         setIsHtmlOutput(true);
      } else {
         setResult(JSON.stringify(response, null, 2));
         setIsHtmlOutput(false);
      }

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

  const handleCopy = async () => {
    if (result) {
      try {
        await navigator.clipboard.writeText(result);
        alert("Conteúdo copiado para a área de transferência!");
      } catch (err) {
        console.error("Falha ao copiar: ", err);
      }
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
            Utilize a área abaixo para testar a geração de prompts dinamicamente.
          </p>
        </header>

        <section className="space-y-6 rounded-2xl border border-zinc-200 bg-white p-6 shadow-sm dark:border-zinc-800 dark:bg-zinc-900">
          <h2 className="text-2xl font-medium tracking-tight">
            Gerar Prompt
          </h2>

          <div className="space-y-4">
            <div className="space-y-2">
              <label htmlFor="input" className="text-sm font-medium text-zinc-700 dark:text-zinc-300">
                Input do usuário
              </label>
              <textarea
                id="input"
                value={input}
                onChange={(e) => setInput(e.target.value)}
                className="w-full min-h-[100px] rounded-xl border border-zinc-300 bg-transparent px-4 py-3 text-sm outline-none focus:border-zinc-500 focus:ring-1 focus:ring-zinc-500 dark:border-zinc-700 dark:bg-zinc-950 dark:text-white"
                placeholder="Digite aqui o seu pedido..."
              />
            </div>

            <button
              onClick={handleGeneratePrompt}
              disabled={isLoading || !input.trim()}
              className="flex w-full items-center justify-center gap-2 rounded-xl bg-zinc-900 px-4 py-3 text-sm font-semibold text-white transition hover:bg-zinc-700 disabled:pointer-events-none disabled:opacity-70 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-zinc-300 sm:w-auto"
            >
              <Sparkles className="h-4 w-4" />
              {isLoading ? "Gerando..." : "Gerar Prompt"}
            </button>
          </div>
        </section>

        <section className="space-y-4 rounded-2xl border border-zinc-200 bg-white p-6 shadow-sm dark:border-zinc-800 dark:bg-zinc-900">
          <div className="flex items-center justify-between">
            <h2 className="text-2xl font-medium tracking-tight">
              Resultado da requisição
            </h2>
            {result && (
              <button
                onClick={handleCopy}
                className="rounded-md bg-zinc-200 px-3 py-1.5 text-xs font-medium text-zinc-700 hover:bg-zinc-300 dark:bg-zinc-800 dark:text-zinc-300 dark:hover:bg-zinc-700"
              >
                Copiar código
              </button>
            )}
          </div>

          {isLoading && (
            <div className="flex flex-col items-center justify-center py-10 space-y-4 text-zinc-500">
              <Sparkles className="h-8 w-8 animate-pulse text-zinc-400" />
              <p className="text-sm">Processando seu pedido...</p>
            </div>
          )}

          {error && (
            <pre className="overflow-x-auto rounded-xl border border-red-300 bg-red-50 p-4 text-sm text-red-700 dark:border-red-700 dark:bg-red-950 dark:text-red-200">
              <code>{error}</code>
            </pre>
          )}

          {result && !isLoading && (
            <div className="overflow-x-auto rounded-xl bg-zinc-100 p-4 text-sm text-zinc-900 dark:bg-zinc-950 dark:text-zinc-100">
              {isHtmlOutput ? (
                <article className="prose prose-sm dark:prose-invert max-w-none">
                  <ReactMarkdown>{result}</ReactMarkdown>
                </article>
              ) : (
                <pre><code>{result}</code></pre>
              )}
            </div>
          )}

          {!isLoading && !error && !result && (
            <p className="text-sm text-zinc-500">
              O resultado gerado aparecerá aqui.
            </p>
          )}
        </section>
      </main>
    </div>
  );
}
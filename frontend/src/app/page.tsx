"use client";

import { apiRequest } from "@/lib/apiClient";
import { Sparkles } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import ReactMarkdown from "react-markdown";

type PurposeOption = {
  value: string;
  label: string;
  description: string;
};

type GenerateRequest = {
  purpose: string;
  model: string;
  brief: string;
  extraContext?: string;
  language?: string;
  style?: string;
  duration?: string;
  aspectRatio?: string;
  apiKey?: string;
};

type GenerateResponse = {
  provider: string;
  purpose: string;
  model: string;
  generatedPrompt: string;
};

type FreeModelCatalogItem = {
  category: string;
  modelId: string;
  provider: string;
  maxContext: string;
  capabilities: string;
  limitations: string;
};

const PURPOSES: PurposeOption[] = [
  { value: "nano_banana_image", label: "Gemini Nano Banana (Imagem)", description: "Prompt de imagem focado no Nano Banana." },
  { value: "chatgpt_images", label: "ChatGPT Images (Imagem)", description: "Prompt para geração de imagem no ChatGPT Images." },
  { value: "veo3_video_script", label: "Google Veo 3 (Roteiro)", description: "Roteiro completo de vídeo com cenas e direção." },
  { value: "veo3_video_json", label: "Google Veo 3 (JSON)", description: "Saída em JSON técnico para pipeline de vídeo." },
  { value: "grok_imagine_image", label: "Grok Imagine (Imagem)", description: "Prompt para imagem estática no Grok Imagine." },
  { value: "grok_imagine_animate", label: "Grok Imagine (Animar Imagem)", description: "Prompt para transformar imagem em vídeo/ani­mação." }
];

const API_KEY_STORAGE = "feradoprompt_openrouter_api_key";

function normalizeApiKey(raw: string): string {
  let value = raw.trim().replace(/^["']|["']$/g, "");
  if (value.toLowerCase().startsWith("bearer ")) {
    value = value.slice(7).trim();
  }
  return value.replace(/\s+/g, "");
}

export default function Home() {
  const [apiKey, setApiKey] = useState("");
  const [purpose, setPurpose] = useState(PURPOSES[0].value);
  const [model, setModel] = useState("");
  const [models, setModels] = useState<string[]>([]);
  const [catalog, setCatalog] = useState<FreeModelCatalogItem[]>([]);
  const [brief, setBrief] = useState("");
  const [extraContext, setExtraContext] = useState("");
  const [language, setLanguage] = useState("pt-BR");
  const [style, setStyle] = useState("");
  const [duration, setDuration] = useState("8s");
  const [aspectRatio, setAspectRatio] = useState("16:9");
  const [loadingModels, setLoadingModels] = useState(false);
  const [isGenerating, setIsGenerating] = useState(false);
  const [result, setResult] = useState("");
  const [error, setError] = useState("");

  const selectedPurpose = useMemo(
    () => PURPOSES.find((item) => item.value === purpose),
    [purpose]
  );

  useEffect(() => {
    const saved = localStorage.getItem(API_KEY_STORAGE);
    if (saved) {
      setApiKey(saved);
    }
  }, []);

  useEffect(() => {
    localStorage.setItem(API_KEY_STORAGE, apiKey);
  }, [apiKey]);

  useEffect(() => {
    const loadCatalog = async () => {
      try {
        const response = await apiRequest<FreeModelCatalogItem[]>(
          "GET",
          "/api/PromptGenerator/models/catalog"
        );
        setCatalog(response);
        const ids = response.map((item) => item.modelId);
        setModels(ids);
        if (ids.length > 0) {
          setModel((current) => (current ? current : ids[0]));
        }
      } catch {
        // Keep page usable even if catalog endpoint is temporarily unavailable.
      }
    };

    loadCatalog();
  }, []);

  const handleLoadFreeModels = async () => {
    setLoadingModels(true);
    setError("");
    try {
      const response = await apiRequest<string[]>(
        "POST",
        "/api/PromptGenerator/models/free",
        { apiKey: apiKey || undefined }
      );
      setModels(response);
      if (response.length > 0) {
        setModel(response[0]);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao carregar modelos.");
    } finally {
      setLoadingModels(false);
    }
  };

  const handleGenerate = async () => {
    if (!brief.trim()) {
      setError("Preencha o briefing.");
      return;
    }

    if (!model.trim()) {
      setError("Escolha um modelo.");
      return;
    }

    setIsGenerating(true);
    setError("");
    setResult("");

    try {
      const payload: GenerateRequest = {
        purpose,
        model,
        brief,
        extraContext: extraContext || undefined,
        language: language || undefined,
        style: style || undefined,
        duration: duration || undefined,
        aspectRatio: aspectRatio || undefined,
        apiKey: apiKey ? normalizeApiKey(apiKey) : undefined
      };

      const response = await apiRequest<GenerateResponse>(
        "POST",
        "/api/PromptGenerator/generate",
        payload
      );

      setResult(response.generatedPrompt);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao gerar prompt.");
    } finally {
      setIsGenerating(false);
    }
  };

  const handleCopy = async () => {
    if (!result) return;
    await navigator.clipboard.writeText(result);
  };

  return (
    <div className="min-h-screen bg-zinc-50 text-zinc-900 dark:bg-zinc-950 dark:text-zinc-100">
      <main className="mx-auto w-full max-w-6xl px-4 py-10 sm:px-6 lg:px-8">
        <header className="mb-8 space-y-2">
          <h1 className="text-3xl font-semibold tracking-tight">Fera do Prompt - Multimodal Generator</h1>
          <p className="text-sm text-zinc-600 dark:text-zinc-300">
            OpenRouter free models para criar prompts de imagem, roteiro Veo3, JSON Veo3 e animação.
          </p>
        </header>

        <section className="grid gap-6 lg:grid-cols-2">
          <div className="space-y-4 rounded-2xl border border-zinc-200 bg-white p-5 shadow-sm dark:border-zinc-800 dark:bg-zinc-900">
            <h2 className="text-lg font-semibold">Configuração</h2>

            <div className="space-y-2">
              <label className="text-sm font-medium">OpenRouter API Key (opcional se backend já tiver)</label>
              <input
                value={apiKey}
                onChange={(e) => setApiKey(e.target.value)}
                type="password"
                placeholder="sk-or-v1-..."
                className="w-full rounded-xl border border-zinc-300 bg-transparent px-3 py-2 text-sm outline-none focus:border-zinc-500 dark:border-zinc-700"
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Objetivo do Prompt</label>
              <select
                value={purpose}
                onChange={(e) => setPurpose(e.target.value)}
                className="w-full rounded-xl border border-zinc-300 bg-transparent px-3 py-2 text-sm outline-none focus:border-zinc-500 dark:border-zinc-700"
              >
                {PURPOSES.map((item) => (
                  <option key={item.value} value={item.value}>
                    {item.label}
                  </option>
                ))}
              </select>
              <p className="text-xs text-zinc-500">{selectedPurpose?.description}</p>
            </div>

            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <label className="text-sm font-medium">Modelo (OpenRouter free)</label>
                <button
                  onClick={handleLoadFreeModels}
                  disabled={loadingModels}
                  className="rounded-md border border-zinc-300 px-2 py-1 text-xs hover:bg-zinc-100 disabled:opacity-50 dark:border-zinc-700 dark:hover:bg-zinc-800"
                >
                  {loadingModels ? "Carregando..." : "Buscar modelos"}
                </button>
              </div>
              <select
                value={model}
                onChange={(e) => setModel(e.target.value)}
                className="w-full rounded-xl border border-zinc-300 bg-transparent px-3 py-2 text-sm outline-none focus:border-zinc-500 dark:border-zinc-700"
              >
                {model ? null : <option value="">Selecione um modelo</option>}
                {models.map((m) => (
                  <option key={m} value={m}>
                    {m}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="space-y-4 rounded-2xl border border-zinc-200 bg-white p-5 shadow-sm dark:border-zinc-800 dark:bg-zinc-900">
            <h2 className="text-lg font-semibold">Briefing</h2>

            <textarea
              value={brief}
              onChange={(e) => setBrief(e.target.value)}
              placeholder="Ex: Produto de skincare premium em cenário editorial minimalista..."
              className="min-h-[120px] w-full rounded-xl border border-zinc-300 bg-transparent px-3 py-2 text-sm outline-none focus:border-zinc-500 dark:border-zinc-700"
            />
            <textarea
              value={extraContext}
              onChange={(e) => setExtraContext(e.target.value)}
              placeholder="Contexto extra, persona da marca, público, referências..."
              className="min-h-[80px] w-full rounded-xl border border-zinc-300 bg-transparent px-3 py-2 text-sm outline-none focus:border-zinc-500 dark:border-zinc-700"
            />

            <div className="grid gap-3 sm:grid-cols-2">
              <input
                value={language}
                onChange={(e) => setLanguage(e.target.value)}
                placeholder="Idioma (ex: pt-BR)"
                className="w-full rounded-xl border border-zinc-300 bg-transparent px-3 py-2 text-sm outline-none focus:border-zinc-500 dark:border-zinc-700"
              />
              <input
                value={style}
                onChange={(e) => setStyle(e.target.value)}
                placeholder="Estilo (cinemático, realista...)"
                className="w-full rounded-xl border border-zinc-300 bg-transparent px-3 py-2 text-sm outline-none focus:border-zinc-500 dark:border-zinc-700"
              />
              <input
                value={duration}
                onChange={(e) => setDuration(e.target.value)}
                placeholder="Duração (ex: 8s)"
                className="w-full rounded-xl border border-zinc-300 bg-transparent px-3 py-2 text-sm outline-none focus:border-zinc-500 dark:border-zinc-700"
              />
              <input
                value={aspectRatio}
                onChange={(e) => setAspectRatio(e.target.value)}
                placeholder="Aspect ratio (ex: 16:9)"
                className="w-full rounded-xl border border-zinc-300 bg-transparent px-3 py-2 text-sm outline-none focus:border-zinc-500 dark:border-zinc-700"
              />
            </div>

            <button
              onClick={handleGenerate}
              disabled={isGenerating}
              className="inline-flex items-center gap-2 rounded-xl bg-zinc-900 px-4 py-2 text-sm font-medium text-white hover:bg-zinc-700 disabled:opacity-60 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-zinc-300"
            >
              <Sparkles className="h-4 w-4" />
              {isGenerating ? "Gerando..." : "Gerar Prompt"}
            </button>
          </div>
        </section>

        <section className="mt-6 rounded-2xl border border-zinc-200 bg-white p-5 shadow-sm dark:border-zinc-800 dark:bg-zinc-900">
          <div className="mb-3 flex items-center justify-between">
            <h2 className="text-lg font-semibold">Saída</h2>
            <button
              onClick={handleCopy}
              disabled={!result}
              className="rounded-md border border-zinc-300 px-3 py-1 text-xs hover:bg-zinc-100 disabled:opacity-40 dark:border-zinc-700 dark:hover:bg-zinc-800"
            >
              Copiar
            </button>
          </div>

          {error ? (
            <p className="rounded-lg border border-red-300 bg-red-50 p-3 text-sm text-red-700 dark:border-red-800 dark:bg-red-950 dark:text-red-200">
              {error}
            </p>
          ) : null}

          {result ? (
            <article className="prose prose-sm mt-2 max-w-none whitespace-pre-wrap rounded-xl bg-zinc-100 p-4 dark:prose-invert dark:bg-zinc-950">
              <ReactMarkdown>{result}</ReactMarkdown>
            </article>
          ) : (
            <p className="text-sm text-zinc-500">A saída gerada aparecerá aqui.</p>
          )}
        </section>

        <section className="mt-6 rounded-2xl border border-zinc-200 bg-white p-5 shadow-sm dark:border-zinc-800 dark:bg-zinc-900">
          <h2 className="mb-3 text-lg font-semibold">Modelos Gratuitos Curados (Março 2026)</h2>
          <div className="overflow-x-auto">
            <table className="w-full min-w-[920px] border-collapse text-left text-sm">
              <thead>
                <tr className="border-b border-zinc-200 dark:border-zinc-700">
                  <th className="px-2 py-2 font-semibold">Categoria</th>
                  <th className="px-2 py-2 font-semibold">Modelo</th>
                  <th className="px-2 py-2 font-semibold">Provedor</th>
                  <th className="px-2 py-2 font-semibold">Contexto Máx.</th>
                  <th className="px-2 py-2 font-semibold">Capacidades</th>
                  <th className="px-2 py-2 font-semibold">Limitações</th>
                </tr>
              </thead>
              <tbody>
                {catalog.map((item) => (
                  <tr key={item.modelId} className="border-b border-zinc-100 align-top dark:border-zinc-800">
                    <td className="px-2 py-2 text-xs">{item.category}</td>
                    <td className="px-2 py-2 font-mono text-xs">{item.modelId}</td>
                    <td className="px-2 py-2">{item.provider}</td>
                    <td className="px-2 py-2">{item.maxContext}</td>
                    <td className="px-2 py-2">{item.capabilities}</td>
                    <td className="px-2 py-2">{item.limitations}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      </main>
    </div>
  );
}

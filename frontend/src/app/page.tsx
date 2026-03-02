"use client";

import { apiRequest } from "@/lib/apiClient";
import { Sparkles } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import ReactMarkdown from "react-markdown";

type PurposeOption = { value: string; label: string; description: string };
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
  clientSessionId?: string;
  parentGenerationId?: string;
};
type GenerateResponse = {
  provider: string;
  purpose: string;
  model: string;
  generatedPrompt: string;
  generationId?: string;
  usedFallback: boolean;
  attempts: number;
  durationMs: number;
  attemptedModels: string[];
};
type FreeModelCatalogItem = { category: string; modelId: string; provider: string; maxContext: string; capabilities: string; limitations: string };
type HealthResponse = {
  requestsLast24h: number;
  successRateLast24h: number;
  avgLatencyMsLast24h: number;
  fallbackRateLast24h: number;
  dailyQuotaPerIp: number;
  topRankedModels: RankingItem[];
};
type RankingItem = { modelId: string; score: number; totalRequests: number; successRate: number; avgLatencyMs: number; consecutiveFailures: number };
type HistoryItem = {
  id: string;
  parentId?: string;
  version: number;
  purpose: string;
  requestedModel: string;
  finalModel: string;
  brief: string;
  generatedPrompt: string;
  success: boolean;
  usedFallback: boolean;
  attempts: number;
  durationMs: number;
  errorMessage?: string;
  createdAt: string;
};
type CompareResponse = { left: HistoryItem; right: HistoryItem; sameFinalModel: boolean; outputLengthDelta: number; changedLineCountEstimate: number };

const PURPOSES: PurposeOption[] = [
  { value: "nano_banana_image", label: "Gemini Nano Banana (Imagem)", description: "Prompt de imagem focado no Nano Banana." },
  { value: "chatgpt_images", label: "ChatGPT Images (Imagem)", description: "Prompt para geracao de imagem no ChatGPT Images." },
  { value: "veo3_video_script", label: "Google Veo 3 (Roteiro)", description: "Roteiro completo de video com cenas e direcao." },
  { value: "veo3_video_json", label: "Google Veo 3 (JSON)", description: "Saida em JSON tecnico para pipeline de video." },
  { value: "grok_imagine_image", label: "Grok Imagine (Imagem)", description: "Prompt para imagem estatica no Grok Imagine." },
  { value: "grok_imagine_animate", label: "Grok Imagine (Animar Imagem)", description: "Prompt para transformar imagem em video/animacao." },
];

const API_KEY_STORAGE = "feradoprompt_openrouter_api_key";
const SESSION_STORAGE = "feradoprompt_session_id";

function normalizeApiKey(raw: string): string {
  let value = raw.trim().replace(/^["']|["']$/g, "");
  if (value.toLowerCase().startsWith("bearer ")) value = value.slice(7).trim();
  return value.replace(/\s+/g, "");
}

function ensureSessionId(): string {
  const current = localStorage.getItem(SESSION_STORAGE);
  if (current) return current;
  const created = crypto.randomUUID();
  localStorage.setItem(SESSION_STORAGE, created);
  return created;
}

export default function Home() {
  const [apiKey, setApiKey] = useState("");
  const [sessionId, setSessionId] = useState("");
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
  const [parentGenerationId, setParentGenerationId] = useState("");
  const [loadingModels, setLoadingModels] = useState(false);
  const [isGenerating, setIsGenerating] = useState(false);
  const [result, setResult] = useState("");
  const [resultMeta, setResultMeta] = useState<GenerateResponse | null>(null);
  const [error, setError] = useState("");
  const [health, setHealth] = useState<HealthResponse | null>(null);
  const [ranking, setRanking] = useState<RankingItem[]>([]);
  const [history, setHistory] = useState<HistoryItem[]>([]);
  const [leftId, setLeftId] = useState("");
  const [rightId, setRightId] = useState("");
  const [comparison, setComparison] = useState<CompareResponse | null>(null);

  const selectedPurpose = useMemo(() => PURPOSES.find((item) => item.value === purpose), [purpose]);

  const loadHealth = async () => setHealth(await apiRequest<HealthResponse>("GET", "/api/PromptGenerator/health"));
  const loadRanking = async () => setRanking(await apiRequest<RankingItem[]>("GET", `/api/PromptGenerator/models/ranking?purpose=${encodeURIComponent(purpose)}`));
  const loadHistory = async (sid: string) => setHistory(await apiRequest<HistoryItem[]>("GET", `/api/PromptGenerator/history?sessionId=${encodeURIComponent(sid)}&limit=25`));

  useEffect(() => {
    const sid = ensureSessionId();
    setSessionId(sid);
    const saved = localStorage.getItem(API_KEY_STORAGE);
    if (saved) setApiKey(saved);
    const bootstrap = async () => {
      try {
        const [catalogRes, healthRes] = await Promise.all([
          apiRequest<FreeModelCatalogItem[]>("GET", "/api/PromptGenerator/models/catalog"),
          apiRequest<HealthResponse>("GET", "/api/PromptGenerator/health"),
        ]);
        setCatalog(catalogRes);
        const ids = catalogRes.map((item) => item.modelId);
        setModels(ids);
        if (ids.length > 0) setModel(ids[0]);
        setHealth(healthRes);
      } catch {}
      await Promise.all([loadRanking().catch(() => {}), loadHistory(sid).catch(() => {})]);
    };
    bootstrap();
  }, []);

  useEffect(() => {
    localStorage.setItem(API_KEY_STORAGE, apiKey);
  }, [apiKey]);

  const handleLoadFreeModels = async () => {
    setLoadingModels(true);
    setError("");
    try {
      const response = await apiRequest<string[]>("POST", "/api/PromptGenerator/models/free", { apiKey: apiKey || undefined });
      setModels(response);
      if (response.length > 0) setModel(response[0]);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao carregar modelos.");
    } finally {
      setLoadingModels(false);
    }
  };

  const handleGenerate = async () => {
    if (!brief.trim()) return setError("Preencha o briefing.");
    if (!model.trim()) return setError("Escolha um modelo.");
    setIsGenerating(true);
    setError("");
    setResult("");
    setComparison(null);
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
        apiKey: apiKey ? normalizeApiKey(apiKey) : undefined,
        clientSessionId: sessionId || undefined,
        parentGenerationId: parentGenerationId || undefined,
      };
      const response = await apiRequest<GenerateResponse>("POST", "/api/PromptGenerator/generate", payload);
      setResult(response.generatedPrompt);
      setResultMeta(response);
      setParentGenerationId(response.generationId ?? "");
      await Promise.all([loadHealth(), loadRanking(), loadHistory(sessionId)]);
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

  const handleRestore = async (id: string) => {
    const restored = await apiRequest<HistoryItem>("POST", `/api/PromptGenerator/history/${id}/restore`);
    setPurpose(restored.purpose);
    setModel(restored.requestedModel);
    setBrief(restored.brief);
    setResult(restored.generatedPrompt);
    setParentGenerationId(restored.id);
  };

  const handleDuplicate = async (id: string) => {
    await apiRequest<HistoryItem>("POST", `/api/PromptGenerator/history/${id}/duplicate`);
    await loadHistory(sessionId);
  };

  const handleCompare = async () => {
    if (!leftId || !rightId) return;
    const compare = await apiRequest<CompareResponse>("GET", `/api/PromptGenerator/history/compare?leftId=${encodeURIComponent(leftId)}&rightId=${encodeURIComponent(rightId)}`);
    setComparison(compare);
  };

  return (
    <div className="min-h-screen bg-zinc-50 text-zinc-900 dark:bg-zinc-950 dark:text-zinc-100">
      <main className="mx-auto w-full max-w-6xl px-4 py-10 sm:px-6 lg:px-8">
        <header className="mb-8 space-y-2">
          <h1 className="text-3xl font-semibold tracking-tight">Fera do Prompt - Smart Generator</h1>
          <p className="text-sm text-zinc-600 dark:text-zinc-300">Ranking inteligente, fallback automatico, observabilidade e versionamento de prompts.</p>
        </header>

        <section className="mb-6 grid gap-4 md:grid-cols-4">
          <div className="rounded-xl border border-zinc-200 bg-white p-3 dark:border-zinc-800 dark:bg-zinc-900"><p className="text-xs text-zinc-500">Requests 24h</p><p className="text-xl font-semibold">{health?.requestsLast24h ?? 0}</p></div>
          <div className="rounded-xl border border-zinc-200 bg-white p-3 dark:border-zinc-800 dark:bg-zinc-900"><p className="text-xs text-zinc-500">Success 24h</p><p className="text-xl font-semibold">{((health?.successRateLast24h ?? 0) * 100).toFixed(1)}%</p></div>
          <div className="rounded-xl border border-zinc-200 bg-white p-3 dark:border-zinc-800 dark:bg-zinc-900"><p className="text-xs text-zinc-500">Latencia media</p><p className="text-xl font-semibold">{(health?.avgLatencyMsLast24h ?? 0).toFixed(0)} ms</p></div>
          <div className="rounded-xl border border-zinc-200 bg-white p-3 dark:border-zinc-800 dark:bg-zinc-900"><p className="text-xs text-zinc-500">Quota diaria/IP</p><p className="text-xl font-semibold">{health?.dailyQuotaPerIp ?? 0}</p></div>
        </section>

        <section className="grid gap-6 lg:grid-cols-2">
          <div className="space-y-4 rounded-2xl border border-zinc-200 bg-white p-5 shadow-sm dark:border-zinc-800 dark:bg-zinc-900">
            <h2 className="text-lg font-semibold">Configuracao</h2>
            <input value={apiKey} onChange={(e) => setApiKey(e.target.value)} type="password" placeholder="OpenRouter key (opcional)" className="w-full rounded-xl border border-zinc-300 bg-transparent px-3 py-2 text-sm outline-none focus:border-zinc-500 dark:border-zinc-700" />
            <select value={purpose} onChange={(e) => setPurpose(e.target.value)} className="w-full rounded-xl border border-zinc-300 bg-transparent px-3 py-2 text-sm outline-none focus:border-zinc-500 dark:border-zinc-700">
              {PURPOSES.map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}
            </select>
            <p className="text-xs text-zinc-500">{selectedPurpose?.description}</p>
            <div className="flex items-center justify-between">
              <label className="text-sm font-medium">Modelo (OpenRouter free)</label>
              <button onClick={handleLoadFreeModels} disabled={loadingModels} className="rounded-md border border-zinc-300 px-2 py-1 text-xs hover:bg-zinc-100 disabled:opacity-50 dark:border-zinc-700 dark:hover:bg-zinc-800">{loadingModels ? "Carregando..." : "Buscar modelos"}</button>
            </div>
            <select value={model} onChange={(e) => setModel(e.target.value)} className="w-full rounded-xl border border-zinc-300 bg-transparent px-3 py-2 text-sm outline-none focus:border-zinc-500 dark:border-zinc-700">
              {models.map((m) => <option key={m} value={m}>{m}</option>)}
            </select>
          </div>

          <div className="space-y-4 rounded-2xl border border-zinc-200 bg-white p-5 shadow-sm dark:border-zinc-800 dark:bg-zinc-900">
            <h2 className="text-lg font-semibold">Briefing</h2>
            <textarea value={brief} onChange={(e) => setBrief(e.target.value)} placeholder="Descreva o que voce quer gerar..." className="min-h-[120px] w-full rounded-xl border border-zinc-300 bg-transparent px-3 py-2 text-sm outline-none focus:border-zinc-500 dark:border-zinc-700" />
            <textarea value={extraContext} onChange={(e) => setExtraContext(e.target.value)} placeholder="Contexto extra..." className="min-h-[80px] w-full rounded-xl border border-zinc-300 bg-transparent px-3 py-2 text-sm outline-none focus:border-zinc-500 dark:border-zinc-700" />
            <div className="grid gap-3 sm:grid-cols-2">
              <input value={language} onChange={(e) => setLanguage(e.target.value)} placeholder="Idioma" className="w-full rounded-xl border border-zinc-300 bg-transparent px-3 py-2 text-sm outline-none focus:border-zinc-500 dark:border-zinc-700" />
              <input value={style} onChange={(e) => setStyle(e.target.value)} placeholder="Estilo" className="w-full rounded-xl border border-zinc-300 bg-transparent px-3 py-2 text-sm outline-none focus:border-zinc-500 dark:border-zinc-700" />
              <input value={duration} onChange={(e) => setDuration(e.target.value)} placeholder="Duracao" className="w-full rounded-xl border border-zinc-300 bg-transparent px-3 py-2 text-sm outline-none focus:border-zinc-500 dark:border-zinc-700" />
              <input value={aspectRatio} onChange={(e) => setAspectRatio(e.target.value)} placeholder="Aspect ratio" className="w-full rounded-xl border border-zinc-300 bg-transparent px-3 py-2 text-sm outline-none focus:border-zinc-500 dark:border-zinc-700" />
            </div>
            <button onClick={handleGenerate} disabled={isGenerating} className="inline-flex items-center gap-2 rounded-xl bg-zinc-900 px-4 py-2 text-sm font-medium text-white hover:bg-zinc-700 disabled:opacity-60 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-zinc-300"><Sparkles className="h-4 w-4" />{isGenerating ? "Gerando..." : "Gerar Prompt"}</button>
          </div>
        </section>

        <section className="mt-6 rounded-2xl border border-zinc-200 bg-white p-5 shadow-sm dark:border-zinc-800 dark:bg-zinc-900">
          <div className="mb-3 flex items-center justify-between">
            <h2 className="text-lg font-semibold">Saida</h2>
            <button onClick={handleCopy} disabled={!result} className="rounded-md border border-zinc-300 px-3 py-1 text-xs hover:bg-zinc-100 disabled:opacity-40 dark:border-zinc-700 dark:hover:bg-zinc-800">Copiar</button>
          </div>
          {error ? <p className="rounded-lg border border-red-300 bg-red-50 p-3 text-sm text-red-700">{error}</p> : null}
          {resultMeta ? <p className="mb-2 text-xs text-zinc-500">Modelo final: {resultMeta.model} | Tentativas: {resultMeta.attempts} | Fallback: {resultMeta.usedFallback ? "sim" : "nao"} | Tempo: {resultMeta.durationMs}ms</p> : null}
          {result ? <article className="prose prose-sm mt-2 max-w-none whitespace-pre-wrap rounded-xl bg-zinc-100 p-4 dark:prose-invert dark:bg-zinc-950"><ReactMarkdown>{result}</ReactMarkdown></article> : <p className="text-sm text-zinc-500">A saida gerada aparecera aqui.</p>}
        </section>

        <section className="mt-6 grid gap-6 lg:grid-cols-2">
          <div className="rounded-2xl border border-zinc-200 bg-white p-5 shadow-sm dark:border-zinc-800 dark:bg-zinc-900">
            <h2 className="mb-3 text-lg font-semibold">Ranking de Modelos</h2>
            <div className="max-h-72 overflow-auto">
              <table className="w-full text-sm">
                <thead><tr className="border-b border-zinc-200 dark:border-zinc-700"><th className="py-2 text-left">Modelo</th><th className="py-2 text-left">Score</th><th className="py-2 text-left">Success</th></tr></thead>
                <tbody>{ranking.map((item) => <tr key={item.modelId} className="border-b border-zinc-100 dark:border-zinc-800"><td className="py-2 font-mono text-xs">{item.modelId}</td><td className="py-2">{item.score.toFixed(3)}</td><td className="py-2">{(item.successRate * 100).toFixed(1)}%</td></tr>)}</tbody>
              </table>
            </div>
          </div>

          <div className="rounded-2xl border border-zinc-200 bg-white p-5 shadow-sm dark:border-zinc-800 dark:bg-zinc-900">
            <h2 className="mb-3 text-lg font-semibold">Comparar Versoes</h2>
            <div className="grid gap-2 sm:grid-cols-2">
              <select value={leftId} onChange={(e) => setLeftId(e.target.value)} className="rounded-xl border border-zinc-300 bg-transparent px-3 py-2 text-sm"><option value="">Versao A</option>{history.map((h) => <option key={`L-${h.id}`} value={h.id}>v{h.version} - {h.finalModel}</option>)}</select>
              <select value={rightId} onChange={(e) => setRightId(e.target.value)} className="rounded-xl border border-zinc-300 bg-transparent px-3 py-2 text-sm"><option value="">Versao B</option>{history.map((h) => <option key={`R-${h.id}`} value={h.id}>v{h.version} - {h.finalModel}</option>)}</select>
            </div>
            <button onClick={handleCompare} className="mt-3 rounded-md border border-zinc-300 px-3 py-1 text-xs hover:bg-zinc-100 dark:border-zinc-700 dark:hover:bg-zinc-800">Comparar</button>
            {comparison ? <p className="mt-3 text-sm text-zinc-600 dark:text-zinc-300">Mesmo modelo: {comparison.sameFinalModel ? "sim" : "nao"} | Delta de tamanho: {comparison.outputLengthDelta} | Linhas alteradas (estimativa): {comparison.changedLineCountEstimate}</p> : null}
          </div>
        </section>

        <section className="mt-6 rounded-2xl border border-zinc-200 bg-white p-5 shadow-sm dark:border-zinc-800 dark:bg-zinc-900">
          <h2 className="mb-3 text-lg font-semibold">Historico + Versionamento</h2>
          <div className="space-y-2">
            {history.map((item) => (
              <div key={item.id} className="rounded-xl border border-zinc-200 p-3 dark:border-zinc-700">
                <p className="text-xs text-zinc-500">v{item.version} | {new Date(item.createdAt).toLocaleString()} | {item.finalModel}</p>
                <p className="mt-1 text-sm">{item.brief.slice(0, 180)}</p>
                <div className="mt-2 flex gap-2">
                  <button onClick={() => handleRestore(item.id)} className="rounded-md border border-zinc-300 px-2 py-1 text-xs hover:bg-zinc-100 dark:border-zinc-700 dark:hover:bg-zinc-800">Restaurar</button>
                  <button onClick={() => handleDuplicate(item.id)} className="rounded-md border border-zinc-300 px-2 py-1 text-xs hover:bg-zinc-100 dark:border-zinc-700 dark:hover:bg-zinc-800">Duplicar</button>
                </div>
              </div>
            ))}
          </div>
        </section>

        <section className="mt-6 rounded-2xl border border-zinc-200 bg-white p-5 shadow-sm dark:border-zinc-800 dark:bg-zinc-900">
          <h2 className="mb-3 text-lg font-semibold">Catalogo Curado</h2>
          <div className="overflow-x-auto">
            <table className="w-full min-w-[920px] border-collapse text-left text-sm">
              <thead><tr className="border-b border-zinc-200 dark:border-zinc-700"><th className="px-2 py-2">Categoria</th><th className="px-2 py-2">Modelo</th><th className="px-2 py-2">Provedor</th><th className="px-2 py-2">Contexto</th><th className="px-2 py-2">Capacidades</th><th className="px-2 py-2">Limitacoes</th></tr></thead>
              <tbody>{catalog.map((item) => <tr key={item.modelId} className="border-b border-zinc-100 align-top dark:border-zinc-800"><td className="px-2 py-2 text-xs">{item.category}</td><td className="px-2 py-2 font-mono text-xs">{item.modelId}</td><td className="px-2 py-2">{item.provider}</td><td className="px-2 py-2">{item.maxContext}</td><td className="px-2 py-2">{item.capabilities}</td><td className="px-2 py-2">{item.limitations}</td></tr>)}</tbody>
            </table>
          </div>
        </section>
      </main>
    </div>
  );
}

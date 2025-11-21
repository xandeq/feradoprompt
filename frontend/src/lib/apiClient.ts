export type ApiMethod = "GET" | "POST" | "DELETE";

const DEFAULT_BASE_URL = "https://api.feradoprompt.com.br";

const BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? DEFAULT_BASE_URL;

export async function apiRequest<TResponse>(
  method: ApiMethod,
  url: string,
  body?: unknown,
): Promise<TResponse> {
  const finalUrl = `${BASE_URL}${url}`;

  try {
    const response = await fetch(finalUrl, {
      method,
      headers: {
        "Content-Type": "application/json",
      },
      body: body ? JSON.stringify(body) : undefined,
    });

    const isJson =
      response.headers.get("content-type")?.includes("application/json") ??
      false;

    const payload = isJson ? await response.json() : await response.text();

    if (!response.ok) {
      const errorMessage =
        typeof payload === "string"
          ? payload
          : payload?.message ?? "Erro na requisição.";

      throw new Error(errorMessage);
    }

    return payload as TResponse;
  } catch (error) {
    console.error("Erro ao chamar a API feradoprompt:", error);

    if (error instanceof Error) {
      throw error;
    }

    throw new Error("Erro inesperado ao chamar a API feradoprompt.");
  }
}

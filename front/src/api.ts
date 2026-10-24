// src/api.ts
// Única porta de saída para a API. Nenhuma página chama fetch direto.

const BASE = String(import.meta.env.VITE_API_URL ?? "").replace(/\/+$/, "");

const MSG_REDE =
  "Não foi possível falar com a API. Ela pode estar acordando (a hospedagem grátis demora cerca de um minuto) ou há um problema de rede/CORS. Tente de novo em instantes.";

/* ------------------------------------------------------------------ */
/* Tipos (espelham os DTOs do swagger.json)                            */
/* ------------------------------------------------------------------ */

export interface Pagina<T> {
  itens: T[];
  pagina: number;
  tamanho: number;
  total: number;
  totalPaginas: number;
}

export interface Servico {
  id: number;
  nome: string;
  descricao: string | null;
  preco: number;
  duracaoMinutos: number;
  ativo: boolean;
}

export interface ServicoCriar {
  nome: string;
  descricao: string | null;
  preco: number;
  duracaoMinutos: number;
}

// O PUT exige também o campo "ativo".
export interface ServicoAtualizar extends ServicoCriar {
  ativo: boolean;
}

export type StatusAgendamento = "Marcado" | "Cancelado";

export interface Agendamento {
  id: number;
  servicoId: number;
  servicoNome: string | null;
  nomeDoPet: string | null;
  dataHora: string; // ISO com fuso
  dataHoraFim: string; // ISO com fuso
  status: StatusAgendamento;
  usuarioId: number;
}

export interface AgendamentoCriar {
  servicoId: number;
  nomeDoPet: string;
  dataHora: string; // new Date(valor).toISOString()
}

export interface LoginEntrada {
  email: string;
  senha: string;
}

export interface RegistroEntrada {
  email: string;
  senha: string;
}

export interface Token {
  token: string;
  expiraEm: string;
  email: string;
  papel: string; // "admin" | "cliente"
}

export interface Usuario {
  id: number;
  email: string;
  papel: string;
}

/* ------------------------------------------------------------------ */
/* Erro da API                                                         */
/* ------------------------------------------------------------------ */

// Campos declarados explicitamente: o tsconfig do Vite liga
// erasableSyntaxOnly, então nada de "constructor(public x)".
export class ApiError extends Error {
  status: number;
  mensagens: string[];

  constructor(status: number, mensagens: string[]) {
    super(mensagens.join(" "));
    this.name = "ApiError";
    this.status = status;
    this.mensagens = mensagens;
  }
}

interface Problema {
  title?: string | null;
  detail?: string | null;
  errors?: Record<string, string[]> | null;
}

function textoPadrao(status: number): string {
  switch (status) {
    case 401:
      return "Não autorizado.";
    case 403:
      return "Você não tem permissão para fazer isso.";
    case 404:
      return "Não encontrado.";
    default:
      return `A API respondeu com erro ${status}.`;
  }
}

async function criarErro(resposta: Response): Promise<ApiError> {
  let problema: Problema | null = null;
  try {
    problema = (await resposta.json()) as Problema;
  } catch {
    problema = null;
  }

  const mensagens: string[] = [];

  // 400 de validação: objeto campo -> lista de mensagens
  if (problema?.errors) {
    for (const lista of Object.values(problema.errors)) {
      mensagens.push(...lista);
    }
  }

  // 409, 403, 404, 401: o texto útil está em "detail"
  if (mensagens.length === 0) {
    mensagens.push(problema?.detail || problema?.title || textoPadrao(resposta.status));
  }

  return new ApiError(resposta.status, mensagens);
}

/** Texto pronto para mostrar na tela, venha o erro de onde vier. */
export function mensagemDeErro(erro: unknown): string {
  if (erro instanceof ApiError) return erro.message;
  if (erro instanceof Error) return erro.message;
  return "Algo deu errado.";
}

/** Erro de cancelamento (AbortController): a página deve ignorá-lo. */
export function foiCancelado(erro: unknown): boolean {
  return erro instanceof DOMException && erro.name === "AbortError";
}

/* ------------------------------------------------------------------ */
/* Ligação com a sessão                                                */
/* ------------------------------------------------------------------ */

// api.ts não importa sessao.tsx (evita dependência circular).
// O provedor de sessão registra aqui como obter o token e o que fazer num 401.
let obterToken: () => string | null = () => null;
let aoNaoAutorizado: () => void = () => {};

export function configurarApi(config: {
  obterToken: () => string | null;
  aoNaoAutorizado: () => void;
}): void {
  obterToken = config.obterToken;
  aoNaoAutorizado = config.aoNaoAutorizado;
}

/* ------------------------------------------------------------------ */
/* Função principal                                                    */
/* ------------------------------------------------------------------ */

interface Opcoes {
  metodo?: "GET" | "POST" | "PUT" | "DELETE";
  corpo?: unknown;
  signal?: AbortSignal;
}

export async function api<T>(caminho: string, opcoes: Opcoes = {}): Promise<T> {
  const token = obterToken();

  const headers: Record<string, string> = { Accept: "application/json" };
  if (opcoes.corpo !== undefined) headers["Content-Type"] = "application/json";
  if (token) headers.Authorization = `Bearer ${token}`;

  let resposta: Response;
  try {
    resposta = await fetch(BASE + caminho, {
      method: opcoes.metodo ?? "GET",
      headers,
      body: opcoes.corpo !== undefined ? JSON.stringify(opcoes.corpo) : undefined,
      signal: opcoes.signal,
    });
  } catch (e) {
    if (foiCancelado(e)) throw e;
    // rede fora, CORS ou API dormindo
    throw new ApiError(0, [MSG_REDE]);
  }

  if (!resposta.ok) {
    const erro = await criarErro(resposta);
    // 401 com token enviado = sessão vencida/inválida.
    // 401 sem token (senha errada no login) NÃO derruba nada.
    if (resposta.status === 401 && token) aoNaoAutorizado();
    throw erro;
  }

  if (resposta.status === 204) return undefined as T;
  return (await resposta.json()) as T;
}

/* ------------------------------------------------------------------ */
/* Utilitário de query string                                          */
/* ------------------------------------------------------------------ */

/** montarQuery({ busca: "banho", pagina: 2, x: "" }) -> "?busca=banho&pagina=2" */
export function montarQuery(params: Record<string, string | number | null | undefined>): string {
  const q = new URLSearchParams();
  for (const [chave, valor] of Object.entries(params)) {
    if (valor !== undefined && valor !== null && valor !== "") {
      q.set(chave, String(valor));
    }
  }
  const texto = q.toString();
  return texto ? `?${texto}` : "";
}

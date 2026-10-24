// src/sessao.tsx
// Sessão do usuário: Context + localStorage + saída automática no expiraEm.

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useLayoutEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import type { ReactNode } from "react";
import { api, configurarApi } from "./api";
import type { LoginEntrada, Token } from "./api";

export interface Sessao {
  token: string;
  email: string;
  papel: string; // "admin" | "cliente"
  expiraEm: string; // ISO
}

interface ContextoSessao {
  sessao: Sessao | null;
  ehAdmin: boolean;
  /** Aviso para mostrar na tela de login ("sua sessão expirou"). */
  aviso: string | null;
  limparAviso: () => void;
  entrar: (dados: LoginEntrada) => Promise<Sessao>;
  sair: () => void;
}

const CHAVE = "patinhas.sessao";
const MSG_EXPIROU = "Sua sessão expirou. Entre de novo.";
const MAX_TIMEOUT = 2 ** 31 - 1; // limite do setTimeout (~24 dias)

/* ------------------------------------------------------------------ */
/* localStorage (sempre com try/catch: pode estar bloqueado)           */
/* ------------------------------------------------------------------ */

function sessaoValida(s: Sessao): boolean {
  const fim = new Date(s.expiraEm).getTime();
  return Number.isFinite(fim) && fim > Date.now();
}

function lerSessaoSalva(): Sessao | null {
  try {
    const texto = localStorage.getItem(CHAVE);
    if (!texto) return null;
    const s = JSON.parse(texto) as Partial<Sessao>;
    if (
      typeof s.token !== "string" ||
      typeof s.email !== "string" ||
      typeof s.papel !== "string" ||
      typeof s.expiraEm !== "string"
    ) {
      localStorage.removeItem(CHAVE);
      return null;
    }
    const completa = s as Sessao;
    if (!sessaoValida(completa)) {
      localStorage.removeItem(CHAVE);
      return null;
    }
    return completa;
  } catch {
    return null;
  }
}

function salvarSessao(s: Sessao | null): void {
  try {
    if (s) localStorage.setItem(CHAVE, JSON.stringify(s));
    else localStorage.removeItem(CHAVE);
  } catch {
    // sem localStorage a sessão vale só até recarregar a página
  }
}

/* ------------------------------------------------------------------ */
/* Context                                                             */
/* ------------------------------------------------------------------ */

const Contexto = createContext<ContextoSessao | null>(null);

export function ProvedorSessao({ children }: { children: ReactNode }) {
  const [sessao, setSessao] = useState<Sessao | null>(lerSessaoSalva);
  const [aviso, setAviso] = useState<string | null>(null);

  // O token fica também numa ref: o api.ts lê o valor atual sem depender de render.
  const tokenRef = useRef<string | null>(sessao?.token ?? null);

  const aplicar = useCallback((nova: Sessao | null) => {
    tokenRef.current = nova?.token ?? null;
    setSessao(nova);
    salvarSessao(nova);
  }, []);

  const sair = useCallback(() => {
    aplicar(null);
  }, [aplicar]);

  const derrubar = useCallback(
    (mensagem: string) => {
      aplicar(null);
      setAviso(mensagem);
    },
    [aplicar],
  );

  // Liga o api.ts à sessão. useLayoutEffect roda antes dos useEffect das
  // páginas, então o primeiro fetch de qualquer página já sai com o token.
  useLayoutEffect(() => {
    configurarApi({
      obterToken: () => tokenRef.current,
      aoNaoAutorizado: () => derrubar(MSG_EXPIROU), // qualquer 401 com token
    });
  }, [derrubar]);

  // Sai sozinho na hora do expiraEm.
  useEffect(() => {
    if (!sessao) return;
    const falta = new Date(sessao.expiraEm).getTime() - Date.now();
    if (falta <= 0) {
      derrubar(MSG_EXPIROU);
      return;
    }
    const id = window.setTimeout(() => derrubar(MSG_EXPIROU), Math.min(falta, MAX_TIMEOUT));
    return () => window.clearTimeout(id);
  }, [sessao, derrubar]);

  const entrar = useCallback(
    async (dados: LoginEntrada): Promise<Sessao> => {
      const resposta = await api<Token>("/api/auth/login", {
        metodo: "POST",
        corpo: dados,
      });
      const nova: Sessao = {
        token: resposta.token,
        email: resposta.email,
        papel: resposta.papel,
        expiraEm: resposta.expiraEm,
      };
      aplicar(nova);
      setAviso(null);
      return nova;
    },
    [aplicar],
  );

  const limparAviso = useCallback(() => setAviso(null), []);

  const valor = useMemo<ContextoSessao>(
    () => ({
      sessao,
      ehAdmin: sessao?.papel.toLowerCase() === "admin",
      aviso,
      limparAviso,
      entrar,
      sair,
    }),
    [sessao, aviso, limparAviso, entrar, sair],
  );

  return <Contexto.Provider value={valor}>{children}</Contexto.Provider>;
}

export function useSessao(): ContextoSessao {
  const ctx = useContext(Contexto);
  if (!ctx) throw new Error("useSessao precisa estar dentro de <ProvedorSessao>.");
  return ctx;
}
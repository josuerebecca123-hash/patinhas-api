// src/componentes/Toasts.tsx
// Avisos curtos (sucesso, erro, informação) num canto da tela.
// Somem sozinhos e ficam numa região aria-live para leitores de tela.
//
// Uso em qualquer página:
//   const toast = useToasts();
//   toast.sucesso("Horário cancelado.");
//   toast.erro("Não foi possível cancelar.");
//   toast.info("Carregando…");

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import type { ReactNode } from "react";
import "./Toasts.css";

type Tipo = "sucesso" | "erro" | "info";

interface Toast {
  id: number;
  tipo: Tipo;
  texto: string;
}

interface ContextoToasts {
  mostrar: (tipo: Tipo, texto: string) => void;
  sucesso: (texto: string) => void;
  erro: (texto: string) => void;
  info: (texto: string) => void;
}

const MAXIMO_NA_TELA = 4;
const DURACAO_MS: Record<Tipo, number> = { sucesso: 5000, info: 5000, erro: 8000 };
const ROTULO: Record<Tipo, string> = { sucesso: "Sucesso", erro: "Erro", info: "Aviso" };

const Contexto = createContext<ContextoToasts | null>(null);

export function ProvedorToasts({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);
  const proximoId = useRef(1);
  const relogios = useRef(new Map<number, number>());

  const remover = useCallback((id: number) => {
    const relogio = relogios.current.get(id);
    if (relogio !== undefined) window.clearTimeout(relogio);
    relogios.current.delete(id);
    setToasts((lista) => lista.filter((t) => t.id !== id));
  }, []);

  const mostrar = useCallback(
    (tipo: Tipo, texto: string) => {
      const id = proximoId.current++;
      setToasts((lista) => [...lista, { id, tipo, texto }].slice(-MAXIMO_NA_TELA));
      relogios.current.set(
        id,
        window.setTimeout(() => remover(id), DURACAO_MS[tipo]),
      );
    },
    [remover],
  );

  // Ao desmontar, cancela os relógios que ainda estiverem rodando.
  useEffect(() => {
    const mapa = relogios.current;
    return () => {
      mapa.forEach((relogio) => window.clearTimeout(relogio));
      mapa.clear();
    };
  }, []);

  const valor = useMemo<ContextoToasts>(
    () => ({
      mostrar,
      sucesso: (texto) => mostrar("sucesso", texto),
      erro: (texto) => mostrar("erro", texto),
      info: (texto) => mostrar("info", texto),
    }),
    [mostrar],
  );

  return (
    <Contexto.Provider value={valor}>
      {children}

      {/* A região fica sempre na página: leitores de tela só anunciam
          o que entra numa região que já existia. */}
      <div className="toasts" role="region" aria-label="Avisos" aria-live="polite">
        {toasts.map((t) => (
          <div
            key={t.id}
            className={`toast toast--${t.tipo}`}
            role={t.tipo === "erro" ? "alert" : "status"}
          >
            <p className="toast__texto">
              <span className="sr-only">{ROTULO[t.tipo]}: </span>
              {t.texto}
            </p>
            <button
              type="button"
              className="toast__fechar"
              aria-label="Fechar aviso"
              onClick={() => remover(t.id)}
            >
              <svg viewBox="0 0 24 24" width="16" height="16" aria-hidden="true" focusable="false">
                <path
                  d="M6 6l12 12M18 6L6 18"
                  stroke="currentColor"
                  strokeWidth="2.5"
                  strokeLinecap="round"
                  fill="none"
                />
              </svg>
            </button>
          </div>
        ))}
      </div>
    </Contexto.Provider>
  );
}

export function useToasts(): ContextoToasts {
  const ctx = useContext(Contexto);
  if (!ctx) throw new Error("useToasts precisa estar dentro de <ProvedorToasts>.");
  return ctx;
}
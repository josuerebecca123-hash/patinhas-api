// src/componentes/BotaoTema.tsx
// Alterna entre tema claro e escuro.
// - o script do index.html já aplicou o tema antes da primeira pintura
// - aqui só lemos o que está aplicado e trocamos o atributo data-tema do <html>
// - a escolha fica no localStorage ("patinhas.tema"); sem escolha, segue o sistema

import { useEffect, useState } from "react";
import "./BotaoTema.css";

type Tema = "claro" | "escuro";

const CHAVE = "patinhas.tema";

function temaAplicado(): Tema {
  return document.documentElement.getAttribute("data-tema") === "escuro" ? "escuro" : "claro";
}

function escolhaSalva(): Tema | null {
  try {
    const valor = localStorage.getItem(CHAVE);
    return valor === "claro" || valor === "escuro" ? valor : null;
  } catch {
    return null;
  }
}

export default function BotaoTema() {
  const [tema, setTema] = useState<Tema>(temaAplicado);

  // Sem escolha salva, acompanha o sistema se ele mudar com a página aberta.
  useEffect(() => {
    const sistema = window.matchMedia("(prefers-color-scheme: dark)");

    function aoMudar(e: MediaQueryListEvent) {
      if (escolhaSalva()) return;
      const novo: Tema = e.matches ? "escuro" : "claro";
      document.documentElement.setAttribute("data-tema", novo);
      setTema(novo);
    }

    sistema.addEventListener("change", aoMudar);
    return () => sistema.removeEventListener("change", aoMudar);
  }, []);

  function alternar() {
    const novo: Tema = tema === "escuro" ? "claro" : "escuro";
    document.documentElement.setAttribute("data-tema", novo);
    setTema(novo);
    try {
      localStorage.setItem(CHAVE, novo);
    } catch {
      // sem localStorage a escolha vale só até recarregar
    }
  }

  return (
    <button
      type="button"
      className="botao-tema"
      onClick={alternar}
      aria-label="Modo escuro"
      aria-pressed={tema === "escuro"}
    >
      {tema === "escuro" ? (
        // sol: clicar volta para o claro
        <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true" focusable="false">
          <circle cx="12" cy="12" r="4.5" fill="currentColor" />
          <path
            d="M12 2v2.5M12 19.5V22M2 12h2.5M19.5 12H22M4.9 4.9l1.8 1.8M17.3 17.3l1.8 1.8M4.9 19.1l1.8-1.8M17.3 6.7l1.8-1.8"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            fill="none"
          />
        </svg>
      ) : (
        // lua: clicar vai para o escuro
        <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true" focusable="false">
          <path d="M21 12.8A9 9 0 1 1 11.2 3a7 7 0 0 0 9.8 9.8z" fill="currentColor" />
        </svg>
      )}
    </button>
  );
}
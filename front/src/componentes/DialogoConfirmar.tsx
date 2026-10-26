// src/componentes/DialogoConfirmar.tsx
// Pergunta "tem certeza?" num <dialog> nativo.
// - showModal(): fundo bloqueado e foco preso dentro do diálogo
// - Esc fecha (e clicar fora também)
// - o foco começa no botão SEGURO (o que não faz nada de irreversível)
//
// Uso:
//   <DialogoConfirmar
//     aberto={alvo !== null}
//     titulo="Cancelar este horário?"
//     texto="Banho porte médio para Tico, 04/12/2026 12:00."
//     rotuloSeguro="Manter horário"
//     rotuloConfirmar="Cancelar horário"
//     perigo
//     ocupado={cancelando}
//     aoConfirmar={confirmar}
//     aoFechar={() => setAlvo(null)}
//   />

import { useEffect, useId, useRef } from "react";
import type { MouseEvent } from "react";
import "./DialogoConfirmar.css";

interface Props {
  aberto: boolean;
  titulo: string;
  texto: string;
  /** Botão seguro: fecha sem fazer nada. É onde o foco começa. */
  rotuloSeguro?: string;
  rotuloConfirmar: string;
  /** Pinta o botão de confirmar como ação destrutiva. */
  perigo?: boolean;
  /** Enquanto a chamada à API roda: trava os botões e o Esc. */
  ocupado?: boolean;
  aoConfirmar: () => void;
  aoFechar: () => void;
}

export default function DialogoConfirmar({
  aberto,
  titulo,
  texto,
  rotuloSeguro = "Voltar",
  rotuloConfirmar,
  perigo = false,
  ocupado = false,
  aoConfirmar,
  aoFechar,
}: Props) {
  const dialogo = useRef<HTMLDialogElement>(null);
  const botaoSeguro = useRef<HTMLButtonElement>(null);
  const idTitulo = useId();
  const idTexto = useId();

  // Abre/fecha o <dialog> conforme a prop "aberto".
  useEffect(() => {
    const d = dialogo.current;
    if (!d) return;
    if (aberto && !d.open) {
      d.showModal();
      botaoSeguro.current?.focus();
    } else if (!aberto && d.open) {
      d.close();
    }
  }, [aberto]);

  // Clique no fundo escuro (no próprio <dialog>, fora do conteúdo) fecha.
  function aoClicarNoFundo(e: MouseEvent<HTMLDialogElement>) {
    if (e.target === e.currentTarget && !ocupado) aoFechar();
  }

  return (
    <dialog
      ref={dialogo}
      className="dialogo"
      aria-labelledby={idTitulo}
      aria-describedby={idTexto}
      // Esc dispara "cancel": enquanto a API responde, não deixa fechar.
      onCancel={(e) => {
        if (ocupado) e.preventDefault();
      }}
      // "close" dispara quando o diálogo fecha (Esc, botão ou código).
      onClose={aoFechar}
      onClick={aoClicarNoFundo}
    >
      <div className="dialogo__conteudo">
        <h2 id={idTitulo}>{titulo}</h2>
        <p id={idTexto}>{texto}</p>

        <div className="dialogo__acoes">
          <button
            ref={botaoSeguro}
            type="button"
            className="botao botao--secundario"
            onClick={aoFechar}
            disabled={ocupado}
          >
            {rotuloSeguro}
          </button>
          <button
            type="button"
            className={perigo ? "botao botao--perigo" : "botao"}
            onClick={aoConfirmar}
            disabled={ocupado}
          >
            {ocupado ? "Aguarde…" : rotuloConfirmar}
          </button>
        </div>
      </div>
    </dialog>
  );
}
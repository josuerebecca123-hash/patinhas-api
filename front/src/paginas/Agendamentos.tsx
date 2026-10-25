// src/paginas/Agendamentos.tsx
// Página protegida: marcar um horário (POST) e ver/cancelar os agendamentos.
// Erros da API aparecem na tela: 400 (lista de validação) e 409 (horário ocupado).

import { useEffect, useState, type FormEvent } from "react";
import { useSearchParams } from "react-router";
import {
  api,
  ApiError,
  foiCancelado,
  mensagemDeErro,
  montarQuery,
  type Agendamento,
  type AgendamentoCriar,
  type Pagina,
  type Servico,
} from "../api";
import { formatarDataHora, formatarDuracao, formatarHora, formatarPreco } from "../formato";
import { usarTitulo } from "../usarTitulo";
import "./Agenda.css";

const TAMANHO = 5;

interface Estado {
  chave: string;
  dados?: Pagina<Agendamento>;
  erro?: string;
}

function lerPagina(valor: string | null): number {
  const n = Number(valor);
  return Number.isInteger(n) && n >= 1 ? n : 1;
}

// Valor mínimo do campo datetime-local: agora, no horário do navegador.
function agoraParaInput(): string {
  const d = new Date();
  d.setMinutes(d.getMinutes() - d.getTimezoneOffset());
  return d.toISOString().slice(0, 16);
}

export default function Agendamentos() {
  usarTitulo("Agendamentos");

  const [params, setParams] = useSearchParams();
  const pagina = lerPagina(params.get("pagina"));
  // vindo do detalhe do serviço: /agendamentos?servico=3 já deixa o serviço escolhido
  const servicoSugerido = params.get("servico") ?? "";

  // ----- lista de agendamentos -----
  const [estado, setEstado] = useState<Estado | null>(null);
  const [tentativa, setTentativa] = useState(0);
  const chave = `${pagina}|${tentativa}`;
  const carregando = estado?.chave !== chave;

  useEffect(() => {
    const controle = new AbortController();
    api<Pagina<Agendamento>>("/api/agendamentos" + montarQuery({ pagina, tamanho: TAMANHO }), {
      signal: controle.signal,
    })
      .then((dados) => setEstado({ chave, dados }))
      .catch((e: unknown) => {
        if (foiCancelado(e)) return;
        setEstado({ chave, erro: mensagemDeErro(e) });
      });
    return () => controle.abort();
  }, [pagina, chave]);

  // ----- serviços para o campo "Serviço" -----
  const [servicos, setServicos] = useState<Servico[] | null>(null);
  const [erroServicos, setErroServicos] = useState<string | null>(null);

  useEffect(() => {
    const controle = new AbortController();
    api<Pagina<Servico>>("/api/servicos" + montarQuery({ tamanho: 100 }), { signal: controle.signal })
      .then((dados) => setServicos((dados.itens ?? []).filter((s) => s.ativo)))
      .catch((e: unknown) => {
        if (!foiCancelado(e)) setErroServicos(mensagemDeErro(e));
      });
    return () => controle.abort();
  }, []);

  // ----- formulário e ações -----
  const [errosForm, setErrosForm] = useState<string[]>([]);
  const [enviando, setEnviando] = useState(false);
  const [sucesso, setSucesso] = useState<string | null>(null);
  const [erroLista, setErroLista] = useState<string | null>(null);
  const [cancelando, setCancelando] = useState<number | null>(null);

  async function marcar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (enviando) return;

    const formulario = e.currentTarget;
    const dados = new FormData(formulario);
    const servicoId = Number(dados.get("servicoId"));
    const nomeDoPet = String(dados.get("nomeDoPet") ?? "").trim();
    const data = new Date(String(dados.get("dataHora") ?? ""));

    if (!servicoId || !nomeDoPet || Number.isNaN(data.getTime())) {
      setErrosForm(["Escolha o serviço, informe o nome do pet e a data e hora."]);
      return;
    }

    const corpo: AgendamentoCriar = { servicoId, nomeDoPet, dataHora: data.toISOString() };

    setErrosForm([]);
    setSucesso(null);
    setEnviando(true);
    try {
      const criado = await api<Agendamento>("/api/agendamentos", { metodo: "POST", corpo });
      formulario.reset();
      setSucesso(
        `Horário marcado: ${criado.servicoNome ?? "serviço"} para ${criado.nomeDoPet ?? nomeDoPet}, ${formatarDataHora(criado.dataHora)}.`,
      );
      if (pagina > 1) setParams({});
      else setTentativa((t) => t + 1);
    } catch (erro) {
      setErrosForm(erro instanceof ApiError ? erro.mensagens : [mensagemDeErro(erro)]);
    } finally {
      setEnviando(false);
    }
  }

  async function cancelar(id: number) {
    setErroLista(null);
    setSucesso(null);
    setCancelando(id);
    try {
      await api<Agendamento>(`/api/agendamentos/${id}/cancelar`, { metodo: "POST" });
      setSucesso("Agendamento cancelado.");
      setTentativa((t) => t + 1);
    } catch (erro) {
      setErroLista(mensagemDeErro(erro));
    } finally {
      setCancelando(null);
    }
  }

  function mudarPagina(nova: number) {
    setParams(nova > 1 ? { pagina: String(nova) } : {});
  }

  const dados = estado?.chave === chave ? estado.dados : undefined;
  const erro = estado?.chave === chave ? estado.erro : undefined;
  const itens = dados?.itens ?? [];

  return (
    <section aria-labelledby="titulo-agenda">
      <h1 id="titulo-agenda">Meus agendamentos</h1>

      <div className="agenda__status" role="status">
        {sucesso && <p className="alerta alerta--ok">{sucesso}</p>}
      </div>

      <div className="agenda">
        {/* ---------- marcar um horário ---------- */}
        <form className="cartao formulario agenda__form" onSubmit={marcar}>
          <h2>Marcar um horário</h2>

          {erroServicos && (
            <p className="alerta alerta--erro" role="alert">
              {erroServicos}
            </p>
          )}

          {errosForm.length > 0 && (
            <div className="alerta alerta--erro" role="alert">
              {errosForm.length === 1 ? (
                <p>{errosForm[0]}</p>
              ) : (
                <ul>
                  {errosForm.map((mensagem, i) => (
                    <li key={i}>{mensagem}</li>
                  ))}
                </ul>
              )}
            </div>
          )}

          <div className="campo">
            <label htmlFor="servicoId">Serviço</label>
            <select
              key={servicos ? "pronto" : "carregando"}
              id="servicoId"
              name="servicoId"
              required
              disabled={!servicos || enviando}
              defaultValue={servicoSugerido}
            >
              <option value="">{servicos ? "Escolha um serviço" : "Carregando serviços…"}</option>
              {servicos?.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.nome} ({formatarPreco(s.preco)}, {formatarDuracao(s.duracaoMinutos)})
                </option>
              ))}
            </select>
          </div>

          <div className="campo">
            <label htmlFor="nomeDoPet">Nome do pet</label>
            <input
              id="nomeDoPet"
              name="nomeDoPet"
              type="text"
              maxLength={100}
              autoComplete="off"
              required
              disabled={enviando}
            />
          </div>

          <div className="campo">
            <label htmlFor="dataHora">Data e hora</label>
            <input
              id="dataHora"
              name="dataHora"
              type="datetime-local"
              min={agoraParaInput()}
              required
              disabled={enviando}
            />
          </div>

          <button type="submit" className="botao" disabled={enviando}>
            {enviando ? "Marcando…" : "Marcar horário"}
          </button>
        </form>

        {/* ---------- lista ---------- */}
        <div className="agenda__lista">
          <h2>Seus horários</h2>

          {erroLista && (
            <p className="alerta alerta--erro agenda__aviso" role="alert">
              {erroLista}
            </p>
          )}

          {carregando && (
            <>
              <p className="sr-only" role="status">
                Carregando agendamentos…
              </p>
              <ul className="agenda__itens" aria-hidden="true">
                {Array.from({ length: 3 }, (_, i) => (
                  <li key={i} className="cartao agendamento agendamento--esqueleto">
                    <span className="esqueleto esq--titulo" />
                    <span className="esqueleto esq--linha" />
                    <span className="esqueleto esq--linha esq--curta" />
                  </li>
                ))}
              </ul>
            </>
          )}

          {!carregando && erro && (
            <div className="alerta alerta--erro agenda__erro" role="alert">
              <p>{erro}</p>
              <button
                type="button"
                className="botao botao--secundario botao--pequeno"
                onClick={() => setTentativa((t) => t + 1)}
              >
                Tentar de novo
              </button>
            </div>
          )}

          {!carregando && dados && itens.length === 0 && (
            <div className="cartao agenda__vazio">
              <p>
                {pagina > 1
                  ? "Essa página não tem agendamentos."
                  : "Você ainda não tem agendamentos. Use o formulário para marcar o primeiro."}
              </p>
              {pagina > 1 && (
                <button type="button" className="botao" onClick={() => mudarPagina(1)}>
                  Voltar ao início
                </button>
              )}
            </div>
          )}

          {!carregando && dados && itens.length > 0 && (
            <>
              <ul className="agenda__itens">
                {itens.map((a) => (
                  <li
                    key={a.id}
                    className={`cartao agendamento${a.status === "Cancelado" ? " agendamento--cancelado" : ""}`}
                  >
                    <div className="agendamento__topo">
                      <h3>{a.servicoNome ?? "Serviço"}</h3>
                      <span className={`etiqueta etiqueta--${(a.status ?? "").toLowerCase()}`}>{a.status}</span>
                    </div>
                    <p>
                      Pet: <strong>{a.nomeDoPet}</strong>
                    </p>
                    <p className="agendamento__quando">
                      {formatarDataHora(a.dataHora)} até {formatarHora(a.dataHoraFim)}
                    </p>
                    {a.status === "Marcado" && (
                      <button
                        type="button"
                        className="botao botao--secundario botao--pequeno"
                        disabled={cancelando === a.id}
                        aria-label={`Cancelar ${a.servicoNome ?? "agendamento"} de ${a.nomeDoPet ?? "pet"} em ${formatarDataHora(a.dataHora)}`}
                        onClick={() => cancelar(a.id)}
                      >
                        {cancelando === a.id ? "Cancelando…" : "Cancelar"}
                      </button>
                    )}
                  </li>
                ))}
              </ul>

              {dados.totalPaginas > 1 && (
                <nav className="paginacao" aria-label="Paginação">
                  <button
                    type="button"
                    className="botao botao--secundario botao--pequeno"
                    disabled={dados.pagina <= 1}
                    onClick={() => mudarPagina(dados.pagina - 1)}
                  >
                    Anterior
                  </button>
                  <span>
                    Página {dados.pagina} de {dados.totalPaginas}
                  </span>
                  <button
                    type="button"
                    className="botao botao--secundario botao--pequeno"
                    disabled={dados.pagina >= dados.totalPaginas}
                    onClick={() => mudarPagina(dados.pagina + 1)}
                  >
                    Próxima
                  </button>
                </nav>
              )}
            </>
          )}
        </div>
      </div>
    </section>
  );
}
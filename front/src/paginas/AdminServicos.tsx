// src/paginas/AdminServicos.tsx
// Painel do admin: criar (POST), editar (PUT, que exige "ativo") e excluir (DELETE) serviços.
// - A lista é pedida COM token, então a API devolve também os serviços inativos.
// - 400 de validação aparece dentro do formulário; 409 (serviço com agendamentos) vira toast de erro.
// - Quem não é admin vê um aviso (o servidor também confere: 403).

import { useEffect, useRef, useState, type FormEvent } from "react";
import { Link, useSearchParams } from "react-router";
import {
  api,
  ApiError,
  foiCancelado,
  mensagemDeErro,
  montarQuery,
  type Pagina,
  type Servico,
  type ServicoAtualizar,
  type ServicoCriar,
} from "../api";
import DialogoConfirmar from "../componentes/DialogoConfirmar";
import { useToasts } from "../componentes/Toasts";
import { formatarDuracao, formatarPreco } from "../formato";
import { useSessao } from "../sessao";
import { usarTitulo } from "../usarTitulo";
import "./AdminServicos.css";

const TAMANHO = 8;

interface Estado {
  chave: string;
  dados?: Pagina<Servico>;
  erro?: string;
}

// "novo" = formulário de criação; Servico = editando esse serviço; null = formulário fechado
type Alvo = "novo" | Servico | null;

function lerPagina(valor: string | null): number {
  const n = Number(valor);
  return Number.isInteger(n) && n >= 1 ? n : 1;
}

export default function AdminServicos() {
  usarTitulo("Admin · Serviços");

  const { ehAdmin } = useSessao();

  if (!ehAdmin) {
    return (
      <section className="admin__aviso" aria-labelledby="titulo-sem-acesso">
        <h1 id="titulo-sem-acesso">Área restrita</h1>
        <p className="alerta alerta--erro" role="alert">
          Esta página é só para administradores. Sua conta é de cliente.
        </p>
        <Link className="botao" to="/">
          Voltar para os serviços
        </Link>
      </section>
    );
  }

  return <Painel />;
}

function Painel() {
  const toast = useToasts();
  const [params, setParams] = useSearchParams();
  const pagina = lerPagina(params.get("pagina"));

  // ----- lista -----
  const [estado, setEstado] = useState<Estado | null>(null);
  const [tentativa, setTentativa] = useState(0);
  const chave = `${pagina}|${tentativa}`;
  const carregando = estado?.chave !== chave;

  useEffect(() => {
    const controle = new AbortController();
    api<Pagina<Servico>>("/api/servicos" + montarQuery({ pagina, tamanho: TAMANHO }), {
      signal: controle.signal,
    })
      .then((dados) => setEstado({ chave, dados }))
      .catch((e: unknown) => {
        if (foiCancelado(e)) return;
        setEstado({ chave, erro: mensagemDeErro(e) });
      });
    return () => controle.abort();
  }, [pagina, chave]);

  const dados = estado?.chave === chave ? estado.dados : undefined;
  const erro = estado?.chave === chave ? estado.erro : undefined;
  const itens = dados?.itens ?? [];

  // ----- formulário (criar / editar) -----
  const [alvo, setAlvo] = useState<Alvo>(null);
  const [errosForm, setErrosForm] = useState<string[]>([]);
  const [salvando, setSalvando] = useState(false);
  const campoNome = useRef<HTMLInputElement>(null);
  const editando = alvo !== null && alvo !== "novo" ? alvo : null;

  // Ao abrir o formulário, leva o foco para o primeiro campo.
  useEffect(() => {
    if (alvo) campoNome.current?.focus();
  }, [alvo]);

  function abrirForm(novo: Alvo) {
    setErrosForm([]);
    setAlvo(novo);
  }

  function fecharForm() {
    setErrosForm([]);
    setAlvo(null);
  }

  async function salvar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (salvando || !alvo) return;

    const f = new FormData(e.currentTarget);
    const nome = String(f.get("nome") ?? "").trim();
    const descricao = String(f.get("descricao") ?? "").trim();
    const preco = Number(f.get("preco"));
    const duracaoMinutos = Number(f.get("duracaoMinutos"));

    if (!nome || Number.isNaN(preco) || Number.isNaN(duracaoMinutos)) {
      setErrosForm(["Informe o nome, o preço e a duração."]);
      return;
    }

    const base: ServicoCriar = { nome, descricao: descricao || null, preco, duracaoMinutos };

    setErrosForm([]);
    setSalvando(true);
    try {
      if (alvo === "novo") {
        await api<Servico>("/api/servicos", { metodo: "POST", corpo: base });
        toast.sucesso("Serviço criado.");
      } else {
        // O PUT exige também o campo "ativo".
        const corpo: ServicoAtualizar = { ...base, ativo: f.get("ativo") === "on" };
        await api<Servico>(`/api/servicos/${alvo.id}`, { metodo: "PUT", corpo });
        toast.sucesso("Serviço atualizado.");
      }
      fecharForm();
      setTentativa((t) => t + 1);
    } catch (erroApi) {
      setErrosForm(erroApi instanceof ApiError ? erroApi.mensagens : [mensagemDeErro(erroApi)]);
    } finally {
      setSalvando(false);
    }
  }

  // ----- excluir (com confirmação) -----
  const [paraExcluir, setParaExcluir] = useState<Servico | null>(null);
  const [excluindo, setExcluindo] = useState(false);

  function mudarPagina(nova: number) {
    setParams(nova > 1 ? { pagina: String(nova) } : {});
  }

  async function confirmarExclusao() {
    if (!paraExcluir) return;

    setExcluindo(true);
    try {
      await api<void>(`/api/servicos/${paraExcluir.id}`, { metodo: "DELETE" });
      toast.sucesso("Serviço excluído.");
      // Se era o último da página, volta uma página; senão só recarrega.
      if (itens.length === 1 && pagina > 1) mudarPagina(pagina - 1);
      else setTentativa((t) => t + 1);
    } catch (erroApi) {
      // 409: serviço com agendamentos. O "detail" da API vira o texto do toast.
      toast.erro(mensagemDeErro(erroApi));
    } finally {
      setExcluindo(false);
      setParaExcluir(null);
    }
  }

  return (
    <section aria-labelledby="titulo-admin">
      <div className="admin__topo">
        <h1 id="titulo-admin">Serviços (admin)</h1>
        {alvo === null && (
          <button type="button" className="botao" onClick={() => abrirForm("novo")}>
            Novo serviço
          </button>
        )}
      </div>

      {/* ---------- formulário ---------- */}
      {alvo !== null && (
        <form
          key={editando ? editando.id : "novo"}
          className="cartao formulario admin__form"
          onSubmit={salvar}
        >
          <h2>{editando ? `Editar: ${editando.nome}` : "Novo serviço"}</h2>

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
            <label htmlFor="nome">Nome</label>
            <input
              ref={campoNome}
              id="nome"
              name="nome"
              type="text"
              maxLength={100}
              defaultValue={editando?.nome ?? ""}
              required
              disabled={salvando}
            />
          </div>

          <div className="campo">
            <label htmlFor="descricao">Descrição</label>
            <textarea
              id="descricao"
              name="descricao"
              maxLength={500}
              defaultValue={editando?.descricao ?? ""}
              disabled={salvando}
            />
            <small>Opcional, até 500 caracteres.</small>
          </div>

          <div className="admin__linha">
            <div className="campo">
              <label htmlFor="preco">Preço (R$)</label>
              <input
                id="preco"
                name="preco"
                type="number"
                inputMode="decimal"
                step="0.01"
                min="0.01"
                max="100000"
                defaultValue={editando?.preco}
                required
                disabled={salvando}
              />
            </div>

            <div className="campo">
              <label htmlFor="duracaoMinutos">Duração (minutos)</label>
              <input
                id="duracaoMinutos"
                name="duracaoMinutos"
                type="number"
                inputMode="numeric"
                step="1"
                min="5"
                max="1440"
                defaultValue={editando?.duracaoMinutos}
                required
                disabled={salvando}
              />
            </div>
          </div>

          {editando && (
            <label className="mostrar-senha">
              <input
                type="checkbox"
                name="ativo"
                defaultChecked={editando.ativo}
                disabled={salvando}
              />
              Serviço ativo (aparece para os clientes)
            </label>
          )}

          <div className="admin__acoes-form">
            <button type="submit" className="botao" disabled={salvando}>
              {salvando ? "Salvando…" : editando ? "Salvar alterações" : "Criar serviço"}
            </button>
            <button
              type="button"
              className="botao botao--secundario"
              onClick={fecharForm}
              disabled={salvando}
            >
              Cancelar
            </button>
          </div>
        </form>
      )}

      {/* ---------- lista ---------- */}
      {carregando && (
        <>
          <p className="sr-only" role="status">
            Carregando serviços…
          </p>
          <ul className="admin__itens" aria-hidden="true">
            {Array.from({ length: 4 }, (_, i) => (
              <li key={i} className="cartao admin-item">
                <span className="esqueleto adm-esq adm-esq--titulo" />
                <span className="esqueleto adm-esq" />
                <span className="esqueleto adm-esq adm-esq--curta" />
              </li>
            ))}
          </ul>
        </>
      )}

      {!carregando && erro && (
        <div className="alerta alerta--erro" role="alert">
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
        <div className="cartao admin__vazio">
          <p>
            {pagina > 1
              ? "Essa página não tem serviços."
              : "Nenhum serviço cadastrado ainda. Crie o primeiro."}
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
          <p className="admin__total" role="status">
            {dados.total} {dados.total === 1 ? "serviço" : "serviços"}
          </p>

          <ul className="admin__itens">
            {itens.map((s) => (
              <li key={s.id} className={`cartao admin-item${s.ativo ? "" : " admin-item--inativo"}`}>
                <div className="admin-item__topo">
                  <h3>{s.nome}</h3>
                  <span className={`etiqueta ${s.ativo ? "etiqueta--marcado" : "etiqueta--inativo"}`}>
                    {s.ativo ? "Ativo" : "Inativo"}
                  </span>
                </div>
                {s.descricao && <p className="admin-item__descricao">{s.descricao}</p>}
                <p className="admin-item__meta">
                  <strong>{formatarPreco(s.preco)}</strong> · {formatarDuracao(s.duracaoMinutos)}
                </p>
                <div className="admin-item__acoes">
                  <button
                    type="button"
                    className="botao botao--secundario botao--pequeno"
                    aria-label={`Editar ${s.nome}`}
                    onClick={() => abrirForm(s)}
                  >
                    Editar
                  </button>
                  <button
                    type="button"
                    className="botao botao--perigo botao--pequeno"
                    aria-label={`Excluir ${s.nome}`}
                    onClick={() => setParaExcluir(s)}
                  >
                    Excluir
                  </button>
                </div>
              </li>
            ))}
          </ul>

          {dados.totalPaginas > 1 && (
            <nav className="admin__paginacao" aria-label="Paginação">
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

      <DialogoConfirmar
        aberto={paraExcluir !== null}
        titulo="Excluir este serviço?"
        texto={
          paraExcluir
            ? `"${paraExcluir.nome}" será excluído. Se já tiver agendamentos, a API não deixa excluir.`
            : ""
        }
        rotuloSeguro="Manter serviço"
        rotuloConfirmar="Excluir serviço"
        perigo
        ocupado={excluindo}
        aoConfirmar={confirmarExclusao}
        aoFechar={() => setParaExcluir(null)}
      />
    </section>
  );
}
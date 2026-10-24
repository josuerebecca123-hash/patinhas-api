// src/paginas/Vitrine.tsx
// Página pública: lista os serviços, com busca e paginação guardadas na URL
// (/?busca=banho&pagina=2), então dá para recarregar, voltar e compartilhar o link.

import { useEffect, useState, type FormEvent } from "react";
import { Link, useSearchParams } from "react-router";
import { api, foiCancelado, mensagemDeErro, montarQuery, type Pagina, type Servico } from "../api";
import { formatarDuracao, formatarPreco } from "../formato";
import { usarTitulo } from "../usarTitulo";
import "./Vitrine.css";

// Se o swagger usar outros nomes de parâmetro (por exemplo "q" ou "tamanhoPagina"),
// ajuste só aqui e na chamada abaixo.
const CAMINHO = "/api/servicos";
const TAMANHO = 6;

interface Estado {
  chave: string; // a consulta a que este resultado pertence
  dados?: Pagina<Servico>;
  erro?: string;
}

function lerPagina(valor: string | null): number {
  const n = Number(valor);
  return Number.isInteger(n) && n >= 1 ? n : 1;
}

export default function Vitrine() {
  usarTitulo("Serviços");

  const [params, setParams] = useSearchParams();
  const busca = (params.get("busca") ?? "").trim();
  const pagina = lerPagina(params.get("pagina"));

  const [estado, setEstado] = useState<Estado | null>(null);
  const [tentativa, setTentativa] = useState(0);
  const [lento, setLento] = useState(false);

  // Enquanto o resultado guardado não for o da consulta atual, está carregando.
  const chave = `${busca}|${pagina}|${tentativa}`;
  const carregando = estado?.chave !== chave;

  useEffect(() => {
    const controle = new AbortController();
    api<Pagina<Servico>>(CAMINHO + montarQuery({ busca, pagina, tamanho: TAMANHO }), {
      signal: controle.signal,
    })
      .then((dados) => setEstado({ chave, dados }))
      .catch((e: unknown) => {
        if (foiCancelado(e)) return;
        setEstado({ chave, erro: mensagemDeErro(e) });
      });
    return () => controle.abort();
  }, [busca, pagina, chave]);

  // A API grátis pode levar cerca de um minuto para acordar: depois de 4 s avisamos.
  useEffect(() => {
    if (!carregando) return;
    const espera = setTimeout(() => setLento(true), 4000);
    return () => {
      clearTimeout(espera);
      setLento(false);
    };
  }, [carregando]);

  function irPara(novaBusca: string, novaPagina: number) {
    const query = montarQuery({ busca: novaBusca, pagina: novaPagina > 1 ? novaPagina : null });
    setParams(query.slice(1));
  }

  function aoBuscar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const valor = String(new FormData(e.currentTarget).get("busca") ?? "").trim();
    irPara(valor, 1);
  }

  function mudarPagina(nova: number) {
    irPara(busca, nova);
    document.getElementById("servicos")?.scrollIntoView();
  }

  const dados = estado?.chave === chave ? estado.dados : undefined;
  const erro = estado?.chave === chave ? estado.erro : undefined;

  return (
    <>
      <section className="abertura" aria-labelledby="titulo-abertura">
        <div className="abertura__texto">
          <h1 id="titulo-abertura">Banho, tosa e cuidado para o seu pet</h1>
          <p>Escolha o serviço, marque o horário e deixe o resto com a gente.</p>
          <a className="botao botao--destaque" href="#servicos">
            Ver serviços
          </a>
        </div>
        <svg className="abertura__patinha" viewBox="0 0 64 64" aria-hidden="true" focusable="false">
          <PatinhaDesenho />
        </svg>
        <svg className="simbolo simbolo--coracao" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
          <path fill="currentColor" d={CAMINHO_CORACAO} />
        </svg>
        <svg className="simbolo simbolo--osso" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
          <g fill="currentColor">
            <rect x="5" y="9.5" width="14" height="5" rx="2.5" />
            <circle cx="5" cy="8.5" r="2.7" />
            <circle cx="5" cy="15.5" r="2.7" />
            <circle cx="19" cy="8.5" r="2.7" />
            <circle cx="19" cy="15.5" r="2.7" />
          </g>
        </svg>
        <svg className="simbolo simbolo--bolhas" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
          <g fill="currentColor">
            <circle cx="9" cy="14" r="5" />
            <circle cx="17" cy="8" r="3.5" />
            <circle cx="16.5" cy="17.5" r="2.5" />
          </g>
        </svg>
      </section>

      <section id="servicos" className="vitrine" aria-labelledby="titulo-servicos">
        <h2 id="titulo-servicos">Nossos serviços</h2>

        <form className="busca" role="search" onSubmit={aoBuscar}>
          <div className="campo">
            <label htmlFor="busca">Buscar serviço</label>
            <input
              key={busca}
              id="busca"
              name="busca"
              type="search"
              defaultValue={busca}
              placeholder="Ex.: banho, tosa, consulta"
              autoComplete="off"
            />
          </div>
          <div className="busca__acoes">
            <button type="submit" className="botao">
              Buscar
            </button>
            {busca && (
              <button type="button" className="botao botao--secundario" onClick={() => irPara("", 1)}>
                Limpar
              </button>
            )}
          </div>
        </form>

        {carregando && (
          <>
            <p className="sr-only" role="status">
              Carregando serviços…
            </p>
            {lento && (
              <p className="alerta alerta--info vitrine__aviso">
                A API está acordando e pode levar até um minuto. Já já os serviços aparecem.
              </p>
            )}
            <EsqueletoCards />
          </>
        )}

        {!carregando && erro && (
          <div className="alerta alerta--erro vitrine__erro" role="alert">
            <p>{erro}</p>
            <button type="button" className="botao botao--secundario botao--pequeno" onClick={() => setTentativa((t) => t + 1)}>
              Tentar de novo
            </button>
          </div>
        )}

        {!carregando && dados && dados.itens.length === 0 && (
          <div className="cartao vazio">
            <p>
              {busca
                ? `Nenhum serviço encontrado para “${busca}”.`
                : pagina > 1
                  ? "Essa página não tem serviços."
                  : "Ainda não há serviços cadastrados."}
            </p>
            {(busca || pagina > 1) && (
              <button type="button" className="botao" onClick={() => irPara("", 1)}>
                Ver todos os serviços
              </button>
            )}
          </div>
        )}

        {!carregando && dados && dados.itens.length > 0 && (
          <>
            <p className="vitrine__contagem" role="status">
              {dados.total === 1 ? "1 serviço" : `${dados.total} serviços`}
              {busca ? ` para “${busca}”` : ""}
            </p>
            <ul className="vitrine__grade">
              {dados.itens.map((servico) => (
                <CartaoServico key={servico.id} servico={servico} />
              ))}
            </ul>
            {dados.totalPaginas > 1 && (
              <Paginacao pagina={dados.pagina} totalPaginas={dados.totalPaginas} aoMudar={mudarPagina} />
            )}
          </>
        )}
      </section>
    </>
  );
}

/* ---------------- peças da página ---------------- */

function CartaoServico({ servico }: { servico: Servico }) {
  return (
    <li className="servico">
      <article className="cartao servico__corpo">
        <span className="servico__icone" aria-hidden="true">
          <IconeServico nome={servico.nome} />
        </span>
        <h3 className="servico__nome">
          <Link to={`/servicos/${servico.id}`}>{servico.nome}</Link>
        </h3>
        {servico.descricao && <p className="servico__descricao">{servico.descricao}</p>}
        <p className="servico__rodape">
          <span className="servico__preco">{formatarPreco(servico.preco)}</span>
          <span className="servico__duracao">{formatarDuracao(servico.duracaoMinutos)}</span>
        </p>
      </article>
    </li>
  );
}

function EsqueletoCards() {
  return (
    <ul className="vitrine__grade" aria-hidden="true">
      {Array.from({ length: TAMANHO }, (_, i) => (
        <li key={i} className="cartao servico--esqueleto">
          <span className="esqueleto esq--icone" />
          <span className="esqueleto esq--titulo" />
          <span className="esqueleto esq--linha" />
          <span className="esqueleto esq--linha esq--curta" />
          <span className="esqueleto esq--preco" />
        </li>
      ))}
    </ul>
  );
}

function Paginacao(props: { pagina: number; totalPaginas: number; aoMudar: (pagina: number) => void }) {
  const { pagina, totalPaginas, aoMudar } = props;
  return (
    <nav className="paginacao" aria-label="Paginação">
      <button
        type="button"
        className="botao botao--secundario botao--pequeno"
        disabled={pagina <= 1}
        onClick={() => aoMudar(pagina - 1)}
      >
        Anterior
      </button>
      <span>
        Página {pagina} de {totalPaginas}
      </span>
      <button
        type="button"
        className="botao botao--secundario botao--pequeno"
        disabled={pagina >= totalPaginas}
        onClick={() => aoMudar(pagina + 1)}
      >
        Próxima
      </button>
    </nav>
  );
}

const CAMINHO_CORACAO =
  "M12 21s-7.5-4.6-9.5-9.2C1.2 8.5 3.2 5 6.6 5c2 0 3.6 1.1 5.4 3 1.8-1.9 3.4-3 5.4-3 3.4 0 5.4 3.5 4.1 6.8C19.5 16.4 12 21 12 21Z";

type TipoIcone = "banho" | "tosa" | "saude" | "pet";

// Escolhe o símbolo pelo nome do serviço (sem depender de campo novo na API).
function tipoDoServico(nome: string | null): TipoIcone {
  const texto = (nome ?? "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase();
  if (/banho|hidrata|spa/.test(texto)) return "banho";
  if (/tosa|corte|unha/.test(texto)) return "tosa";
  if (/consulta|vet|vacina|exame|clinic/.test(texto)) return "saude";
  return "pet";
}

function IconeServico({ nome }: { nome: string | null }) {
  const tipo = tipoDoServico(nome);
  return (
    <svg viewBox="0 0 24 24" focusable="false">
      {tipo === "banho" && (
        <g fill="currentColor">
          <circle cx="9" cy="14" r="5" />
          <circle cx="17" cy="8" r="3.5" />
          <circle cx="16.5" cy="17.5" r="2.5" />
        </g>
      )}
      {tipo === "tosa" && (
        <g fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
          <circle cx="6" cy="6" r="3" />
          <circle cx="6" cy="18" r="3" />
          <path d="M8.2 8.2 20 20M8.2 15.8 20 4" />
        </g>
      )}
      {tipo === "saude" && <path fill="currentColor" d={CAMINHO_CORACAO} />}
      {tipo === "pet" && (
        <g fill="currentColor">
          <ellipse cx="12" cy="16" rx="5" ry="4" />
          <ellipse cx="5.5" cy="11" rx="2" ry="2.8" transform="rotate(-20 5.5 11)" />
          <ellipse cx="9.5" cy="6.5" rx="2" ry="2.8" />
          <ellipse cx="14.5" cy="6.5" rx="2" ry="2.8" />
          <ellipse cx="18.5" cy="11" rx="2" ry="2.8" transform="rotate(20 18.5 11)" />
        </g>
      )}
    </svg>
  );
}

// Patinha grande da faixa de abertura (marca d'água).
function PatinhaDesenho() {
  return (
    <g fill="currentColor">
      <ellipse cx="32" cy="42" rx="14" ry="11" />
      <ellipse cx="14" cy="26" rx="6" ry="8" transform="rotate(-18 14 26)" />
      <ellipse cx="25" cy="14" rx="6" ry="8.5" />
      <ellipse cx="39" cy="14" rx="6" ry="8.5" />
      <ellipse cx="50" cy="26" rx="6" ry="8" transform="rotate(18 50 26)" />
    </g>
  );
}
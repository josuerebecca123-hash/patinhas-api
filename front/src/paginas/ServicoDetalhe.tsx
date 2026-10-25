// src/paginas/ServicoDetalhe.tsx
// Detalhe público de um serviço (/servicos/:id).
// Id que não existe (404 da API) ou que nem é número vira a tela "não encontrado".

import { useEffect, useState } from "react";
import { Link, useParams } from "react-router";
import { api, ApiError, foiCancelado, mensagemDeErro, type Servico } from "../api";
import { formatarDuracao, formatarPreco } from "../formato";
import { usarTitulo } from "../usarTitulo";
import "./Detalhe.css";

interface Estado {
  chave: string;
  servico?: Servico;
  naoEncontrado?: boolean;
  erro?: string;
}

export default function ServicoDetalhe() {
  const { id = "" } = useParams();
  const idNumero = Number(id);
  const idValido = Number.isInteger(idNumero) && idNumero >= 1;

  const [estado, setEstado] = useState<Estado | null>(null);
  const [tentativa, setTentativa] = useState(0);

  const chave = `${id}|${tentativa}`;
  const carregando = idValido && estado?.chave !== chave;

  useEffect(() => {
    if (!idValido) return;
    const controle = new AbortController();
    api<Servico>(`/api/servicos/${idNumero}`, { signal: controle.signal })
      .then((servico) => setEstado({ chave, servico }))
      .catch((e: unknown) => {
        if (foiCancelado(e)) return;
        if (e instanceof ApiError && e.status === 404) setEstado({ chave, naoEncontrado: true });
        else setEstado({ chave, erro: mensagemDeErro(e) });
      });
    return () => controle.abort();
  }, [idValido, idNumero, chave]);

  const atual = estado?.chave === chave ? estado : undefined;
  const naoEncontrado = !idValido || atual?.naoEncontrado === true;
  const servico = atual?.servico;

  usarTitulo(naoEncontrado ? "Serviço não encontrado" : (servico?.nome ?? "Serviço"));

  if (naoEncontrado) {
    return (
      <section className="cartao detalhe detalhe--vazio" aria-labelledby="titulo-nao-encontrado">
        <h1 id="titulo-nao-encontrado">Serviço não encontrado</h1>
        <p>Esse serviço não existe ou foi removido.</p>
        <div className="detalhe__acoes">
          <Link className="botao" to="/">
            Ver todos os serviços
          </Link>
        </div>
      </section>
    );
  }

  if (carregando) {
    return (
      <section className="cartao detalhe detalhe--esqueleto" aria-hidden="true">
        <span className="esqueleto esq--titulo" />
        <span className="esqueleto esq--linha" />
        <span className="esqueleto esq--linha esq--curta" />
        <span className="esqueleto esq--preco" />
        <p className="sr-only" role="status">
          Carregando serviço…
        </p>
      </section>
    );
  }

  if (atual?.erro) {
    return (
      <section className="detalhe">
        <div className="alerta alerta--erro detalhe__erro" role="alert">
          <p>{atual.erro}</p>
          <button
            type="button"
            className="botao botao--secundario botao--pequeno"
            onClick={() => setTentativa((t) => t + 1)}
          >
            Tentar de novo
          </button>
        </div>
        <Link className="detalhe__voltar" to="/">
          Voltar para os serviços
        </Link>
      </section>
    );
  }

  if (!servico) return null;

  return (
    <article className="cartao detalhe" aria-labelledby="titulo-servico">
      <Link className="detalhe__voltar" to="/">
        Voltar para os serviços
      </Link>

      <div className="detalhe__topo">
        <h1 id="titulo-servico">{servico.nome}</h1>
        {!servico.ativo && <span className="etiqueta etiqueta--cancelado">Inativo</span>}
      </div>

      <p className="detalhe__descricao">{servico.descricao || "Este serviço ainda não tem descrição."}</p>

      <dl className="detalhe__dados">
        <div>
          <dt>Preço</dt>
          <dd className="detalhe__preco">{formatarPreco(servico.preco)}</dd>
        </div>
        <div>
          <dt>Duração</dt>
          <dd>{formatarDuracao(servico.duracaoMinutos)}</dd>
        </div>
      </dl>

      {servico.ativo && (
        <div className="detalhe__acoes">
          <Link className="botao botao--destaque" to={`/agendamentos?servico=${servico.id}`}>
            Agendar este serviço
          </Link>
        </div>
      )}
    </article>
  );
}
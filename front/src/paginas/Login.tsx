// src/paginas/Login.tsx
// Entrar na conta. Mostra o aviso "sua sessão expirou" (guardado pela sessão)
// e os erros da API (401 com "detail", 400 com a lista de "errors").

import { useState, type FormEvent } from "react";
import { Link, Navigate, useLocation } from "react-router";
import { ApiError, mensagemDeErro } from "../api";
import { useSessao } from "../sessao";
import { usarTitulo } from "../usarTitulo";
import "./Autenticacao.css";

// Para onde ir depois de entrar, quando a pessoa não veio de uma página protegida.
// Troque por "/agendamentos" quando essa página existir.
const DESTINO_PADRAO = "/";

export default function Login() {
  usarTitulo("Entrar");

  const { sessao, aviso, limparAviso, entrar } = useSessao();
  const location = useLocation();

  // A rota protegida (próximos passos) manda a página de origem em state.de.
  const de = (location.state as { de?: unknown } | null)?.de;
  const destino = typeof de === "string" && de.startsWith("/") ? de : DESTINO_PADRAO;

  const [erros, setErros] = useState<string[]>([]);
  const [enviando, setEnviando] = useState(false);
  const [mostrarSenha, setMostrarSenha] = useState(false);

  // Quem já está logado não precisa ver o formulário.
  if (sessao) return <Navigate to={destino} replace />;

  async function aoEnviar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (enviando) return;

    const dados = new FormData(e.currentTarget);
    const email = String(dados.get("email") ?? "").trim();
    const senha = String(dados.get("senha") ?? "");

    limparAviso();
    setErros([]);
    setEnviando(true);
    try {
      await entrar({ email, senha });
      // Com a sessão criada, o <Navigate> acima leva para o destino.
    } catch (erro) {
      setErros(erro instanceof ApiError ? erro.mensagens : [mensagemDeErro(erro)]);
    } finally {
      setEnviando(false);
    }
  }

  return (
    <section className="autenticacao" aria-labelledby="titulo-login">
      <h1 id="titulo-login">Entrar</h1>
      <p className="autenticacao__intro">Entre para ver e marcar os horários do seu pet.</p>

      <form className="cartao formulario" onSubmit={aoEnviar} noValidate={false}>
        {aviso && (
          <p className="alerta alerta--info" role="status">
            {aviso}
          </p>
        )}

        {erros.length > 0 && (
          <div className="alerta alerta--erro" role="alert">
            {erros.length === 1 ? (
              <p>{erros[0]}</p>
            ) : (
              <ul>
                {erros.map((mensagem, i) => (
                  <li key={i}>{mensagem}</li>
                ))}
              </ul>
            )}
          </div>
        )}

        <div className="campo">
          <label htmlFor="email">E-mail</label>
          <input
            id="email"
            name="email"
            type="email"
            autoComplete="username"
            required
            disabled={enviando}
          />
        </div>

        <div className="campo">
          <label htmlFor="senha">Senha</label>
          <input
            id="senha"
            name="senha"
            type={mostrarSenha ? "text" : "password"}
            autoComplete="current-password"
            required
            disabled={enviando}
          />
        </div>

        <label className="mostrar-senha">
          <input
            type="checkbox"
            checked={mostrarSenha}
            onChange={(e) => setMostrarSenha(e.target.checked)}
          />
          Mostrar senha
        </label>

        <button type="submit" className="botao" disabled={enviando}>
          {enviando ? "Entrando…" : "Entrar"}
        </button>
      </form>

      <p className="autenticacao__rodape">
        Ainda não tem conta? <Link to="/cadastro">Criar conta</Link>
      </p>
    </section>
  );
}
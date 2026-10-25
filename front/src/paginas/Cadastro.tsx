// src/paginas/Cadastro.tsx
// Criar conta (POST /api/auth/registro). A conta nova é sempre de cliente.
// Erros da API na tela: 400 (lista de validação) e 409 (e-mail já usado).
// Depois de criar, entra automaticamente e vai para a agenda.

import { useState, type FormEvent } from "react";
import { Link, Navigate } from "react-router";
import { api, ApiError, mensagemDeErro, type RegistroEntrada, type Usuario } from "../api";
import { useSessao } from "../sessao";
import { usarTitulo } from "../usarTitulo";
import "./Autenticacao.css";

const DESTINO = "/agendamentos";

interface Forca {
  nivel: 0 | 1 | 2 | 3 | 4;
  rotulo: string;
}

// Medidor simples: só orienta, quem manda nas regras é a API (8 a 72 caracteres).
function forcaDaSenha(senha: string): Forca {
  if (!senha) return { nivel: 0, rotulo: "" };
  if (senha.length < 8) return { nivel: 1, rotulo: "muito curta" };

  const tipos = [/[a-z]/, /[A-Z]/, /\d/, /[^A-Za-z0-9]/].filter((regra) => regra.test(senha)).length;
  const pontos = tipos + (senha.length >= 12 ? 1 : 0);

  if (pontos <= 1) return { nivel: 2, rotulo: "fraca" };
  if (pontos <= 3) return { nivel: 3, rotulo: "média" };
  return { nivel: 4, rotulo: "forte" };
}

export default function Cadastro() {
  usarTitulo("Criar conta");

  const { sessao, entrar } = useSessao();

  const [senha, setSenha] = useState("");
  const [repeticao, setRepeticao] = useState("");
  const [mostrarSenha, setMostrarSenha] = useState(false);
  const [erros, setErros] = useState<string[]>([]);
  const [enviando, setEnviando] = useState(false);

  // Quem já está logado não precisa criar conta.
  if (sessao) return <Navigate to={DESTINO} replace />;

  const forca = forcaDaSenha(senha);
  const diferentes = repeticao !== "" && senha !== repeticao;

  async function aoEnviar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (enviando) return;

    const email = String(new FormData(e.currentTarget).get("email") ?? "").trim();

    if (senha !== repeticao) {
      setErros(["As senhas não são iguais."]);
      return;
    }

    const corpo: RegistroEntrada = { email, senha };

    setErros([]);
    setEnviando(true);
    try {
      await api<Usuario>("/api/auth/registro", { metodo: "POST", corpo });
    } catch (erro) {
      setErros(erro instanceof ApiError ? erro.mensagens : [mensagemDeErro(erro)]);
      setEnviando(false);
      return;
    }

    // Conta criada: entra direto. Com a sessão pronta, o <Navigate> acima leva à agenda.
    try {
      await entrar({ email, senha });
    } catch (erro) {
      setErros([
        `Conta criada, mas não foi possível entrar automaticamente. ${mensagemDeErro(erro)} Tente pela página Entrar.`,
      ]);
    } finally {
      setEnviando(false);
    }
  }

  const tipoSenha = mostrarSenha ? "text" : "password";

  return (
    <section className="autenticacao" aria-labelledby="titulo-cadastro">
      <h1 id="titulo-cadastro">Criar conta</h1>
      <p className="autenticacao__intro">Crie sua conta para marcar horários para o seu pet.</p>

      <form className="cartao formulario" onSubmit={aoEnviar}>
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
            maxLength={200}
            autoComplete="username"
            required
            disabled={enviando}
          />
        </div>

        <div className="campo">
          <label htmlFor="senha">Senha</label>
          <input
            id="senha"
            type={tipoSenha}
            value={senha}
            onChange={(e) => setSenha(e.target.value)}
            minLength={8}
            maxLength={72}
            autoComplete="new-password"
            aria-describedby="ajuda-senha"
            required
            disabled={enviando}
          />
          <small id="ajuda-senha">De 8 a 72 caracteres.</small>
          <div className="forca" data-nivel={forca.nivel} aria-hidden="true">
            <span className="forca__barra" />
            <span className="forca__barra" />
            <span className="forca__barra" />
            <span className="forca__barra" />
          </div>
          <small className="forca__rotulo" aria-live="polite">
            {forca.rotulo ? `Força da senha: ${forca.rotulo}` : ""}
          </small>
        </div>

        <div className="campo">
          <label htmlFor="repeticao">Repita a senha</label>
          <input
            id="repeticao"
            type={tipoSenha}
            value={repeticao}
            onChange={(e) => setRepeticao(e.target.value)}
            maxLength={72}
            autoComplete="new-password"
            aria-invalid={diferentes}
            aria-describedby={diferentes ? "erro-repeticao" : undefined}
            required
            disabled={enviando}
          />
          {diferentes && (
            <small id="erro-repeticao" className="campo__erro">
              As senhas não são iguais.
            </small>
          )}
        </div>

        <label className="mostrar-senha">
          <input
            type="checkbox"
            checked={mostrarSenha}
            onChange={(e) => setMostrarSenha(e.target.checked)}
          />
          Mostrar senhas
        </label>

        <button type="submit" className="botao" disabled={enviando}>
          {enviando ? "Criando conta…" : "Criar conta"}
        </button>
      </form>

      <p className="autenticacao__rodape">
        Já tem conta? <Link to="/login">Entrar</Link>
      </p>
    </section>
  );
}
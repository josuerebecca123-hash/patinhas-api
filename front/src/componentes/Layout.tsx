// src/componentes/Layout.tsx
// Moldura de todas as páginas: link "pular", cabeçalho, conteúdo e rodapé.
// Usado como "rota de layout": as páginas aparecem no lugar do <Outlet />.

import { Link, NavLink, Outlet, useNavigate } from "react-router";
import { useSessao } from "../sessao";

function Logo() {
  // Patinha simples; usa currentColor, então a cor vem do CSS (tema claro/escuro).
  return (
    <svg
      className="logo"
      viewBox="0 0 64 64"
      width="32"
      height="32"
      aria-hidden="true"
      focusable="false"
    >
      <ellipse cx="32" cy="43" rx="15" ry="12" fill="currentColor" />
      <ellipse cx="13" cy="30" rx="6" ry="8" fill="currentColor" transform="rotate(-20 13 30)" />
      <ellipse cx="25" cy="17" rx="6" ry="8.5" fill="currentColor" />
      <ellipse cx="39" cy="17" rx="6" ry="8.5" fill="currentColor" />
      <ellipse cx="51" cy="30" rx="6" ry="8" fill="currentColor" transform="rotate(20 51 30)" />
    </svg>
  );
}

export default function Layout() {
  const { sessao, ehAdmin, sair } = useSessao();
  const navegar = useNavigate();

  function aoSair() {
    sair();
    navegar("/");
  }

  return (
    <>
      <a className="pular" href="#conteudo">
        Pular para o conteúdo
      </a>

      <header className="cabecalho">
        <Link to="/" className="marca" aria-label="Patinhas, página inicial">
          <Logo />
          <span>Patinhas</span>
        </Link>

        <nav className="menu" aria-label="Principal">
          {sessao ? (
            <>
              <NavLink to="/agendamentos">Agenda</NavLink>
              {ehAdmin && <NavLink to="/admin/servicos">Admin</NavLink>}
              <button type="button" className="menu__sair" onClick={aoSair}>
                Sair
              </button>
            </>
          ) : (
            <NavLink to="/login">Entrar</NavLink>
          )}
          {/* O botão de modo claro/escuro entra aqui, na etapa de acabamento. */}
        </nav>
      </header>

      <main id="conteudo" className="principal" tabIndex={-1}>
        <Outlet />
      </main>

      <footer className="rodape">
        <p>
          Projeto escolar — empresa fictícia, não é um serviço real. Não use seu e-mail nem sua senha
          de verdade.
        </p>
      </footer>
    </>
  );
}
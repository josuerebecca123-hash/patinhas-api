// src/componentes/RotaProtegida.tsx
// Envolve rotas que exigem login. Sem sessão, manda para /login e guarda a página
// de origem em state.de (o Login volta para lá). Com papel="admin", quem não é
// admin vê um aviso em vez da página.

import { Link, Navigate, Outlet, useLocation } from "react-router";
import { useSessao } from "../sessao";

export default function RotaProtegida({ papel }: { papel?: string }) {
  const { sessao } = useSessao();
  const location = useLocation();

  if (!sessao) {
    return <Navigate to="/login" replace state={{ de: location.pathname + location.search }} />;
  }

  if (papel && sessao.papel.toLowerCase() !== papel.toLowerCase()) {
    return (
      <section className="cartao" role="alert">
        <h1>Sem permissão</h1>
        <p>Esta área é só para administradores.</p>
        <Link className="botao" to="/">
          Voltar para os serviços
        </Link>
      </section>
    );
  }

  return <Outlet />;
}
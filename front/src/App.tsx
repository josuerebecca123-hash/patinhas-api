// src/App.tsx
// Mapa de rotas do app. As páginas reais entram aqui uma a uma.

import { Route, Routes } from "react-router";
import Layout from "./componentes/Layout";
import RotaProtegida from "./componentes/RotaProtegida";
import { useSessao } from "./sessao";
import Agendamentos from "./paginas/Agendamentos";
import Login from "./paginas/Login";
import Vitrine from "./paginas/Vitrine";

// PROVISÓRIO: só a rota "*" ainda usa isto. Será trocada pela página
// "não encontrada" de verdade.
function Provisoria({ titulo }: { titulo: string }) {
  const { sessao } = useSessao();
  return (
    <section>
      <h1>{titulo}</h1>
      <p>{sessao ? `Logado como ${sessao.email} (${sessao.papel})` : "Sem sessão"}</p>
    </section>
  );
}

export default function App() {
  return (
    <Routes>
      {/* Todas as páginas ficam dentro do Layout (cabeçalho + rodapé) */}
      <Route element={<Layout />}>
        <Route path="/" element={<Vitrine />} />
        <Route path="/login" element={<Login />} />

        {/* Só com sessão: sem login, manda para /login e volta depois */}
        <Route element={<RotaProtegida />}>
          <Route path="/agendamentos" element={<Agendamentos />} />
        </Route>

        <Route path="*" element={<Provisoria titulo="Página não encontrada" />} />
      </Route>
    </Routes>
  );
}
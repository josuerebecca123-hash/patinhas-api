// src/App.tsx
// Mapa de rotas do app. As páginas reais entram aqui uma a uma.

import { Route, Routes } from "react-router";
import Layout from "./componentes/Layout";
import { useSessao } from "./sessao";

// PROVISÓRIO: só para testar o router, o layout e a sessão. Será trocado pela
// página pública (Vitrine) e pela página "não encontrada" de verdade.
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
        <Route path="/" element={<Provisoria titulo="Patinhas: vitrine em construção" />} />
        <Route path="*" element={<Provisoria titulo="Página não encontrada" />} />
      </Route>
    </Routes>
  );
}
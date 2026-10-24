// src/App.tsx
// Mapa de rotas do app. As páginas reais entram aqui uma a uma.

import { Route, Routes } from "react-router";
import { useSessao } from "./sessao";

// PROVISÓRIO: só para testar o router e a sessão. Será trocado pela
// página pública (Vitrine) e pela página "não encontrada" de verdade.
function Provisoria({ titulo }: { titulo: string }) {
  const { sessao } = useSessao();
  return (
    <main style={{ padding: "2rem", fontFamily: "sans-serif" }}>
      <h1>{titulo}</h1>
      <p>{sessao ? `Logado como ${sessao.email} (${sessao.papel})` : "Sem sessão"}</p>
    </main>
  );
}

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Provisoria titulo="Patinhas: vitrine em construção" />} />
      <Route path="*" element={<Provisoria titulo="Página não encontrada" />} />
    </Routes>
  );
}
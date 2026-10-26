// src/App.tsx
// Mapa de rotas do app. As páginas novas entram aqui com uma linha <Route>.

import { Route, Routes } from "react-router";
import Layout from "./componentes/Layout";
import RotaProtegida from "./componentes/RotaProtegida";
import AdminServicos from "./paginas/AdminServicos";
import Agendamentos from "./paginas/Agendamentos";
import Cadastro from "./paginas/Cadastro";
import Login from "./paginas/Login";
import NaoEncontrada from "./paginas/NaoEncontrada";
import ServicoDetalhe from "./paginas/ServicoDetalhe";
import Vitrine from "./paginas/Vitrine";

export default function App() {
  return (
    <Routes>
      {/* Todas as páginas ficam dentro do Layout (cabeçalho + rodapé) */}
      <Route element={<Layout />}>
        <Route path="/" element={<Vitrine />} />
        <Route path="/servicos/:id" element={<ServicoDetalhe />} />
        <Route path="/login" element={<Login />} />
        <Route path="/cadastro" element={<Cadastro />} />

        {/* Só com sessão: sem login, manda para /login e volta depois */}
        <Route element={<RotaProtegida />}>
          <Route path="/agendamentos" element={<Agendamentos />} />
          {/* Só admin: quem é cliente vê o aviso "área restrita" dentro da página */}
          <Route path="/admin/servicos" element={<AdminServicos />} />
        </Route>

        <Route path="*" element={<NaoEncontrada />} />
      </Route>
    </Routes>
  );
}
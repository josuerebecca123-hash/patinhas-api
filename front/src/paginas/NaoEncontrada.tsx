// src/paginas/NaoEncontrada.tsx
// Rota "*": endereço que não existe. Não chama a API.

import { Link } from "react-router";
import { usarTitulo } from "../usarTitulo";
import "./NaoEncontrada.css";

export default function NaoEncontrada() {
  usarTitulo("Página não encontrada");

  return (
    <section className="nao-encontrada" aria-labelledby="titulo-404">
      <p className="nao-encontrada__codigo" aria-hidden="true">
        404
      </p>
      <h1 id="titulo-404">Página não encontrada</h1>
      <p>
        O endereço que você abriu não existe aqui. O link pode estar errado ou a página mudou de
        lugar.
      </p>
      <Link className="botao" to="/">
        Voltar para os serviços
      </Link>
    </section>
  );
}
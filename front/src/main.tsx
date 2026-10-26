import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter } from "react-router";
import "./index.css";
import App from "./App.tsx";
import { ProvedorSessao } from "./sessao.tsx";
import { ProvedorToasts } from "./componentes/Toasts.tsx";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <BrowserRouter>
      <ProvedorToasts>
        <ProvedorSessao>
          <App />
        </ProvedorSessao>
      </ProvedorToasts>
    </BrowserRouter>
  </StrictMode>,
);
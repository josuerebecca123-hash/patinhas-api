// src/usarTitulo.ts
// Define o título da aba: usarTitulo("Entrar") -> "Entrar · Patinhas".

import { useEffect } from "react";

export function usarTitulo(titulo: string): void {
  useEffect(() => {
    document.title = `${titulo} · Patinhas`;
  }, [titulo]);
}
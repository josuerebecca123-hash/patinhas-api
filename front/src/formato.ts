// src/formato.ts
// Formatadores de texto para a tela. Nenhuma página monta preço ou duração na mão.

const moeda = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" });

/** 79.9 -> "R$ 79,90" */
export function formatarPreco(valor: number): string {
  return moeda.format(valor);
}

/** 45 -> "45 min", 60 -> "1h", 90 -> "1h 30min" */
export function formatarDuracao(minutos: number): string {
  const horas = Math.floor(minutos / 60);
  const resto = minutos % 60;
  if (horas === 0) return `${resto} min`;
  return resto === 0 ? `${horas}h` : `${horas}h ${resto}min`;
}

/** "2026-10-25T14:30:00-03:00" -> "25/10/2026 14:30" (no fuso do navegador) */
export function formatarDataHora(iso: string): string {
  return new Date(iso).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" });
}

/** só a hora: "14:30" */
export function formatarHora(iso: string): string {
  return new Date(iso).toLocaleTimeString("pt-BR", { timeStyle: "short" });
}
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
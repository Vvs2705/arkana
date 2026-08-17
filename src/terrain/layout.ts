// ============================================================================
// ARKANA v0.1 — layout FIXO da arena 60×60 (raia B — Engenharia de Simulação)
// Determinístico por definição: NENHUM Math.random aqui — só hash por célula.
// Mesmo seed implícito em toda máquina → mesma arena sempre (replays, testes).
// Biomas exigidos (GDD seção 14 + DoD item 4): LAGO no NE, FLORESTA densa no
// SW, manchas de GRAMA ALTA, ROCHAS em aglomerados, CAMPO ABERTO central,
// faixas de DIRT (margens do lago, clareiras, estrada diagonal) e um anel de
// ROCK na borda (paredão natural que fecha a arena).
// ============================================================================
import { TerrainMaterial } from '../core/types';

// ------------------------------------------------------------------ hashes
// sais distintos por camada — evita correlação visual entre features
const SALT_FOREST = 11;
const SALT_PATCH = 23;
const SALT_ROCK = 37;
const SALT_ROCK_SIZE = 41;

/** hash inteiro determinístico por célula (mistura estilo Murmur, sem estado) */
function hashInt(x: number, y: number, salt: number): number {
  let h = Math.imul(x + 1, 73856093) ^ Math.imul(y + 1, 19349663) ^ Math.imul(salt + 1, 83492791);
  h ^= h >>> 13;
  h = Math.imul(h, 0x5bd1e995);
  h ^= h >>> 15;
  return h >>> 0;
}

/** hash normalizado para [0, 1) — pseudo-ruído por célula */
function hash01(x: number, y: number, salt: number): number {
  return hashInt(x, y, salt) / 4294967296;
}

// ------------------------------------------------------------------ geração
/**
 * Gera a matriz de materiais da arena (indexada como grid[y][x]).
 * Posições das features são frações de cols/rows — tunado para 60×60,
 * mas degrada de forma aceitável em outros tamanhos.
 */
export function generateLayout(cols: number, rows: number): TerrainMaterial[][] {
  const M = TerrainMaterial;

  // ---- base: campo de grama
  const grid: TerrainMaterial[][] = [];
  for (let y = 0; y < rows; y++) {
    const row: TerrainMaterial[] = [];
    for (let x = 0; x < cols; x++) row.push(M.GRASS);
    grid.push(row);
  }

  // ---- estrada diagonal de terra (NW → SE, cruza o campo central)
  for (let y = 0; y < rows; y++) {
    for (let x = 0; x < cols; x++) {
      if (Math.abs(x - y) <= 1) grid[y][x] = M.DIRT;
    }
  }

  // ---- LAGO: elipse ~14×10 células no quadrante NE, com margem (praia) de DIRT
  const lakeCx = cols * 0.73;
  const lakeCy = rows * 0.25;
  const lakeRx = cols * 0.115; // ≈ 6.9 células de raio → ~14 de largura
  const lakeRy = rows * 0.082; // ≈ 4.9 células de raio → ~10 de altura
  for (let y = 0; y < rows; y++) {
    for (let x = 0; x < cols; x++) {
      const nx = (x - lakeCx) / lakeRx;
      const ny = (y - lakeCy) / lakeRy;
      if (nx * nx + ny * ny <= 1) {
        grid[y][x] = M.WATER;
      } else {
        const mx = (x - lakeCx) / (lakeRx + 1.7);
        const my = (y - lakeCy) / (lakeRy + 1.6);
        if (mx * mx + my * my <= 1) grid[y][x] = M.DIRT; // margem do lago
      }
    }
  }

  // ---- FLORESTA densa ~18×14 no quadrante SW: TREE ~45% intercalada com GRASS
  const fx0 = Math.round(cols * 0.08);
  const fx1 = Math.round(cols * 0.37);
  const fy0 = Math.round(rows * 0.66);
  const fy1 = Math.round(rows * 0.89);
  for (let y = fy0; y <= fy1; y++) {
    for (let x = fx0; x <= fx1; x++) {
      if (grid[y][x] === M.GRASS && hash01(x, y, SALT_FOREST) < 0.45) grid[y][x] = M.TREE;
    }
  }

  // ---- clareiras de DIRT (respiros táticos; fogo não atravessa terra nua)
  const clearings = [
    { cx: cols * 0.22, cy: rows * 0.78, r: 2.4 }, // clareira dentro da floresta
    { cx: cols * 0.67, cy: rows * 0.62, r: 2.8 }, // clareira a leste do campo
  ];
  for (const c of clearings) {
    const ri = Math.ceil(c.r);
    const ccx = Math.round(c.cx);
    const ccy = Math.round(c.cy);
    for (let dy = -ri; dy <= ri; dy++) {
      for (let dx = -ri; dx <= ri; dx++) {
        if (dx * dx + dy * dy > c.r * c.r) continue;
        const x = ccx + dx;
        const y = ccy + dy;
        if (x <= 0 || y <= 0 || x >= cols - 1 || y >= rows - 1) continue;
        if (grid[y][x] !== M.WATER) grid[y][x] = M.DIRT;
      }
    }
  }

  // ---- manchas de GRAMA ALTA: 4 manchas de ~6×5 células (ocultação — GDD 14)
  const patches = [
    { fx: 0.13, fy: 0.28 }, // NW — emboscada perto da estrada
    { fx: 0.38, fy: 0.30 }, // centro-norte
    { fx: 0.80, fy: 0.70 }, // SE
    { fx: 0.57, fy: 0.82 }, // sul — beira da floresta
  ];
  for (const p of patches) {
    const pcx = Math.round(cols * p.fx);
    const pcy = Math.round(rows * p.fy);
    for (let dy = -2; dy <= 2; dy++) {
      for (let dx = -3; dx <= 2; dx++) { // caixa 6×5
        const x = pcx + dx;
        const y = pcy + dy;
        if (x <= 0 || y <= 0 || x >= cols - 1 || y >= rows - 1) continue;
        // cantos irregulares (determinísticos) — contorno orgânico
        if (Math.abs(dx) >= 2 && Math.abs(dy) === 2 && hash01(x, y, SALT_PATCH) < 0.45) continue;
        if (grid[y][x] === M.GRASS) grid[y][x] = M.TALL_GRASS;
      }
    }
  }

  // ---- ROCHAS espalhadas: aglomerados de 2–5 células (cobertura indestrutível)
  // semente ~1 a cada 149 células de grama elegíveis; longe da estrada, do
  // spawn central e de dentro da floresta (para não selar os corredores)
  const clusterOffsets: ReadonlyArray<readonly [number, number]> = [
    [0, 0], [1, 0], [0, 1], [-1, 0], [1, 1],
  ];
  const centerX = (cols - 1) / 2;
  const centerY = (rows - 1) / 2;
  for (let y = 2; y < rows - 2; y++) {
    for (let x = 2; x < cols - 2; x++) {
      if (grid[y][x] !== M.GRASS) continue;
      if (Math.abs(x - y) <= 2) continue; // não obstruir a estrada
      if (Math.max(Math.abs(x - centerX), Math.abs(y - centerY)) <= 7) continue; // longe do spawn
      if (x >= fx0 && x <= fx1 && y >= fy0 && y <= fy1) continue; // fora da floresta
      if (hashInt(x, y, SALT_ROCK) % 149 !== 0) continue;
      const size = 2 + (hashInt(x, y, SALT_ROCK_SIZE) % 4); // 2..5 células
      for (let i = 0; i < size; i++) {
        const [dx, dy] = clusterOffsets[i];
        const rx = x + dx;
        const ry = y + dy;
        if (rx <= 0 || ry <= 0 || rx >= cols - 1 || ry >= rows - 1) continue;
        if (grid[ry][rx] === M.GRASS) grid[ry][rx] = M.ROCK;
      }
    }
  }

  // ---- CAMPO ABERTO central: células ~28–32 livres de bloqueio
  // (spawn do jogador e do dummy — garantia dura, sobrescreve qualquer feature)
  const scx = Math.round(centerX);
  const scy = Math.round(centerY);
  for (let y = scy - 3; y <= scy + 3; y++) {
    for (let x = scx - 3; x <= scx + 3; x++) {
      if (x <= 0 || y <= 0 || x >= cols - 1 || y >= rows - 1) continue;
      const m = grid[y][x];
      if (m === M.TREE || m === M.ROCK || m === M.WATER) grid[y][x] = M.GRASS;
    }
  }

  // ---- paredão natural: anel de 1 célula de ROCK na borda (por último)
  for (let x = 0; x < cols; x++) {
    grid[0][x] = M.ROCK;
    grid[rows - 1][x] = M.ROCK;
  }
  for (let y = 0; y < rows; y++) {
    grid[y][0] = M.ROCK;
    grid[y][cols - 1] = M.ROCK;
  }

  return grid;
}

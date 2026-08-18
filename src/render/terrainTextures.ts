// ============================================================================
// PRISMA-1 · raia 2 — texturas procedurais do TERRENO em resolução visual 64px
// (o mundo lógico continua TILE=32; a RT do TerrainGrid é 2× e exibida a 0.5).
//
// Conteúdo:
//   - tiles base 64px com 4 VARIANTES por material/estado (anti-xadrez);
//   - peças de AUTOTILING por prioridade (água > areia > terra > grama):
//     bordas onduladas N/S/L/O + cantos, periódicas (emendam entre células);
//   - DECALS esparsos (pedras, flores, rachaduras) determinísticos por célula;
//   - texturas de onda/espuma consumidas pelo TerrainSkin.
//
// Tudo determinístico: hash(cx,cy) — NUNCA Math.random (coerência entre redraws).
// Zero assets externos (regra do projeto). Prefixo de chave: "terrain-".
// ============================================================================
import Phaser from 'phaser';
import { TerrainMaterial, TerrainState } from '../core/types';
import { Settings } from '../core/settings';

/** lado visual da célula em px de textura (lógico continua 32) */
export const VIS = 64;

type G = Phaser.GameObjects.Graphics;
type CellLike = { material: TerrainMaterial; state: TerrainState } | null;
export type CellAt = (cx: number, cy: number) => CellLike;

// ------------------------------------------------------------------ hashes
// mesmo estilo de layout.ts (que é intocável) — sais próprios desta camada
function hashInt(x: number, y: number, salt: number): number {
  let h = Math.imul(x + 1, 73856093) ^ Math.imul(y + 1, 19349663) ^ Math.imul(salt + 1, 83492791);
  h ^= h >>> 13;
  h = Math.imul(h, 0x5bd1e995);
  h ^= h >>> 15;
  return h >>> 0;
}

/** hash normalizado [0,1) — exportado p/ o TerrainSkin (fases de onda) */
export function terrainHash01(x: number, y: number, salt: number): number {
  return hashInt(x, y, salt) / 4294967296;
}

const SALT_VAR = 101;    // variante do tile base
const SALT_EDGE = 131;   // variante da borda (por lado)
const SALT_DECAL = 113;  // sorteio do decal
const SALT_DPOS = 127;   // posição do decal na célula

// ------------------------------------------------------------------ solos
// "solo" p/ autotiling: rocha/árvore/grama alta assentam sobre GRAMA;
// DIRT vizinho de água vira AREIA (praia) — puramente visual e estável,
// porque `material` é imutável (regra 3 do chemistry engine).
const G_GRASS = 0;
const G_DIRT = 1;
const G_SAND = 2;
const G_WATER = 3;
const GROUND_NAME = ['grass', 'dirt', 'sand', 'water'] as const;

function groundOf(cx: number, cy: number, at: CellAt): number {
  const c = at(cx, cy);
  if (!c) return -1; // fora do mapa: nunca gera borda
  switch (c.material) {
    case TerrainMaterial.WATER: return G_WATER;
    case TerrainMaterial.DIRT:
      for (let dy = -1; dy <= 1; dy++) {
        for (let dx = -1; dx <= 1; dx++) {
          if (dx === 0 && dy === 0) continue;
          if (at(cx + dx, cy + dy)?.material === TerrainMaterial.WATER) return G_SAND;
        }
      }
      return G_DIRT;
    default: return G_GRASS; // GRASS, TALL_GRASS, ROCK, TREE
  }
}

// ------------------------------------------------------------------ geração
function gfx(scene: Phaser.Scene): G {
  return scene.make.graphics({ x: 0, y: 0 }, false);
}
function bake(g: G, key: string, w: number, h: number): void {
  g.generateTexture(key, w, h);
  g.destroy();
}
/** jitter determinístico local à textura (seed = variante) */
function j(seed: number, i: number): number {
  return terrainHash01(seed * 31 + i, i * 7 + 3, 977);
}

// ---- tiles base (4 variantes) ---------------------------------------------
function bakeGrass(scene: Phaser.Scene, v: number): void {
  const g = gfx(scene);
  const base = [0x2e5d34, 0x2f6036, 0x2c5931, 0x306239][v];
  g.fillStyle(base);
  g.fillRect(0, 0, VIS, VIS);
  for (let i = 0; i < 26; i++) {
    g.fillStyle(i % 2 === 0 ? 0x3a7042 : 0x27512d, 0.9);
    g.fillRect(j(v, i) * VIS, j(v, i + 40) * VIS, 3, 3);
  }
  g.lineStyle(2, 0x3f7a46, 0.9);
  for (let i = 0; i < 5; i++) {
    const x = 6 + j(v, i + 80) * (VIS - 12);
    const y = 10 + j(v, i + 90) * (VIS - 16);
    g.lineBetween(x, y + 6, x + (j(v, i + 95) - 0.5) * 6, y);
  }
  bake(g, `terrain-grass-${v}`, VIS, VIS);
}

function bakeTallGrass(scene: Phaser.Scene, v: number): void {
  const g = gfx(scene);
  const base = [0x24522c, 0x255630, 0x224e29, 0x275933][v];
  g.fillStyle(base);
  g.fillRect(0, 0, VIS, VIS);
  for (let i = 0; i < 14; i++) {
    const x = 5 + j(v, i) * (VIS - 10);
    const tall = 22 + j(v, i + 20) * 16;
    g.lineStyle(3, i % 3 === 0 ? 0x5c9c55 : 0x4f8f4a, 0.95);
    g.lineBetween(x, VIS - 3, x + (j(v, i + 33) - 0.5) * 12, VIS - 3 - tall);
  }
  g.fillStyle(0x1c421f, 0.5);
  for (let i = 0; i < 8; i++) {
    g.fillRect(j(v, i + 60) * VIS, VIS - 8 + j(v, i + 70) * 6, 4, 2);
  }
  bake(g, `terrain-tall-${v}`, VIS, VIS);
}

function bakeDirt(scene: Phaser.Scene, v: number): void {
  const g = gfx(scene);
  const base = [0x6b4a2b, 0x6e4d2e, 0x674628, 0x715030][v];
  g.fillStyle(base);
  g.fillRect(0, 0, VIS, VIS);
  for (let i = 0; i < 22; i++) {
    g.fillStyle(i % 2 === 0 ? 0x7d5835 : 0x5d3f24, 0.9);
    g.fillRect(j(v, i) * VIS, j(v, i + 40) * VIS, 4, 3);
  }
  g.fillStyle(0x8a7350, 0.8);
  for (let i = 0; i < 3; i++) {
    g.fillEllipse(8 + j(v, i + 80) * (VIS - 16), 8 + j(v, i + 88) * (VIS - 16), 5, 4);
  }
  bake(g, `terrain-dirt-${v}`, VIS, VIS);
}

function bakeSand(scene: Phaser.Scene, v: number): void {
  const g = gfx(scene);
  const base = [0xc4a96b, 0xc9b075, 0xbfa262, 0xcbb27a][v];
  g.fillStyle(base);
  g.fillRect(0, 0, VIS, VIS);
  for (let i = 0; i < 24; i++) {
    g.fillStyle(i % 2 === 0 ? 0xd9c284 : 0xa98f52, 0.85);
    g.fillRect(j(v, i) * VIS, j(v, i + 40) * VIS, 3, 2);
  }
  // marcas de maré sutis
  g.lineStyle(1, 0xb59a58, 0.5);
  const yy = 14 + j(v, 99) * 30;
  g.lineBetween(4, yy, VIS - 4, yy + (j(v, 98) - 0.5) * 8);
  bake(g, `terrain-sand-${v}`, VIS, VIS);
}

function bakeWater(scene: Phaser.Scene, v: number): void {
  const g = gfx(scene);
  const base = [0x1b5e9e, 0x1c62a4, 0x1a5a97, 0x1e65a8][v];
  g.fillStyle(base);
  g.fillRect(0, 0, VIS, VIS);
  g.fillStyle(0x17548e, 0.6);
  g.fillEllipse(16 + j(v, 1) * 30, 16 + j(v, 2) * 30, 26, 16);
  g.lineStyle(2, 0x2f7fc4, 0.5);
  for (let k = 0; k < 2; k++) {
    const y0 = 14 + k * 28 + j(v, k + 10) * 8;
    g.beginPath();
    g.moveTo(4, y0);
    g.lineTo(20, y0 - 3);
    g.lineTo(38, y0 + 2);
    g.lineTo(58, y0 - 2);
    g.strokePath();
  }
  bake(g, `terrain-water-${v}`, VIS, VIS);
}

function bakeBurned(scene: Phaser.Scene, v: number): void {
  const g = gfx(scene);
  g.fillStyle(0x1c1a18);
  g.fillRect(0, 0, VIS, VIS);
  for (let i = 0; i < 12; i++) {
    g.fillStyle(i % 2 === 0 ? 0x33302c : 0x0f0e0d, 0.95);
    g.fillRect(j(v, i) * VIS, j(v, i + 40) * VIS, 6, 4);
  }
  // toco/garrancho carbonizado
  g.fillStyle(0x0b0a09);
  g.fillRect(20 + j(v, 77) * 20, 22 + j(v, 78) * 20, 5, 14);
  // brasas residuais — beleza SEM perder a leitura de "carvão" (GDD 14)
  for (let i = 0; i < 3; i++) {
    const ex = 8 + j(v, i + 60) * (VIS - 16);
    const ey = 8 + j(v, i + 66) * (VIS - 16);
    g.fillStyle(0xffa04a, 0.28);
    g.fillCircle(ex, ey, 4);
    g.fillStyle(0xff5a2a, 0.85);
    g.fillCircle(ex, ey, 1.6);
  }
  bake(g, `terrain-burned-${v}`, VIS, VIS);
}

function bakeFrozen(scene: Phaser.Scene, v: number): void {
  const g = gfx(scene);
  g.fillStyle(0xa8d8f0);
  g.fillRect(0, 0, VIS, VIS);
  // brilho diagonal (sheen)
  g.fillStyle(0xc6ecfb, 0.55);
  g.beginPath();
  const o = j(v, 5) * 20;
  g.moveTo(10 + o, 0);
  g.lineTo(30 + o, 0);
  g.lineTo(6 + o, VIS);
  g.lineTo(-14 + o, VIS);
  g.closePath();
  g.fillPath();
  // sombra de profundidade nas bordas
  g.fillStyle(0x8fc4e0, 0.4);
  g.fillRect(0, VIS - 5, VIS, 5);
  g.fillRect(VIS - 5, 0, 5, VIS);
  // rachaduras finas
  g.lineStyle(1, 0xd8f0fa, 0.95);
  for (let k = 0; k < 2; k++) {
    const x0 = 8 + j(v, k + 20) * 40;
    const y0 = 6 + j(v, k + 30) * 20;
    g.beginPath();
    g.moveTo(x0, y0);
    g.lineTo(x0 + 10, y0 + 16);
    g.lineTo(x0 + 4, y0 + 30);
    g.lineTo(x0 + 14, y0 + 44);
    g.strokePath();
  }
  bake(g, `terrain-frozen-${v}`, VIS, VIS);
}

function bakeMud(scene: Phaser.Scene, v: number): void {
  const g = gfx(scene);
  g.fillStyle(0x3f2c17);
  g.fillRect(0, 0, VIS, VIS);
  for (let i = 0; i < 4; i++) {
    g.fillStyle(0x54401f, 0.95);
    g.fillEllipse(10 + j(v, i) * (VIS - 20), 10 + j(v, i + 8) * (VIS - 20), 18, 10);
  }
  for (let i = 0; i < 3; i++) {
    g.fillStyle(0x2b1d0e, 0.95);
    g.fillEllipse(12 + j(v, i + 16) * (VIS - 24), 12 + j(v, i + 24) * (VIS - 24), 12, 6);
  }
  // reflexo molhado — lama deve parecer ÚMIDA (mais bonita, igualmente legível)
  g.fillStyle(0x6d5426, 0.6);
  g.fillEllipse(16 + j(v, 50) * 32, 16 + j(v, 51) * 32, 8, 3);
  bake(g, `terrain-mud-${v}`, VIS, VIS);
}

function bakeWall(scene: Phaser.Scene, v: number): void {
  const g = gfx(scene);
  g.fillStyle(0x7a6a55);
  g.fillRect(0, 0, VIS, VIS);
  // fileiras de blocos com meia-junta (2 fileiras de 32px)
  const rows = [0, 32];
  rows.forEach((ry, ri) => {
    const off = ri % 2 === 0 ? 0 : 16;
    for (let bx = -1; bx < 3; bx++) {
      const x0 = bx * 32 + off;
      // leve variação de tom por bloco
      g.fillStyle([0x7f6f59, 0x776750, 0x83725c, 0x74644e][(bx + ri + v) & 3], 1);
      g.fillRect(x0 + 2, ry + 2, 28, 28);
      // bisel: luz em cima/esquerda, sombra embaixo/direita
      g.fillStyle(0x8d7c64, 0.8);
      g.fillRect(x0 + 2, ry + 2, 28, 3);
      g.fillRect(x0 + 2, ry + 2, 3, 28);
      g.fillStyle(0x574a3a, 0.8);
      g.fillRect(x0 + 2, ry + 27, 28, 3);
      g.fillRect(x0 + 27, ry + 2, 3, 28);
    }
  });
  // argamassa
  g.lineStyle(3, 0x4e4234, 1);
  g.strokeRect(1, 1, VIS - 2, VIS - 2);
  g.lineBetween(0, 32, VIS, 32);
  bake(g, `terrain-wall-${v}`, VIS, VIS);
}

/** rocha como OVERLAY transparente — assenta sobre o solo autotilado */
function bakeRock(scene: Phaser.Scene, v: number): void {
  const g = gfx(scene);
  const ox = (j(v, 1) - 0.5) * 6;
  const oy = (j(v, 2) - 0.5) * 6;
  g.fillStyle(0x000000, 0.22);
  g.fillEllipse(32 + ox, 42 + oy, 46, 16); // sombra projetada
  g.fillStyle(0x6f7480);
  g.fillCircle(32 + ox, 33 + oy, 21);
  g.fillStyle(0x8a8f9c);
  g.fillCircle(25 + ox, 26 + oy, 11);
  g.fillStyle(0x565a64);
  g.fillCircle(41 + ox, 40 + oy, 9);
  g.lineStyle(1, 0x4a4e57, 0.8);
  g.lineBetween(22 + ox, 36 + oy, 34 + ox, 30 + oy);
  bake(g, `terrain-rock-${v}`, VIS, VIS);
}

/** árvore como OVERLAY transparente — copa com dois tons + sombra */
function bakeTree(scene: Phaser.Scene, v: number): void {
  const g = gfx(scene);
  const ox = (j(v, 3) - 0.5) * 6;
  g.fillStyle(0x000000, 0.28);
  g.fillEllipse(32, 46, 44, 15); // sombra projetada
  g.fillStyle(0x4a3018);
  g.fillRect(28, 30, 8, 20); // tronco
  g.fillStyle(0x1d4023);
  g.fillCircle(32 + ox, 25, 22); // copa base
  g.fillStyle(0x2c5c33);
  g.fillCircle(24 + ox, 19, 11);
  g.fillCircle(41 + ox, 24, 10);
  g.fillStyle(0x3a7a44, 0.85);
  g.fillCircle(27 + ox, 15, 6); // brilho da copa
  bake(g, `terrain-tree-${v}`, VIS, VIS);
}

// ---- bordas de autotiling --------------------------------------------------
// profundidade da língua ondulada; SOMENTE harmônicas inteiras sem fase no
// termo constante → f(0) = f(64) = D exato: as bordas EMENDAM entre células
const EDGE_D = 11;
function edgeDepth(u: number, variant: number): number {
  const t = (2 * Math.PI * u) / VIS;
  return variant === 0
    ? EDGE_D + 3.6 * Math.sin(t) + 2.1 * Math.sin(2 * t)
    : EDGE_D + 2.2 * Math.sin(t) - 3.0 * Math.sin(3 * t);
}

const EDGE_W = 20; // espessura máx. da peça de borda
type Side = 'n' | 's' | 'w' | 'e';

/** (u,v) → coords da textura conforme o lado (v = distância da borda externa) */
function sideXY(side: Side, u: number, v: number): { x: number; y: number } {
  switch (side) {
    case 'n': return { x: u, y: v };
    case 's': return { x: u, y: EDGE_W - v };
    case 'w': return { x: v, y: u };
    case 'e': return { x: EDGE_W - v, y: u };
  }
}

interface EdgeStyle { fill: number; line?: { color: number; width: number; alpha: number }; foam?: boolean }
const EDGE_STYLE: Record<string, EdgeStyle> = {
  dirt: { fill: 0x6b4a2b, line: { color: 0x54401f, width: 2, alpha: 0.45 } },
  sand: { fill: 0xc4a96b, line: { color: 0xa98f52, width: 2, alpha: 0.4 } },
  water: { fill: 0x1b5e9e, foam: true },
};

function bakeEdge(scene: Phaser.Scene, mat: string, side: Side, variant: number): void {
  const g = gfx(scene);
  const st = EDGE_STYLE[mat];
  const horizontal = side === 'n' || side === 's';
  const w = horizontal ? VIS : EDGE_W;
  const h = horizontal ? EDGE_W : VIS;
  // polígono: borda externa reta + limite interno ondulado
  const pts: { x: number; y: number }[] = [sideXY(side, 0, 0), sideXY(side, VIS, 0)];
  for (let u = VIS; u >= 0; u -= 4) pts.push(sideXY(side, u, edgeDepth(u, variant)));
  g.fillStyle(st.fill);
  g.fillPoints(pts, true);
  // linha do limite (espuma na água; contorno sutil em terra/areia)
  const boundary: { x: number; y: number }[] = [];
  for (let u = 0; u <= VIS; u += 4) boundary.push(sideXY(side, u, edgeDepth(u, variant)));
  if (st.foam) {
    g.lineStyle(4, 0x2f7fc4, 0.45); // faixa d'água clara antes da espuma
    g.strokePoints(boundary.map((p) => p), false);
    g.lineStyle(3, 0xeef7ff, 0.85); // espuma
    const inner: { x: number; y: number }[] = [];
    for (let u = 0; u <= VIS; u += 4) inner.push(sideXY(side, u, edgeDepth(u, variant) - 1));
    g.strokePoints(inner, false);
  } else if (st.line) {
    g.lineStyle(st.line.width, st.line.color, st.line.alpha);
    g.strokePoints(boundary, false);
  }
  bake(g, `terrain-edge-${mat}-${side}-${variant}`, w, h);
}

type Corner = 'nw' | 'ne' | 'sw' | 'se';
const CORNER_CENTER: Record<Corner, { x: number; y: number }> = {
  nw: { x: 0, y: 0 }, ne: { x: EDGE_W, y: 0 }, sw: { x: 0, y: EDGE_W }, se: { x: EDGE_W, y: EDGE_W },
};

function bakeCorner(scene: Phaser.Scene, mat: string, corner: Corner): void {
  const g = gfx(scene);
  const st = EDGE_STYLE[mat];
  const c = CORNER_CENTER[corner];
  g.fillStyle(st.fill);
  g.fillCircle(c.x, c.y, 13);
  if (st.foam) {
    g.lineStyle(3, 0xeef7ff, 0.85);
    g.strokeCircle(c.x, c.y, 13);
  } else if (st.line) {
    g.lineStyle(st.line.width, st.line.color, st.line.alpha);
    g.strokeCircle(c.x, c.y, 13);
  }
  bake(g, `terrain-corner-${mat}-${corner}`, EDGE_W, EDGE_W);
}

// ---- decals ----------------------------------------------------------------
function bakeDecals(scene: Phaser.Scene): void {
  // pedrinha
  for (let v = 0; v < 2; v++) {
    const g = gfx(scene);
    g.fillStyle(0x000000, 0.2);
    g.fillEllipse(8, 9, 12, 5);
    g.fillStyle(v === 0 ? 0x777c86 : 0x8a8272);
    g.fillEllipse(8, 7, 11, 8);
    g.fillStyle(v === 0 ? 0x9aa0ab : 0xa89f8d, 0.9);
    g.fillEllipse(6, 5, 5, 3);
    bake(g, `terrain-decal-stone-${v}`, 16, 12);
  }
  // flores (3 cores)
  const petals = [0xe8e6f0, 0xd98fb5, 0x8fc7e8];
  const cores = [0xf0c75e, 0xf5e6a0, 0xf0c75e];
  for (let v = 0; v < 3; v++) {
    const g = gfx(scene);
    g.fillStyle(0x27512d, 0.9);
    g.fillRect(5, 6, 2, 5); // caule
    g.fillStyle(petals[v], 0.95);
    g.fillCircle(3, 4, 2.2);
    g.fillCircle(9, 4, 2.2);
    g.fillCircle(6, 1.6, 2.2);
    g.fillCircle(6, 6.4, 2.2);
    g.fillStyle(cores[v]);
    g.fillCircle(6, 4, 1.8);
    bake(g, `terrain-decal-flower-${v}`, 12, 12);
  }
  // rachadura no chão seco
  for (let v = 0; v < 2; v++) {
    const g = gfx(scene);
    g.lineStyle(2, 0x2b1d0e, 0.55);
    g.beginPath();
    g.moveTo(1, 3 + v * 2);
    g.lineTo(6, 7);
    g.lineTo(12, 5 + v * 3);
    g.lineTo(17, 11);
    g.strokePath();
    g.beginPath();
    g.moveTo(8, 6);
    g.lineTo(10, 12);
    g.strokePath();
    bake(g, `terrain-decal-crack-${v}`, 18, 14);
  }
}

// ---- água animada (consumido pelo TerrainSkin) -----------------------------
function bakeWaterFx(scene: Phaser.Scene): void {
  // brilhos de onda: arcos suaves claros (o sprite anima posição/alpha)
  const g = gfx(scene);
  g.lineStyle(3, 0xbfe6ff, 0.9);
  g.beginPath(); g.arc(20, 22, 12, Math.PI * 0.15, Math.PI * 0.85); g.strokePath();
  g.beginPath(); g.arc(44, 40, 10, Math.PI * 0.2, Math.PI * 0.8); g.strokePath();
  g.lineStyle(2, 0xdff2ff, 0.8);
  g.beginPath(); g.arc(38, 14, 7, Math.PI * 0.2, Math.PI * 0.8); g.strokePath();
  bake(g, 'terrain-wave', VIS, VIS);
  // espuma da margem: fiada de bolhas brancas alongadas
  const f = gfx(scene);
  f.fillStyle(0xffffff, 0.9);
  f.fillEllipse(12, 7, 14, 6);
  f.fillStyle(0xffffff, 0.65);
  f.fillEllipse(26, 6, 10, 5);
  f.fillStyle(0xffffff, 0.45);
  f.fillEllipse(38, 8, 8, 4);
  bake(f, 'terrain-foam', 48, 14);
}

// ------------------------------------------------------------------ entrada
/** gera TODAS as texturas de terreno 64px — idempotente, chamado pelo
 *  TerrainGrid (construtor) e pelo TerrainSkin (init) */
export function generateTerrainTextures(scene: Phaser.Scene): void {
  if (scene.textures.exists('terrain-wave')) return;
  for (let v = 0; v < 4; v++) {
    bakeGrass(scene, v);
    bakeTallGrass(scene, v);
    bakeDirt(scene, v);
    bakeSand(scene, v);
    bakeWater(scene, v);
    bakeBurned(scene, v);
    bakeFrozen(scene, v);
    bakeMud(scene, v);
    bakeWall(scene, v);
    bakeRock(scene, v);
    bakeTree(scene, v);
  }
  const mats = ['dirt', 'sand', 'water'];
  const sides: Side[] = ['n', 's', 'w', 'e'];
  const corners: Corner[] = ['nw', 'ne', 'sw', 'se'];
  for (const m of mats) {
    for (const s of sides) {
      bakeEdge(scene, m, s, 0);
      bakeEdge(scene, m, s, 1);
    }
    for (const c of corners) bakeCorner(scene, m, c);
  }
  bakeDecals(scene);
  bakeWaterFx(scene);
}

// ------------------------------------------------------------------ desenho
// lados cardinais: [dx, dy, nome, offsetX, offsetY]
const CARD: ReadonlyArray<readonly [number, number, Side, number, number]> = [
  [0, -1, 'n', 0, 0],
  [0, 1, 's', 0, VIS - EDGE_W],
  [-1, 0, 'w', 0, 0],
  [1, 0, 'e', VIS - EDGE_W, 0],
];
// diagonais: [dx, dy, nome, offsetX, offsetY]
const DIAG: ReadonlyArray<readonly [number, number, Corner, number, number]> = [
  [-1, -1, 'nw', 0, 0],
  [1, -1, 'ne', VIS - EDGE_W, 0],
  [-1, 1, 'sw', 0, VIS - EDGE_W],
  [1, 1, 'se', VIS - EDGE_W, VIS - EDGE_W],
];

/**
 * Redesenha UMA célula na RenderTexture 2× do TerrainGrid (coords de textura:
 * cx*64, cy*64). Deve ser chamada entre beginDraw()/endDraw().
 * Determinística: mesma célula → sempre o mesmo desenho (variantes e decals
 * por hash; autotiling só olha MATERIAIS, que são imutáveis — redraw de uma
 * célula suja nunca exige redesenhar vizinhos).
 */
export function drawTerrainCell(
  rt: Phaser.GameObjects.RenderTexture,
  cx: number,
  cy: number,
  at: CellAt,
): void {
  const cell = at(cx, cy);
  if (!cell) return;
  const x = cx * VIS;
  const y = cy * VIS;
  const v = hashInt(cx, cy, SALT_VAR) & 3;

  // estados que SUBSTITUEM o chão (tile pleno, sem autotiling — a borda dura
  // aqui é leitura de gameplay: a área afetada precisa ser exata)
  switch (cell.state) {
    case TerrainState.BURNED: rt.batchDraw(`terrain-burned-${v}`, x, y); return;
    case TerrainState.FROZEN: rt.batchDraw(`terrain-frozen-${v}`, x, y); return;
    case TerrainState.MUD: rt.batchDraw(`terrain-mud-${v}`, x, y); return;
    case TerrainState.WALL: rt.batchDraw(`terrain-wall-${v}`, x, y); return;
    default: break; // NORMAL/BURNING/ELECTRIFIED mantêm o tile do material
  }

  // 1) solo base
  const g = groundOf(cx, cy, at);
  let baseKey: string;
  if (g === G_WATER) baseKey = `terrain-water-${v}`;
  else if (g === G_SAND) baseKey = `terrain-sand-${v}`;
  else if (g === G_DIRT) baseKey = `terrain-dirt-${v}`;
  else if (cell.material === TerrainMaterial.TALL_GRASS) baseKey = `terrain-tall-${v}`;
  else baseKey = `terrain-grass-${v}`;
  rt.batchDraw(baseKey, x, y);

  // 2) autotiling: vizinhos de prioridade MAIOR avançam sobre esta célula
  const overlays: { pri: number; key: string; ox: number; oy: number }[] = [];
  for (let i = 0; i < CARD.length; i++) {
    const [dx, dy, side, ox, oy] = CARD[i];
    const ng = groundOf(cx + dx, cy + dy, at);
    if (ng > g) {
      const ev = hashInt(cx + dx, cy + dy, SALT_EDGE + i) & 1;
      overlays.push({ pri: ng, key: `terrain-edge-${GROUND_NAME[ng]}-${side}-${ev}`, ox, oy });
    }
  }
  for (const [dx, dy, corner, ox, oy] of DIAG) {
    const ng = groundOf(cx + dx, cy + dy, at);
    if (ng <= g) continue;
    // canto só quando NENHUM cardinal adjacente já traz o mesmo material
    if (groundOf(cx + dx, cy, at) === ng || groundOf(cx, cy + dy, at) === ng) continue;
    overlays.push({ pri: ng, key: `terrain-corner-${GROUND_NAME[ng]}-${corner}`, ox, oy });
  }
  overlays.sort((a, b) => a.pri - b.pri); // menor prioridade primeiro; água por cima
  for (const o of overlays) rt.batchDraw(o.key, x + o.ox, y + o.oy);

  // 3) material de superfície (rocha/árvore) sobre o solo autotilado
  if (cell.material === TerrainMaterial.ROCK) rt.batchDraw(`terrain-rock-${v}`, x, y);
  else if (cell.material === TerrainMaterial.TREE) rt.batchDraw(`terrain-tree-${v}`, x, y);

  // 4) decals esparsos (gate de qualidade: Baixa = sem decals)
  if (Settings.get().video.quality === 'low') return;
  if (cell.material === TerrainMaterial.ROCK || cell.material === TerrainMaterial.TREE) return;
  if (g === G_WATER) return;
  const r = terrainHash01(cx, cy, SALT_DECAL);
  let decal: string | null = null;
  if (g === G_GRASS && cell.material === TerrainMaterial.GRASS) {
    if (r < 0.05) decal = `terrain-decal-flower-${hashInt(cx, cy, 7) % 3}`;
    else if (r < 0.12) decal = `terrain-decal-stone-${hashInt(cx, cy, 9) & 1}`;
  } else if (g === G_DIRT) {
    if (r < 0.07) decal = `terrain-decal-stone-${hashInt(cx, cy, 9) & 1}`;
    else if (r < 0.12) decal = `terrain-decal-crack-${hashInt(cx, cy, 11) & 1}`;
  } else if (g === G_SAND) {
    if (r < 0.07) decal = `terrain-decal-stone-${hashInt(cx, cy, 9) & 1}`;
  }
  if (decal) {
    const px = 14 + terrainHash01(cx, cy, SALT_DPOS) * (VIS - 34);
    const py = 14 + terrainHash01(cy, cx, SALT_DPOS) * (VIS - 34);
    rt.batchDraw(decal, x + px, y + py);
  }
}

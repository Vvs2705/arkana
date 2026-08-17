// ============================================================================
// ARKANA v0.1 — texturas procedurais (dono: COORDENADOR)
// Toda a arte placeholder é gerada em runtime com Phaser.Graphics — zero PNGs.
// Regra de acessibilidade (GDD seção 10): cada elemento tem COR + FORMA distintas.
// Chamar generateAllTextures(scene, onProgress) na LoadingScene.
// ============================================================================
import Phaser from 'phaser';
import { TEX, ELEMENT_COLORS, TILE } from './config';
import { Element } from './types';

type G = Phaser.GameObjects.Graphics;

function gfx(scene: Phaser.Scene): G {
  return scene.make.graphics({ x: 0, y: 0 }, false);
}

function bake(g: G, key: string, w: number, h: number): void {
  g.generateTexture(key, w, h);
  g.destroy();
}

/** ruído determinístico simples p/ variação de tiles (sem Math.random p/ coerência) */
function jitter(i: number): number {
  return ((i * 2654435761) % 97) / 97;
}

// ------------------------------------------------------------------ tiles
function tileGrass(scene: Phaser.Scene): void {
  const g = gfx(scene);
  g.fillStyle(0x2e5d34);
  g.fillRect(0, 0, TILE, TILE);
  for (let i = 0; i < 10; i++) {
    const x = jitter(i) * TILE, y = jitter(i + 13) * TILE;
    g.fillStyle(0x3a7042, 1);
    g.fillRect(x, y, 2, 2);
  }
  bake(g, TEX.TILE_GRASS, TILE, TILE);
}

function tileTallGrass(scene: Phaser.Scene): void {
  const g = gfx(scene);
  g.fillStyle(0x24522c);
  g.fillRect(0, 0, TILE, TILE);
  g.lineStyle(2, 0x4f8f4a);
  for (let i = 0; i < 7; i++) {
    const x = 3 + jitter(i) * (TILE - 6);
    g.beginPath();
    g.moveTo(x, TILE - 2);
    g.lineTo(x + (jitter(i + 5) - 0.5) * 6, TILE - 14 - jitter(i + 9) * 8);
    g.strokePath();
  }
  bake(g, TEX.TILE_TALL_GRASS, TILE, TILE);
}

function tileDirt(scene: Phaser.Scene): void {
  const g = gfx(scene);
  g.fillStyle(0x6b4a2b);
  g.fillRect(0, 0, TILE, TILE);
  for (let i = 0; i < 8; i++) {
    g.fillStyle(0x7d5835);
    g.fillRect(jitter(i + 3) * TILE, jitter(i + 17) * TILE, 3, 2);
  }
  bake(g, TEX.TILE_DIRT, TILE, TILE);
}

function tileRock(scene: Phaser.Scene): void {
  const g = gfx(scene);
  g.fillStyle(0x2e5d34); // grama por baixo
  g.fillRect(0, 0, TILE, TILE);
  g.fillStyle(0x6f7480);
  g.fillCircle(TILE / 2, TILE / 2 + 2, 13);
  g.fillStyle(0x8a8f9c);
  g.fillCircle(TILE / 2 - 4, TILE / 2 - 3, 7);
  g.fillStyle(0x565a64);
  g.fillCircle(TILE / 2 + 6, TILE / 2 + 6, 5);
  bake(g, TEX.TILE_ROCK, TILE, TILE);
}

function tileWater(scene: Phaser.Scene): void {
  const g = gfx(scene);
  g.fillStyle(0x1b5e9e);
  g.fillRect(0, 0, TILE, TILE);
  g.lineStyle(2, 0x2aa7ff, 0.7);
  g.beginPath();
  g.moveTo(2, 10); g.lineTo(10, 8); g.lineTo(18, 11); g.lineTo(28, 9);
  g.strokePath();
  g.beginPath();
  g.moveTo(4, 22); g.lineTo(14, 20); g.lineTo(24, 23); g.lineTo(30, 21);
  g.strokePath();
  bake(g, TEX.TILE_WATER, TILE, TILE);
}

function tileTree(scene: Phaser.Scene): void {
  const g = gfx(scene);
  g.fillStyle(0x2e5d34);
  g.fillRect(0, 0, TILE, TILE);
  g.fillStyle(0x4a3018);
  g.fillRect(TILE / 2 - 3, TILE / 2, 6, 12);
  g.fillStyle(0x1d4023);
  g.fillCircle(TILE / 2, TILE / 2 - 4, 12);
  g.fillStyle(0x2c5c33);
  g.fillCircle(TILE / 2 - 4, TILE / 2 - 8, 7);
  bake(g, TEX.TILE_TREE, TILE, TILE);
}

function tileBurned(scene: Phaser.Scene): void {
  const g = gfx(scene);
  g.fillStyle(0x1c1a18);
  g.fillRect(0, 0, TILE, TILE);
  for (let i = 0; i < 6; i++) {
    g.fillStyle(0x33302c);
    g.fillRect(jitter(i + 7) * TILE, jitter(i + 23) * TILE, 4, 3);
  }
  g.fillStyle(0x0f0e0d);
  g.fillRect(TILE / 2 - 2, TILE / 2 - 2, 4, 8); // toco carbonizado
  bake(g, TEX.TILE_BURNED, TILE, TILE);
}

function tileFrozen(scene: Phaser.Scene): void {
  const g = gfx(scene);
  g.fillStyle(0xa8d8f0);
  g.fillRect(0, 0, TILE, TILE);
  g.lineStyle(1, 0xd8f0fa, 0.9);
  g.beginPath();
  g.moveTo(4, 4); g.lineTo(16, 14); g.lineTo(12, 26);
  g.strokePath();
  g.beginPath();
  g.moveTo(24, 6); g.lineTo(20, 18); g.lineTo(28, 28);
  g.strokePath();
  bake(g, TEX.TILE_FROZEN, TILE, TILE);
}

function tileMud(scene: Phaser.Scene): void {
  const g = gfx(scene);
  g.fillStyle(0x3f2c17);
  g.fillRect(0, 0, TILE, TILE);
  g.fillStyle(0x54401f, 1);
  g.fillEllipse(10, 12, 12, 7);
  g.fillEllipse(24, 22, 10, 6);
  g.fillStyle(0x2b1d0e);
  g.fillEllipse(20, 8, 8, 4);
  bake(g, TEX.TILE_MUD, TILE, TILE);
}

function tileWall(scene: Phaser.Scene): void {
  const g = gfx(scene);
  g.fillStyle(0x7a6a55);
  g.fillRect(0, 0, TILE, TILE);
  g.lineStyle(2, 0x574a3a);
  g.strokeRect(1, 1, TILE - 2, TILE - 2);
  g.lineBetween(0, TILE / 2, TILE, TILE / 2);
  g.lineBetween(TILE / 2, 0, TILE / 2, TILE / 2);
  g.lineBetween(TILE / 4, TILE / 2, TILE / 4, TILE);
  g.lineBetween(3 * TILE / 4, TILE / 2, 3 * TILE / 4, TILE);
  bake(g, TEX.TILE_WALL, TILE, TILE);
}

// ------------------------------------------------------------------ overlays
function fxFlame(scene: Phaser.Scene): void {
  const g = gfx(scene);
  g.fillStyle(0xff5a2a, 0.95);
  g.fillTriangle(8, 0, 0, 16, 16, 16);
  g.fillStyle(0xf5d90a, 0.9);
  g.fillTriangle(8, 6, 4, 15, 12, 15);
  bake(g, TEX.FX_FLAME, 16, 16);
}

function fxSpark(scene: Phaser.Scene): void {
  const g = gfx(scene);
  g.lineStyle(2, 0xf5d90a, 1);
  g.beginPath();
  g.moveTo(8, 0); g.lineTo(4, 7); g.lineTo(10, 8); g.lineTo(5, 16);
  g.strokePath();
  bake(g, TEX.FX_SPARK, 14, 16);
}

// ------------------------------------------------------------------ entidades
/** mago: manto + chapéu pontudo (silhueta legível em 28px) */
function entWizard(scene: Phaser.Scene, key: string, robe: number, trim: number): void {
  const g = gfx(scene);
  // manto
  g.fillStyle(robe);
  g.fillTriangle(14, 8, 4, 26, 24, 26);
  // cabeça
  g.fillStyle(0xe8c9a0);
  g.fillCircle(14, 9, 5);
  // chapéu
  g.fillStyle(trim);
  g.fillTriangle(14, 0, 7, 8, 21, 8);
  g.fillRect(5, 7, 18, 3);
  // detalhe do manto
  g.fillStyle(trim, 0.8);
  g.fillRect(12, 14, 4, 10);
  bake(g, key, 28, 28);
}

function entDummy(scene: Phaser.Scene): void {
  const g = gfx(scene);
  // alvo de treino: poste + placa circular
  g.fillStyle(0x8a6a3a);
  g.fillRect(12, 10, 4, 18);
  g.fillStyle(0xd9c9a0);
  g.fillCircle(14, 10, 9);
  g.lineStyle(2, 0xb03030);
  g.strokeCircle(14, 10, 8);
  g.strokeCircle(14, 10, 4);
  g.fillStyle(0xb03030);
  g.fillCircle(14, 10, 2);
  bake(g, TEX.DUMMY, 28, 30);
}

// ------------------------------------------------------------------ projéteis
// formas DISTINTAS por elemento (nunca só cor): fogo=círculo flamejante,
// água=gota, terra=quadrado, vento=anel, raio=zigue-zague
function projFire(scene: Phaser.Scene): void {
  const g = gfx(scene);
  g.fillStyle(ELEMENT_COLORS[Element.FIRE], 0.4);
  g.fillCircle(9, 9, 9);
  g.fillStyle(ELEMENT_COLORS[Element.FIRE]);
  g.fillCircle(9, 9, 6);
  g.fillStyle(0xffd080);
  g.fillCircle(9, 9, 3);
  bake(g, TEX.PROJ_FIRE, 18, 18);
}

function projWater(scene: Phaser.Scene): void {
  const g = gfx(scene);
  g.fillStyle(ELEMENT_COLORS[Element.WATER], 0.45);
  g.fillEllipse(9, 10, 16, 12);
  g.fillStyle(ELEMENT_COLORS[Element.WATER]);
  g.fillTriangle(9, 0, 3, 10, 15, 10);
  g.fillEllipse(9, 11, 12, 9);
  g.fillStyle(0xcdeaff);
  g.fillCircle(7, 9, 2);
  bake(g, TEX.PROJ_WATER, 18, 18);
}

function projEarth(scene: Phaser.Scene): void {
  const g = gfx(scene);
  g.fillStyle(ELEMENT_COLORS[Element.EARTH]);
  g.fillRect(2, 2, 14, 14);
  g.fillStyle(0xc99a5e);
  g.fillRect(4, 4, 6, 6);
  g.fillStyle(0x6b4a26);
  g.fillRect(10, 10, 5, 5);
  bake(g, TEX.PROJ_EARTH, 18, 18);
}

function projWind(scene: Phaser.Scene): void {
  const g = gfx(scene);
  g.lineStyle(3, ELEMENT_COLORS[Element.WIND], 1);
  g.strokeCircle(9, 9, 7);
  g.lineStyle(2, 0xffffff, 0.6);
  g.strokeCircle(9, 9, 4);
  bake(g, TEX.PROJ_WIND, 18, 18);
}

function projLightning(scene: Phaser.Scene): void {
  const g = gfx(scene);
  g.lineStyle(3, ELEMENT_COLORS[Element.LIGHTNING], 1);
  g.beginPath();
  g.moveTo(9, 0); g.lineTo(3, 8); g.lineTo(10, 9); g.lineTo(5, 18);
  g.strokePath();
  g.lineStyle(1, 0xffffff, 0.8);
  g.beginPath();
  g.moveTo(9, 1); g.lineTo(4, 8); g.lineTo(11, 9) ; g.lineTo(6, 17);
  g.strokePath();
  bake(g, TEX.PROJ_LIGHTNING, 14, 18);
}

// ------------------------------------------------------------------ ícones 40×40
function iconBase(g: G, color: number): void {
  g.fillStyle(0x141a36);
  g.fillRoundedRect(0, 0, 40, 40, 8);
  g.lineStyle(2, color);
  g.strokeRoundedRect(1, 1, 38, 38, 8);
}

function iconFire(scene: Phaser.Scene): void {
  const g = gfx(scene);
  iconBase(g, ELEMENT_COLORS[Element.FIRE]);
  g.fillStyle(ELEMENT_COLORS[Element.FIRE]);
  g.fillTriangle(20, 6, 9, 30, 31, 30);
  g.fillStyle(0xffd080);
  g.fillTriangle(20, 16, 14, 29, 26, 29);
  bake(g, TEX.ICON_FIRE, 40, 40);
}

function iconWater(scene: Phaser.Scene): void {
  const g = gfx(scene);
  iconBase(g, ELEMENT_COLORS[Element.WATER]);
  g.fillStyle(ELEMENT_COLORS[Element.WATER]);
  g.fillTriangle(20, 6, 10, 22, 30, 22);
  g.fillEllipse(20, 25, 20, 16);
  g.fillStyle(0xcdeaff);
  g.fillCircle(16, 22, 3);
  bake(g, TEX.ICON_WATER, 40, 40);
}

function iconEarth(scene: Phaser.Scene): void {
  const g = gfx(scene);
  iconBase(g, ELEMENT_COLORS[Element.EARTH]);
  g.fillStyle(ELEMENT_COLORS[Element.EARTH]);
  g.fillRect(9, 9, 22, 22);
  g.fillStyle(0xc99a5e);
  g.fillRect(12, 12, 9, 9);
  bake(g, TEX.ICON_EARTH, 40, 40);
}

function iconWind(scene: Phaser.Scene): void {
  const g = gfx(scene);
  iconBase(g, ELEMENT_COLORS[Element.WIND]);
  g.lineStyle(3, ELEMENT_COLORS[Element.WIND]);
  g.beginPath();
  g.moveTo(8, 14); g.lineTo(26, 14);
  g.strokePath();
  g.strokeCircle(29, 11, 3);
  g.beginPath();
  g.moveTo(8, 22); g.lineTo(30, 22);
  g.strokePath();
  g.strokeCircle(33, 25, 3);
  g.beginPath();
  g.moveTo(8, 30); g.lineTo(22, 30);
  g.strokePath();
  bake(g, TEX.ICON_WIND, 40, 40);
}

function iconLightning(scene: Phaser.Scene): void {
  const g = gfx(scene);
  iconBase(g, ELEMENT_COLORS[Element.LIGHTNING]);
  g.fillStyle(ELEMENT_COLORS[Element.LIGHTNING]);
  g.fillTriangle(24, 5, 12, 22, 20, 22);
  g.fillTriangle(18, 18, 16, 35, 28, 18);
  bake(g, TEX.ICON_LIGHTNING, 40, 40);
}

// ------------------------------------------------------------------ UI
/** Selo de Arkana: pentágono rúnico + 5 gemas elementais (GDD seção 10) */
function uiSeal(scene: Phaser.Scene): void {
  const g = gfx(scene);
  const cx = 60, cy = 60, R = 52, r = 44;
  const pts: { x: number; y: number }[] = [];
  for (let i = 0; i < 5; i++) {
    const a = -Math.PI / 2 + (i * 2 * Math.PI) / 5;
    pts.push({ x: cx + R * Math.cos(a), y: cy + R * Math.sin(a) });
  }
  g.lineStyle(3, 0xf0c75e, 0.9);
  g.strokeCircle(cx, cy, r + 4);
  g.beginPath();
  g.moveTo(pts[0].x, pts[0].y);
  for (let i = 1; i <= 5; i++) g.lineTo(pts[i % 5].x, pts[i % 5].y);
  g.strokePath();
  // pentagrama interno sutil
  g.lineStyle(1, 0xf0c75e, 0.35);
  g.beginPath();
  g.moveTo(pts[0].x, pts[0].y);
  g.lineTo(pts[2].x, pts[2].y);
  g.lineTo(pts[4].x, pts[4].y);
  g.lineTo(pts[1].x, pts[1].y);
  g.lineTo(pts[3].x, pts[3].y);
  g.closePath();
  g.strokePath();
  // gemas elementais nas pontas
  const order = [Element.FIRE, Element.WATER, Element.EARTH, Element.WIND, Element.LIGHTNING];
  order.forEach((el, i) => {
    g.fillStyle(ELEMENT_COLORS[el]);
    g.fillCircle(pts[i].x, pts[i].y, 6);
    g.lineStyle(1, 0xffffff, 0.7);
    g.strokeCircle(pts[i].x, pts[i].y, 6);
  });
  bake(g, TEX.SEAL, 120, 120);
}

function uiParticle(scene: Phaser.Scene): void {
  const g = gfx(scene);
  g.fillStyle(0xffffff, 0.9);
  g.fillCircle(3, 3, 3);
  bake(g, TEX.PARTICLE, 6, 6);
}

function uiPixel(scene: Phaser.Scene): void {
  const g = gfx(scene);
  g.fillStyle(0xffffff);
  g.fillRect(0, 0, 2, 2);
  bake(g, TEX.PIXEL, 2, 2);
}

// ------------------------------------------------------------------ entrada
const STEPS: ((s: Phaser.Scene) => void)[] = [
  tileGrass, tileTallGrass, tileDirt, tileRock, tileWater, tileTree,
  tileBurned, tileFrozen, tileMud, tileWall,
  fxFlame, fxSpark,
  (s) => entWizard(s, TEX.PLAYER, 0x3b2f6b, 0xf0c75e),
  (s) => entWizard(s, TEX.BOT, 0x6b2f2f, 0x9aa),
  entDummy,
  projFire, projWater, projEarth, projWind, projLightning,
  iconFire, iconWater, iconEarth, iconWind, iconLightning,
  uiSeal, uiParticle, uiPixel,
];

/**
 * Gera todas as texturas do jogo. onProgress recebe 0..1 (barra real do Loading).
 * Idempotente: pula chaves já existentes.
 *
 * PROJETO PRISMA (planejado, ver docs/PROJETO_PRISMA.md): a intenção é que estas
 * funções virem o FALLBACK e módulos em `src/core/art/*` sobrescrevam as chaves
 * com arte de alta qualidade, mantendo os dois níveis — se um módulo de arte
 * sumir, o jogo continua renderizando com a arte de protótipo.
 */
export function generateAllTextures(
  scene: Phaser.Scene,
  onProgress?: (p: number) => void,
): void {
  if (scene.textures.exists(TEX.PIXEL)) { onProgress?.(1); return; }
  STEPS.forEach((fn, i) => {
    fn(scene);
    onProgress?.((i + 1) / STEPS.length);
  });
}

/** substitui uma textura existente (ponto de extensão para o Projeto Prisma) */
export function replaceTexture(scene: Phaser.Scene, key: string): void {
  if (scene.textures.exists(key)) scene.textures.remove(key);
}

// PRISMA-1 · raia 4 — texturas de partícula do VFX (arquivo da raia 4).
// Regra: chaves com prefixo "vfx"; textures.ts (coordenador) NÃO é tocado.
// Todas brancas — cor vem por tint nos emitters (1 textura serve N elementos).
import Phaser from 'phaser';

export const VFX_TEX = {
  /** brilho radial suave — flash de impacto, pólen ambiental */
  SOFT: 'vfx-soft',
  /** anel de onda de choque — escala para fora no impacto */
  RING: 'vfx-ring',
  /** blob de fumaça — camada 3 do fogo de terreno */
  SMOKE: 'vfx-smoke',
} as const;

function gfx(scene: Phaser.Scene): Phaser.GameObjects.Graphics {
  return scene.make.graphics({ x: 0, y: 0 }, false);
}

/** Idempotente — chamado no init do CombatVfx (texturas sobrevivem a restarts). */
export function generateVfxTextures(scene: Phaser.Scene): void {
  if (scene.textures.exists(VFX_TEX.SOFT)) return;

  // brilho suave: círculos concêntricos com alpha crescendo ao centro
  let g = gfx(scene);
  const steps = 5;
  for (let i = 0; i < steps; i++) {
    g.fillStyle(0xffffff, 0.1 + (i / (steps - 1)) * 0.55);
    g.fillCircle(8, 8, 8 - i * 1.5);
  }
  g.generateTexture(VFX_TEX.SOFT, 16, 16);
  g.destroy();

  // onda de choque: anel duplo (halo fino + corpo)
  g = gfx(scene);
  g.lineStyle(2, 0xffffff, 0.45);
  g.strokeCircle(16, 16, 14);
  g.lineStyle(3, 0xffffff, 1);
  g.strokeCircle(16, 16, 11);
  g.generateTexture(VFX_TEX.RING, 32, 32);
  g.destroy();

  // fumaça: blobs sobrepostos (forma irregular; alpha/tint no emitter)
  g = gfx(scene);
  g.fillStyle(0xffffff, 0.5);
  g.fillCircle(10, 13, 7);
  g.fillCircle(16, 9, 6);
  g.fillCircle(7, 8, 5);
  g.generateTexture(VFX_TEX.SMOKE, 24, 22);
  g.destroy();
}

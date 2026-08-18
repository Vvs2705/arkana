// ============================================================================
// PRISMA-1 · raia 3 — texturas 64px dos personagens (arte procedural).
// Desenhadas grandes e EXIBIDAS no tamanho lógico do protótipo (28px) —
// hitbox/colisão intactas (BODY_RADIUS é independente do sprite).
// Idempotente: ensureCharacterTextures pode ser chamada por qualquer entidade.
// ============================================================================
import Phaser from 'phaser';

export const CHAR_TEX = {
  PLAYER: 'character-player',
  BOT: 'character-bot',
  DUMMY: 'character-dummy',
  SHADOW: 'character-shadow',
  CLOAK: 'character-cloak',
} as const;

/** magos desenhados em 64px, exibidos em 28px (tamanho lógico atual) */
export const CHAR_BASE_SCALE = 28 / 64;
/** segmento de manto desenhado em 2× e exibido na metade (borda suave) */
export const CLOAK_BASE_SCALE = 0.5;

type G = Phaser.GameObjects.Graphics;

function gfx(scene: Phaser.Scene): G {
  return scene.make.graphics({ x: 0, y: 0 }, false);
}

function bake(g: G, key: string, w: number, h: number): void {
  g.generateTexture(key, w, h);
  g.destroy();
}

/** escurece/clareia um 0xRRGGBB multiplicando os canais */
function shade(c: number, f: number): number {
  const r = Math.min(255, Math.round(((c >> 16) & 0xff) * f));
  const g = Math.min(255, Math.round(((c >> 8) & 0xff) * f));
  const b = Math.min(255, Math.round((c & 0xff) * f));
  return (r << 16) | (g << 8) | b;
}

/** mago 64×64: manto em camadas, dobras, cabeça, chapéu pontudo com aba */
function wizard64(scene: Phaser.Scene, key: string, robe: number, hat: number): void {
  const g = gfx(scene);
  const robeDark = shade(robe, 0.62);
  const robeLight = shade(robe, 1.25);

  // manto: camada de trás (mais escura e larga) + principal + bainha
  g.fillStyle(robeDark);
  g.fillTriangle(32, 24, 8, 61, 56, 61);
  g.fillStyle(robe);
  g.fillTriangle(32, 22, 12, 59, 52, 59);
  g.fillStyle(robeDark, 0.9);
  g.fillEllipse(32, 59, 40, 7);

  // dobras do tecido
  g.lineStyle(2, robeDark, 0.85);
  g.lineBetween(26, 34, 21, 56);
  g.lineBetween(38, 34, 43, 56);
  // brilho lateral (luz vindo de cima-esquerda)
  g.lineStyle(2, robeLight, 0.5);
  g.lineBetween(23, 34, 17, 55);

  // faixa central decorativa (cor do chapéu)
  g.fillStyle(hat, 0.9);
  g.fillRect(29, 32, 6, 24);

  // ombros
  g.fillStyle(robe);
  g.fillEllipse(32, 30, 32, 13);

  // cabeça + sombra da aba na testa + olhos
  g.fillStyle(0xe8c9a0);
  g.fillCircle(32, 21, 9);
  g.fillStyle(0xb08d63, 0.55);
  g.fillEllipse(32, 16, 16, 6);
  g.fillStyle(0x2a2138);
  g.fillCircle(29, 23, 1.5);
  g.fillCircle(35, 23, 1.5);

  // chapéu: cone com sombreado + aba elíptica
  g.fillStyle(hat);
  g.fillTriangle(32, 1, 18, 17, 46, 17);
  g.fillStyle(0x000000, 0.18);
  g.fillTriangle(32, 1, 32, 17, 46, 17);
  g.fillStyle(hat);
  g.fillEllipse(32, 17, 40, 8);
  g.fillStyle(0x000000, 0.18);
  g.fillEllipse(32, 18.5, 40, 4);

  bake(g, key, 64, 64);
}

/** dummy de treino 64×68: poste com veios, base e alvo circular detalhado */
function dummy64(scene: Phaser.Scene): void {
  const g = gfx(scene);
  // base fincada no chão
  g.fillStyle(0x4a3018);
  g.fillEllipse(32, 63, 22, 7);
  // poste com veios de madeira
  g.fillStyle(0x7a5c32);
  g.fillRect(28, 24, 8, 38);
  g.lineStyle(1, 0x5c4425, 0.9);
  g.lineBetween(30, 26, 30, 60);
  g.lineBetween(34, 28, 34, 61);
  // placa do alvo
  g.fillStyle(0xd9c9a0);
  g.fillCircle(32, 22, 20);
  g.lineStyle(3, 0x8a6a3a);
  g.strokeCircle(32, 22, 19);
  g.lineStyle(4, 0xb03030);
  g.strokeCircle(32, 22, 13);
  g.strokeCircle(32, 22, 7);
  g.fillStyle(0xb03030);
  g.fillCircle(32, 22, 3);
  // brilho da placa
  g.fillStyle(0xffffff, 0.25);
  g.fillEllipse(25, 14, 10, 6);
  bake(g, CHAR_TEX.DUMMY, 64, 68);
}

/** sombra elíptica suave 32×16 (camadas de alpha simulam gradiente radial) */
function shadowTex(scene: Phaser.Scene): void {
  const g = gfx(scene);
  g.fillStyle(0x000000, 0.30);
  g.fillEllipse(16, 8, 30, 13);
  g.fillStyle(0x000000, 0.28);
  g.fillEllipse(16, 8, 22, 9);
  g.fillStyle(0x000000, 0.25);
  g.fillEllipse(16, 8, 13, 5);
  bake(g, CHAR_TEX.SHADOW, 32, 16);
}

/** segmento de manto 28×22 (branco — tintado com a cor da veste em runtime) */
function cloakTex(scene: Phaser.Scene): void {
  const g = gfx(scene);
  g.fillStyle(0xffffff, 0.55);
  g.fillEllipse(14, 11, 26, 19);
  g.fillStyle(0xffffff, 1);
  g.fillEllipse(14, 11, 20, 14);
  bake(g, CHAR_TEX.CLOAK, 28, 22);
}

/** gera as texturas de personagem uma única vez (chamada pelas entidades) */
export function ensureCharacterTextures(scene: Phaser.Scene): void {
  if (scene.textures.exists(CHAR_TEX.SHADOW)) return;
  shadowTex(scene);
  cloakTex(scene);
  // player: manto roxo-arcano com detalhes dourados (paleta GDD §10)
  wizard64(scene, CHAR_TEX.PLAYER, 0x3b2f6b, 0xf0c75e);
  // bot: base quase neutra — o tint por elemento (identidade) colore em runtime
  wizard64(scene, CHAR_TEX.BOT, 0xbdbdc4, 0xd8d8de);
  dummy64(scene);
}

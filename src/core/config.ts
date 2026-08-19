// ============================================================================
// ARKANA v0.1 — configuração global (dono: COORDENADOR — não alterar)
// ============================================================================
import { Element } from './types';

export const GAME_WIDTH = 1280;
export const GAME_HEIGHT = 720;

export const TILE = 32;
export const ARENA_COLS = 60;
export const ARENA_ROWS = 60;

export const STUDIO_NAME = 'VSTACK GAMES';
export const GAME_TITLE = 'ARKANA';
export const GAME_VERSION = 'v0.1 — Campo de Provas';

// Paleta (GDD seção 10)
export const COLORS = {
  night: 0x0b1026,
  nightDeep: 0x05070f,
  gold: 0xf0c75e,
  goldDim: 0x8a7336,
  text: 0xe8e6f0,
  textDim: 0x9a97ad,
  danger: 0xe04040,
  panel: 0x141a36,
  panelLight: 0x222b55,
} as const;

export const ELEMENT_COLORS: Record<Element, number> = {
  [Element.FIRE]: 0xff5a2a,
  [Element.WATER]: 0x2aa7ff,
  [Element.EARTH]: 0xa8763e,
  [Element.WIND]: 0x8fe8c9,
  [Element.LIGHTNING]: 0xf5d90a,
};

export const SHIELD_COLORS: Record<number, number> = {
  1: 0xffffff, // branco
  2: 0x3aa0ff, // azul
  3: 0xb060ff, // roxo
  4: 0xf0c75e, // dourado
};

export const SCENE = {
  SPLASH: 'Splash',
  LOADING: 'Loading',
  TITLE: 'Title',
  MENU: 'Menu',
  SETTINGS: 'Settings',
  CREDITS: 'Credits',
  ARENA: 'Arena',
  PAUSE: 'Pause',
  TOUCH_LAYOUT: 'TouchLayout',
} as const;

// Chaves de textura geradas por src/core/textures.ts (generateAllTextures)
export const TEX = {
  // tiles 32×32
  TILE_GRASS: 'tile-grass',
  TILE_TALL_GRASS: 'tile-tall-grass',
  TILE_DIRT: 'tile-dirt',
  TILE_ROCK: 'tile-rock',
  TILE_WATER: 'tile-water',
  TILE_TREE: 'tile-tree',
  TILE_BURNED: 'tile-burned',
  TILE_FROZEN: 'tile-frozen',
  TILE_MUD: 'tile-mud',
  TILE_WALL: 'tile-wall',
  // overlays de estado
  FX_FLAME: 'fx-flame',
  FX_SPARK: 'fx-spark',
  // entidades
  PLAYER: 'ent-player',
  BOT: 'ent-bot',
  DUMMY: 'ent-dummy',
  // projéteis (formas distintas por elemento — acessibilidade)
  PROJ_FIRE: 'proj-fire',
  PROJ_WATER: 'proj-water',
  PROJ_EARTH: 'proj-earth',
  PROJ_WIND: 'proj-wind',
  PROJ_LIGHTNING: 'proj-lightning',
  // ícones de elemento 40×40 (forma + símbolo distintos)
  ICON_FIRE: 'icon-fire',
  ICON_WATER: 'icon-water',
  ICON_EARTH: 'icon-earth',
  ICON_WIND: 'icon-wind',
  ICON_LIGHTNING: 'icon-lightning',
  // UI
  SEAL: 'ui-seal',
  PARTICLE: 'ui-particle',
  PIXEL: 'ui-pixel',
  // controles de toque (Fase 2 — GDD 19.3)
  STICK_BASE: 'touch-stick-base',
  STICK_THUMB: 'touch-stick-thumb',
  BTN_ATTACK: 'touch-btn-attack',
  BTN_TACTIC: 'touch-btn-tactic',
  BTN_DODGE: 'touch-btn-dodge',
  BTN_PAUSE: 'touch-btn-pause',
} as const;

export const PROJ_TEX: Record<Element, string> = {
  [Element.FIRE]: TEX.PROJ_FIRE,
  [Element.WATER]: TEX.PROJ_WATER,
  [Element.EARTH]: TEX.PROJ_EARTH,
  [Element.WIND]: TEX.PROJ_WIND,
  [Element.LIGHTNING]: TEX.PROJ_LIGHTNING,
};

export const ICON_TEX: Record<Element, string> = {
  [Element.FIRE]: TEX.ICON_FIRE,
  [Element.WATER]: TEX.ICON_WATER,
  [Element.EARTH]: TEX.ICON_EARTH,
  [Element.WIND]: TEX.ICON_WIND,
  [Element.LIGHTNING]: TEX.ICON_LIGHTNING,
};

export const DEPTH = {
  TERRAIN: 0,
  TERRAIN_FX: 5,
  ENTITY: 10,
  PLAYER: 12,
  PROJECTILE: 15,
  VFX: 20,
  HUD: 100,
} as const;

// Pilhas de fonte (Google Fonts com fallback de sistema)
export const FONTS = {
  title: '"Cinzel", Georgia, serif',
  ui: '"Chakra Petch", "Segoe UI", sans-serif',
  body: '"Inter", "Segoe UI", sans-serif',
} as const;

// ---------------------------------------------------------------------------
// Controles de toque (Fase 2 — GDD 19.3). Posições em px do canvas 1280×720;
// o Scale.FIT projeta p/ a tela. Tamanhos = raios base ANTES da escala do
// jogador (Settings.controls.touch.scale).
// ---------------------------------------------------------------------------
export type TouchControlId = 'stick' | 'attack' | 'tactic' | 'dodge' | 'carousel';

export const TOUCH = {
  /** alvo mínimo de toque em dp (1dp ≈ 1px CSS; devicePixelRatio já está
   *  embutido no tamanho CSS do canvas — conversão p/ px do canvas em runtime) */
  minDp: 48,
  scaleMin: 0.7,
  scaleMax: 1.5,
  /** posições padrão — zonas de polegar: movimento à esquerda, ações à direita */
  defaults: {
    stick: { x: 170, y: 540 },
    attack: { x: 1150, y: 600 },
    tactic: { x: 1044, y: 654 },
    dodge: { x: 1064, y: 514 },
    carousel: { x: 1060, y: 420 },
  },
  /** raios/tamanhos base em px do canvas */
  size: {
    stickBase: 70,
    stickThumb: 30,
    attack: 48,
    tactic: 34,
    dodge: 34,
    icon: 44,
    iconGap: 54,
    pause: 26,
  },
} as const;

/**
 * Raio mínimo, em px do canvas 1280×720, equivalente a TOUCH.minDp na tela real.
 *
 * `dispW` é a largura CSS do canvas (Phaser: `scale.displaySize.width`) e MUDA
 * durante a sessão — rotação, tela dividida, WebView reajustando depois do
 * load. Por isso a conversão é uma função pura de `dispW`, e não uma constante
 * calculada uma vez: a TouchControls a chama a cada RESIZE, e o portão do build
 * (vite.config.ts) confere o piso de 48dp com ESTA função, não com uma cópia.
 */
export function minHitRadiusPx(dispW: number): number {
  return dpToPx(TOUCH.minDp, dispW) / 2;
}

/**
 * Converte dp da tela real para px do canvas 1280x720.
 *
 * Existe porque `minHitRadiusPx` nao era o unico numero de DEDO medido em px de
 * canvas: a deadzone de mira tambem era, e variava **7x** conforme a tela (~4dp
 * de arrasto num telefone a 375 CSS, ~28dp num monitor 2560). E' a mesma classe
 * de defeito que ja' mordeu duas vezes neste projeto -- o piso de 48dp virando
 * raio errado por tela, e o alvo de toque parando de valer depois de um giro.
 *
 * REGRA: numero que o DEDO sente vive em dp e passa por aqui. Numero que o
 * MUNDO sente (alcance de magia, raio de explosao) vive em unidade de mundo e
 * nao encosta nesta funcao.
 */
export function dpToPx(dp: number, dispW: number): number {
  const w = dispW > 0 ? dispW : GAME_WIDTH;
  return dp * (GAME_WIDTH / w);
}

/** cópia mutável das posições padrão (p/ DEFAULT_SETTINGS e "Restaurar padrão") */
export function defaultTouchLayout(): Record<TouchControlId, { x: number; y: number }> {
  return structuredClone(TOUCH.defaults) as unknown as Record<TouchControlId, { x: number; y: number }>;
}

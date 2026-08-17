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

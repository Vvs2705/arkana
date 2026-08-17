// ============================================================================
// ARKANA v0.1 — CONTRATO COMPARTILHADO ENTRE RAIAS (dono: COORDENADOR)
// Workers implementam EXATAMENTE estas interfaces. NÃO alterar este arquivo.
// ============================================================================
import Phaser from 'phaser';

// ---------------------------------------------------------------- Elementos
export enum Element {
  FIRE = 'fire',
  WATER = 'water',
  EARTH = 'earth',
  WIND = 'wind',
  LIGHTNING = 'lightning',
}
export const ELEMENTS: Element[] = [
  Element.FIRE, Element.WATER, Element.EARTH, Element.WIND, Element.LIGHTNING,
];

// ---------------------------------------------------------------- Terreno
export enum TerrainMaterial { GRASS, TALL_GRASS, DIRT, ROCK, WATER, TREE }
export enum TerrainState { NORMAL, BURNING, BURNED, FROZEN, ELECTRIFIED, MUD, WALL }

export interface TerrainCell {
  material: TerrainMaterial;
  state: TerrainState;
  /** ms restantes do estado atual; -1 = permanente (ex.: BURNED) */
  stateTimer: number;
  /** hp restante quando state === WALL (muro de pedra) */
  wallHp?: number;
}

export interface HazardInfo {
  /** dano por segundo que a célula aplica a quem estiver nela (0 = nenhum) */
  dps: number;
  /** multiplicador de velocidade de quem pisa (1 = normal; lama ≈ 0.45) */
  slowMult: number;
}

/**
 * API do módulo de terreno reativo (raia B — src/terrain/TerrainGrid.ts).
 * Consumida pela ArenaScene (raia C), Bots e HUD (raia D).
 * O grid se auto-renderiza (RenderTexture base + overlays de fogo/raio);
 * entidades NÃO usam arcade colliders contra tiles — consultam isBlocking.
 */
export interface ITerrainGrid {
  readonly cols: number;
  readonly rows: number;
  readonly tileSize: number;
  /** gera o layout fixo da arena (lago, floresta, grama alta, rochas, campo) */
  generate(): void;
  getCell(cx: number, cy: number): TerrainCell | null;
  worldToCell(x: number, y: number): { cx: number; cy: number };
  /** centro da célula em coordenadas de mundo */
  cellToWorld(cx: number, cy: number): { x: number; y: number };
  /** bloqueia movimento: TREE viva, ROCK, WALL, WATER não-congelada */
  isBlocking(cx: number, cy: number): boolean;
  /** bloqueia projéteis: TREE viva, ROCK, WALL (água NÃO bloqueia projétil) */
  blocksProjectile(cx: number, cy: number): boolean;
  getSpeedMult(cx: number, cy: number): number;
  getHazard(cx: number, cy: number): HazardInfo;
  /** true em TALL_GRASS intacta — reduz raio de detecção dos bots */
  isConcealing(cx: number, cy: number): boolean;
  /**
   * Aplica um elemento numa área (impacto de projétil/tática).
   * strong=true (tática) usa raio maior e efeitos garantidos.
   * Implementa a tabela da seção 14 do GDD (fogo propaga, água congela lago,
   * raio eletrocuta água, água+terra=lama, terra=muro, vento espalha/apaga).
   */
  applyElement(worldX: number, worldY: number, element: Element, radiusCells: number, strong: boolean): void;
  /** dano em muros/árvores (projéteis que batem em célula bloqueante) */
  damageCell(cx: number, cy: number, dmg: number): void;
  /** cor 0xRRGGBB da célula para o minimapa */
  getCellColor(cx: number, cy: number): number;
  update(time: number, delta: number): void;
  destroy(): void;
}

// ---------------------------------------------------------------- Entidades
/** referência leve de entidade viva, exposta a bots/HUD (sem acoplar classes) */
export interface EntityRef {
  x: number;
  y: number;
  hp: number;
  alive: boolean;
  isPlayer: boolean;
  /** id estável p/ minimapa/telemetria ('player', 'bot-1'..., 'dummy') */
  id: string;
}

export interface ProjectileSpawn {
  x: number;
  y: number;
  /** radianos */
  angle: number;
  element: Element;
  fromPlayer: boolean;
  damage: number;
  speed: number;
  /** raio de colisão em px */
  radius: number;
  /** true = tática: aplica elemento no terreno com força (raio maior) */
  strong?: boolean;
}

// ---------------------------------------------------------------- Stats p/ HUD
export interface PlayerStats {
  hp: number;
  maxHp: number;
  shield: number;
  shieldMax: number;
  /** 1..4 (branco/azul/roxo/dourado) */
  shieldLevel: number;
  /** dano acumulado p/ evolução e limiar seguinte (-1 quando nível máx.) */
  evoProgress: number;
  evoNext: number;
  mana: number;
  manaMax: number;
  element: Element;
  tacticCdRemaining: number;
  tacticCdTotal: number;
  dodgeCdRemaining: number;
  dodgeCdTotal: number;
}

/**
 * API que a ArenaScene (raia C) implementa e injeta em Bot/Dummy/HUD (raia D).
 * Bots atacam via spawnProjectile; HUD lê getPlayerStats/getEntities.
 */
export interface ArenaApi {
  scene: Phaser.Scene;
  terrain: ITerrainGrid;
  spawnProjectile(opts: ProjectileSpawn): void;
  getPlayerRef(): EntityRef;
  /** player + bots vivos + dummy (dummy sempre presente) */
  getEntities(): EntityRef[];
  getPlayerStats(): PlayerStats;
}

// ---------------------------------------------------------------- Eventos
/** emitidos/ouvidos via scene.events da ArenaScene */
export const EVT = {
  /** (novoNivel: number) — escudo evoluiu */
  SHIELD_EVOLVED: 'arkana-shield-evolved',
  /** (dano: number) */
  PLAYER_DAMAGED: 'arkana-player-damaged',
  PLAYER_DIED: 'arkana-player-died',
  /** (id: string) */
  BOT_DIED: 'arkana-bot-died',
  /** ({ x, y, amount }: { x: number; y: number; amount: number }) */
  DAMAGE_NUMBER: 'arkana-damage-number',
  /** (el: Element) */
  ELEMENT_CHANGED: 'arkana-element-changed',
} as const;

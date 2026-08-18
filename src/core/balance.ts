// ============================================================================
// ARKANA v0.1 — dados de balanceamento (papel: Designer de Sistemas)
// "Design antes de código: números viram dados; código lê dados." (equipe-arkana)
// Fontes: GDD seções 4, 5, 14. Dono: COORDENADOR — ajustes só aqui, nunca inline.
// ============================================================================
import { Element } from './types';

export interface BasicSpell {
  dmg: number;
  /** px/s */
  speed: number;
  manaCost: number;
  /** raio de colisão do projétil em px */
  radius: number;
  /** intervalo mínimo entre disparos (ms) */
  fireRateMs: number;
  /** raio (em células) do efeito elemental no terreno ao impactar */
  terrainRadius: number;
}

export interface TacticSpell {
  dmg: number;
  speed: number;
  radius: number;
  cooldownMs: number;
  /** raio (células) do efeito elemental forte no terreno */
  terrainRadius: number;
}

export const BAL = {
  player: {
    hp: 100,
    /** px/s */
    speed: 230,
    manaMax: 100,
    /** mana/s (regenera devagar — GDD regra global 2) */
    manaRegen: 16,
    dodge: {
      /** distância do dash em px */
      dist: 150,
      /** duração do dash em ms */
      durationMs: 200,
      /** invulnerabilidade em ms (i-frames curtos) */
      iframesMs: 280,
      cooldownMs: 1600,
    },
  },

  // Escudo de Magia Evolutivo (GDD seção 5)
  shield: {
    /** capacidade por nível 1..4 */
    caps: [50, 75, 100, 125],
    /** dano acumulado necessário p/ atingir o nível (índice = nível-1) */
    thresholds: [0, 150, 400, 900],
  },

  elements: {
    [Element.FIRE]: {
      basic: { dmg: 13, speed: 500, manaCost: 9, radius: 7, fireRateMs: 260, terrainRadius: 1 } as BasicSpell,
      tactic: { dmg: 22, speed: 420, radius: 12, cooldownMs: 6000, terrainRadius: 3 } as TacticSpell,
    },
    [Element.WATER]: {
      basic: { dmg: 11, speed: 540, manaCost: 8, radius: 7, fireRateMs: 230, terrainRadius: 1 } as BasicSpell,
      tactic: { dmg: 16, speed: 460, radius: 12, cooldownMs: 5500, terrainRadius: 3 } as TacticSpell,
    },
    [Element.EARTH]: {
      basic: { dmg: 16, speed: 420, manaCost: 11, radius: 8, fireRateMs: 340, terrainRadius: 1 } as BasicSpell,
      /** tática de terra ergue muro na área de impacto */
      tactic: { dmg: 10, speed: 380, radius: 12, cooldownMs: 7000, terrainRadius: 2 } as TacticSpell,
    },
    [Element.WIND]: {
      basic: { dmg: 9, speed: 620, manaCost: 7, radius: 7, fireRateMs: 200, terrainRadius: 1 } as BasicSpell,
      tactic: { dmg: 12, speed: 520, radius: 14, cooldownMs: 5000, terrainRadius: 3 } as TacticSpell,
    },
    [Element.LIGHTNING]: {
      basic: { dmg: 15, speed: 700, manaCost: 10, radius: 6, fireRateMs: 300, terrainRadius: 1 } as BasicSpell,
      tactic: { dmg: 20, speed: 600, radius: 10, cooldownMs: 6500, terrainRadius: 3 } as TacticSpell,
    },
  } as Record<Element, { basic: BasicSpell; tactic: TacticSpell }>,

  // Terreno reativo (GDD seção 14; fogo: modelo 4 estados da seção 16.1)
  terrain: {
    /** dano do fogo por tick: começa em burnDmgMin e sobe até burnDmgMax */
    burnDmgMin: 4,
    burnDmgMax: 10,
    burnTickMs: 500,
    /** chance por tick de propagar para CADA vizinho inflamável */
    propagateChance: 0.30,
    /** célula queima por ~8s e vira carvão (BURNED) */
    burnDurationMs: 8000,
    /** lago congelado vira ponte por ~10s */
    freezeDurationMs: 10000,
    electrifyDurationMs: 1600,
    electrifyDps: 30,
    mudDurationMs: 12000,
    mudSlowMult: 0.45,
    /** muro de pedra ~200hp (GDD seção 14) */
    wallHp: 200,
    /** dano que projéteis causam em muros/árvores ao colidir */
    projectileCellDmg: 34,
    tallGrassSpeedMult: 0.92,
  },

  bots: {
    count: 5,
    hp: 100,
    shield: 50,
    speed: 175,
    /** raio de visão normal em px */
    sightRadius: 340,
    /** raio de visão quando o alvo está em grama alta */
    sightConcealed: 100,
    attackRange: 430,
    fireRateMs: 950,
    dmg: 8,
    projSpeed: 430,
    projRadius: 7,
    /** distância (células) de fogo que dispara fuga */
    fleeRadiusCells: 2,
    respawnMs: 6000,
  },

  dummy: {
    /** dummy de treino: hp alto, mostra números, reseta sozinho */
    hp: 100000,
    resetAfterIdleMs: 4000,
  },

  // Controles de toque (Fase 2 — GDD 19.3)
  touch: {
    /** esquema "Simples": a mira gruda no inimigo mais próximo dentro de um
     *  cone na direção do arrasto/última mira */
    aimAssist: {
      coneDeg: 30,
      rangePx: 520,
    },
    /** zona morta do joystick e do arrasto de mira (fração do raio / px) */
    stickDeadzone: 0.15,
    aimDeadzonePx: 14,
  },
} as const;

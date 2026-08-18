// ============================================================================
// PRISMA-1 — contrato dos módulos de apresentação (escrito pelo coordenador).
// Cada raia preenche o SEU módulo (lighting/terrainSkin/characters/vfx) e não
// toca nos demais. Regra da leva: só a camada visual muda — nenhuma alteração
// em types.ts, balance.ts (valores), layout.ts ou lógica de simulação.
// ============================================================================
import Phaser from 'phaser';
import { ArenaApi } from '../core/types';

/** A arena vista pelos módulos: uma Phaser.Scene que implementa a ArenaApi. */
export type ArenaLike = Phaser.Scene & ArenaApi;

export interface RenderModule {
  /** chamado ao fim do create() da ArenaScene — terreno e entidades já existem */
  init(scene: ArenaLike): void;
  update(time: number, delta: number): void;
  /** chamado no shutdown da cena — remover listeners, pipelines e objetos próprios */
  destroy(): void;
}

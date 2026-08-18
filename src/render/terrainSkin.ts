// ============================================================================
// PRISMA-1 · raia 2 — pele do terreno: ÁGUA VIVA (ondulação + espuma na borda).
// Camada puramente visual sobre a RT do TerrainGrid — zero simulação aqui.
// O autotiling/variantes/decals estáticos vivem no redraw do TerrainGrid
// (src/render/terrainTextures.ts); este módulo só cuida do que se MOVE.
//
// Gate de qualidade (settings.video.quality): 'low' → módulo inerte (terreno
// estático); 'medium'/'high' → tudo. 60 FPS é invariante: ~110 sprites de onda
// + ~50 de espuma, atualizados num único loop sem alocação por frame.
// ============================================================================
import Phaser from 'phaser';
import { ArenaLike, RenderModule } from './contract';
import { DEPTH } from '../core/config';
import { Settings } from '../core/settings';
import { TerrainMaterial, TerrainState } from '../core/types';
import type { ITerrainGrid } from '../core/types';
import { generateTerrainTextures, terrainHash01 } from './terrainTextures';

interface WaterFx {
  s: Phaser.GameObjects.Image;
  cx: number;
  cy: number;
  baseX: number;
  baseY: number;
  phase: number;
}

export class TerrainSkin implements RenderModule {
  private terrain: ITerrainGrid | null = null;
  private waves: WaterFx[] = [];
  private foams: WaterFx[] = [];

  init(scene: ArenaLike): void {
    if (Settings.get().video.quality === 'low') return; // Baixa = sem animação
    generateTerrainTextures(scene); // idempotente (TerrainGrid já gera antes)
    const t = scene.terrain;
    this.terrain = t;
    // varredura ÚNICA no init — materiais são imutáveis, a lista nunca muda
    for (let cy = 0; cy < t.rows; cy++) {
      for (let cx = 0; cx < t.cols; cx++) {
        const cell = t.getCell(cx, cy);
        if (!cell || cell.material !== TerrainMaterial.WATER) continue;
        const { x, y } = t.cellToWorld(cx, cy);
        const phase = terrainHash01(cx, cy, 991) * Math.PI * 2;
        const s = scene.add.image(x, y, 'terrain-wave')
          .setScale(0.5)
          .setDepth(DEPTH.TERRAIN + 1)
          .setAlpha(0.2);
        this.waves.push({ s, cx, cy, baseX: x, baseY: y, phase });
        // espuma: um sprite por aresta água→terra (vizinhança-4)
        const sides: ReadonlyArray<readonly [number, number, number]> = [
          [0, -1, 0], [0, 1, 0], [-1, 0, Math.PI / 2], [1, 0, Math.PI / 2],
        ];
        for (const [dx, dy, rot] of sides) {
          const n = t.getCell(cx + dx, cy + dy);
          if (!n || n.material === TerrainMaterial.WATER) continue;
          const f = scene.add.image(x + dx * 12, y + dy * 12, 'terrain-foam')
            .setScale(0.5)
            .setRotation(rot)
            .setDepth(DEPTH.TERRAIN + 2)
            .setAlpha(0.5);
          this.foams.push({ s: f, cx, cy, baseX: x + dx * 12, baseY: y + dy * 12, phase: phase + dx * 1.3 + dy * 2.1 });
        }
      }
    }
  }

  update(time: number, _delta: number): void {
    const t = this.terrain;
    if (!t) return;
    for (const w of this.waves) {
      // gelo congela a ondulação (estado legível — GDD 14); eletrizada segue líquida
      const liquid = t.getCell(w.cx, w.cy)?.state !== TerrainState.FROZEN;
      w.s.setVisible(liquid);
      if (!liquid) continue;
      w.s.x = w.baseX + Math.sin(time * 0.0011 + w.phase) * 1.8;
      w.s.y = w.baseY + Math.cos(time * 0.0009 + w.phase) * 1.2;
      w.s.setAlpha(0.16 + 0.09 * Math.sin(time * 0.0016 + w.phase * 1.7));
    }
    for (const f of this.foams) {
      const liquid = t.getCell(f.cx, f.cy)?.state !== TerrainState.FROZEN;
      f.s.setVisible(liquid);
      if (!liquid) continue;
      f.s.setAlpha(0.36 + 0.22 * Math.sin(time * 0.002 + f.phase));
      f.s.y = f.baseY + Math.sin(time * 0.0013 + f.phase) * 0.8;
    }
  }

  destroy(): void {
    for (const w of this.waves) w.s.destroy();
    for (const f of this.foams) f.s.destroy();
    this.waves = [];
    this.foams = [];
    this.terrain = null;
  }
}

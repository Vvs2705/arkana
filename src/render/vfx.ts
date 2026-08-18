// ============================================================================
// PRISMA-1 · raia 4 — VFX e juice (apresentação pura; zero lógica de jogo).
//
// Entregas: onda de choque + flash + hitstop + recuo de câmera nos impactos,
// fogo de terreno em 3 camadas (chama da raia B + brasas + fumaça daqui),
// partículas ambientais (pólen na floresta/grama alta) e screen shake em
// camadas via trauma decaindo (impacto pequeno ≠ explosão).
//
// Integração com o ProjectilePool (arquivo da MESMA raia): via o holder
// `projectileVfx` exportado abaixo — nenhum evento novo, nenhuma mudança na
// API do pool. Fogo do terreno: varredura periódica do ITerrainGrid (somente
// leitura). Teto de partículas: Settings.video.quality (low/medium/high).
// ============================================================================
import Phaser from 'phaser';
import { ArenaLike, RenderModule } from './contract';
import { Element, EVT, TerrainMaterial, TerrainState } from '../core/types';
import { DEPTH, ELEMENT_COLORS, TEX } from '../core/config';
import { Settings } from '../core/settings';
import { generateVfxTextures, VFX_TEX } from './vfxTextures';

// ---------------------------------------------------------------- juice
/** hitstop: dip curtíssimo de tempo ao acertar entidade (apresentação) */
const HITSTOP_MS = 40;
const HITSTOP_STRONG_MS = 60;
/** intervalo mínimo entre hitstops — combate denso não vira slideshow */
const HITSTOP_COOLDOWN_MS = 160;

/** trauma somado por tipo de impacto (shake = trauma² → camadas naturais) */
const TRAUMA_HIT = 0.16;
const TRAUMA_HIT_STRONG = 0.32;
const TRAUMA_TERRAIN = 0.06;
const TRAUMA_TERRAIN_STRONG = 0.22;
/** trauma por ponto de dano sofrido pelo JOGADOR (e teto por evento) */
const TRAUMA_PER_PLAYER_DMG = 0.012;
const TRAUMA_PLAYER_DMG_MAX = 0.35;
const TRAUMA_DECAY_PER_S = 1.7;
/** amplitude máxima do shake (fração do viewport — ~9px em 720p) */
const SHAKE_MAX_FRAC = 0.012;
const SHAKE_PULSE_MS = 60;

/** recuo visual: chute direcional da câmera na direção do projétil */
const KICK_PX = 5;
const KICK_STRONG_PX = 9;
const KICK_DECAY_MS = 110;

// ---------------------------------------------------------------- anéis/flash
const RING_START_SCALE = 0.35;
const IMPACT_RING_SCALE = 2.4;
const IMPACT_RING_SCALE_STRONG = 4.2;
const IMPACT_RING_MS = 240;
const IMPACT_RING_MS_STRONG = 320;
const SPAWN_RING_SCALE = 0.9;
const SPAWN_RING_MS = 150;
const FLASH_SCALE = 2.6;
const FLASH_MS = 110;
/** teto de imagens de efeito reutilizáveis (anéis + flashes simultâneos) */
const FX_POOL_CAP = 24;

// ---------------------------------------------------------------- fogo/ambiente
/** período da varredura do grid atrás de células BURNING */
const FIRE_SCAN_MS = 300;
/** máximo de células queimando rastreadas (varredura corta aqui) */
const FIRE_TRACK_CAP = 160;
/** células que contam para a taxa de emissão (incêndio grande satura) */
const FIRE_CELL_CAP = 80;
const EMBERS_PER_CELL_PER_S = 1.5;
const SMOKE_PER_CELL_PER_S = 0.6;
/** teto de emissões por frame (também protege contra delta acumulado) */
const EMBER_MAX_PER_FRAME = 8;
const SMOKE_MAX_PER_FRAME = 3;
/** cadência de tentativa de pólen (1 partícula por tentativa válida) */
const POLLEN_INTERVAL_MS = 260;

/** orçamento por nível de qualidade (Settings.video.quality) */
const QUALITY_BUDGET = {
  low: { fireRate: 0.35, smoke: false, ambient: false },
  medium: { fireRate: 0.65, smoke: true, ambient: true },
  high: { fireRate: 1, smoke: true, ambient: true },
} as const;

/**
 * Ponte visual para o ProjectilePool (arquivo da mesma raia 4) — evita criar
 * eventos novos e não toca a API do pool. Setado no init, limpo no destroy.
 */
export const projectileVfx: { current: CombatVfx | null } = { current: null };

export class CombatVfx implements RenderModule {
  private scene!: ArenaLike;

  private ember?: Phaser.GameObjects.Particles.ParticleEmitter;
  private smoke?: Phaser.GameObjects.Particles.ParticleEmitter;
  private pollen?: Phaser.GameObjects.Particles.ParticleEmitter;
  /** pool de imagens p/ anéis de choque e flashes (reuso, sem alocação) */
  private fxPool: Phaser.GameObjects.Image[] = [];

  /** centros (mundo) das células BURNING da última varredura */
  private burning: { x: number; y: number }[] = [];
  private scanAcc = FIRE_SCAN_MS; // força varredura no 1º frame
  private emberAcc = 0;
  private smokeAcc = 0;
  private pollenAcc = 0;

  private trauma = 0;
  private kickX = 0;
  private kickY = 0;
  private hitstopTimer?: number;
  private lastHitstopAt = -1e9;

  // ------------------------------------------------------------------- init
  init(scene: ArenaLike): void {
    this.scene = scene;
    generateVfxTextures(scene);

    // camada 2 do fogo: brasas subindo (aditivas, tons de fogo)
    this.ember = scene.add.particles(0, 0, TEX.PARTICLE, {
      speedX: { min: -14, max: 14 },
      speedY: { min: -60, max: -24 },
      scale: { start: 0.8, end: 0 },
      alpha: { start: 1, end: 0 },
      lifespan: { min: 500, max: 950 },
      tint: [0xffd080, 0xff7a2a, 0xf5d90a],
      blendMode: Phaser.BlendModes.ADD,
      emitting: false,
    }).setDepth(DEPTH.VFX);

    // camada 3 do fogo: fumaça escura crescendo e dissipando
    this.smoke = scene.add.particles(0, 0, VFX_TEX.SMOKE, {
      speedX: { min: -8, max: 8 },
      speedY: { min: -26, max: -12 },
      scale: { start: 0.7, end: 1.9 },
      alpha: { start: 0.26, end: 0 },
      lifespan: { min: 1200, max: 2000 },
      tint: 0x3a3a42,
      emitting: false,
    }).setDepth(DEPTH.VFX);

    // ambiente: pólen discreto derivando na floresta/grama alta
    this.pollen = scene.add.particles(0, 0, VFX_TEX.SOFT, {
      speed: { min: 3, max: 12 },
      angle: { min: 0, max: 360 },
      scale: { start: 0.3, end: 0.1 },
      alpha: { start: 0.35, end: 0 },
      lifespan: { min: 2400, max: 3800 },
      tint: 0xeaf5b8,
      emitting: false,
    }).setDepth(DEPTH.VFX);

    // dano no jogador (inclui queimadura/eletrocussão do terreno) → trauma
    scene.events.on(EVT.PLAYER_DAMAGED, this.onPlayerDamaged, this);

    projectileVfx.current = this;
  }

  // ----------------------------------------------------------------- update
  update(_time: number, delta: number): void {
    this.updateFire(delta);
    this.updateAmbient(delta);
    this.updateShake(delta);
  }

  // ---------------------------------------------------------------- destroy
  destroy(): void {
    projectileVfx.current = null;
    if (this.hitstopTimer !== undefined) {
      window.clearTimeout(this.hitstopTimer);
      this.hitstopTimer = undefined;
      this.scene.game.loop.wake();
    }
    this.scene.events.off(EVT.PLAYER_DAMAGED, this.onPlayerDamaged, this);
    this.ember?.destroy();
    this.smoke?.destroy();
    this.pollen?.destroy();
    this.ember = this.smoke = this.pollen = undefined;
    for (const img of this.fxPool) {
      this.scene.tweens.killTweensOf(img);
      img.destroy();
    }
    this.fxPool = [];
    this.burning = [];
    this.trauma = 0;
    this.kickX = 0;
    this.kickY = 0;
    this.scene.cameras.main.setFollowOffset(0, 0);
  }

  // ================================================================== ponte
  // Chamados pelo ProjectilePool (camada visual da raia 4 em Projectile.ts).

  /** pequeno anel de conjuração na origem do disparo */
  projectileSpawned(x: number, y: number, el: Element, strong: boolean): void {
    this.ringAt(x, y, ELEMENT_COLORS[el], SPAWN_RING_SCALE * (strong ? 1.3 : 1), SPAWN_RING_MS, 0.5);
  }

  /** impacto com peso: onda de choque + (em entidade) flash, hitstop e recuo */
  projectileImpact(
    x: number, y: number,
    dirX: number, dirY: number,
    el: Element, strong: boolean, hitEntity: boolean,
  ): void {
    this.ringAt(
      x, y, ELEMENT_COLORS[el],
      strong ? IMPACT_RING_SCALE_STRONG : IMPACT_RING_SCALE,
      strong ? IMPACT_RING_MS_STRONG : IMPACT_RING_MS,
      0.9,
    );
    if (hitEntity) {
      this.flashAt(x, y);
      // recuo visual: câmera leva um chute na direção do golpe
      this.kickX += dirX * (strong ? KICK_STRONG_PX : KICK_PX);
      this.kickY += dirY * (strong ? KICK_STRONG_PX : KICK_PX);
      this.addTrauma(strong ? TRAUMA_HIT_STRONG : TRAUMA_HIT);
      this.hitstop(strong ? HITSTOP_STRONG_MS : HITSTOP_MS);
    } else {
      this.addTrauma(strong ? TRAUMA_TERRAIN_STRONG : TRAUMA_TERRAIN);
    }
  }

  // ============================================================ fogo 3 camadas
  private updateFire(delta: number): void {
    this.scanAcc += delta;
    if (this.scanAcc >= FIRE_SCAN_MS) {
      this.scanAcc = 0;
      this.scanBurning();
    }
    const n = Math.min(this.burning.length, FIRE_CELL_CAP);
    if (n === 0 || !this.ember || !this.smoke) return;

    const q = QUALITY_BUDGET[Settings.get().video.quality];
    const dtS = delta / 1000;

    this.emberAcc = Math.min(this.emberAcc + n * EMBERS_PER_CELL_PER_S * q.fireRate * dtS, EMBER_MAX_PER_FRAME);
    while (this.emberAcc >= 1) {
      this.emberAcc -= 1;
      const c = this.burning[Math.floor(Math.random() * this.burning.length)];
      this.ember.emitParticleAt(
        c.x + Phaser.Math.Between(-12, 12),
        c.y + Phaser.Math.Between(-10, 6),
      );
    }

    if (!q.smoke) return;
    this.smokeAcc = Math.min(this.smokeAcc + n * SMOKE_PER_CELL_PER_S * q.fireRate * dtS, SMOKE_MAX_PER_FRAME);
    while (this.smokeAcc >= 1) {
      this.smokeAcc -= 1;
      const c = this.burning[Math.floor(Math.random() * this.burning.length)];
      this.smoke.emitParticleAt(
        c.x + Phaser.Math.Between(-10, 10),
        c.y + Phaser.Math.Between(-12, 0),
      );
    }
  }

  /** varre o grid (somente leitura) atrás de células em chamas — a cada 300ms */
  private scanBurning(): void {
    this.burning.length = 0;
    const t = this.scene.terrain;
    for (let cy = 0; cy < t.rows; cy++) {
      for (let cx = 0; cx < t.cols; cx++) {
        const cell = t.getCell(cx, cy);
        if (cell && cell.state === TerrainState.BURNING) {
          this.burning.push(t.cellToWorld(cx, cy));
          if (this.burning.length >= FIRE_TRACK_CAP) return;
        }
      }
    }
  }

  // ================================================================= ambiente
  private updateAmbient(delta: number): void {
    if (!this.pollen) return;
    const q = QUALITY_BUDGET[Settings.get().video.quality];
    if (!q.ambient) return;
    this.pollenAcc += delta;
    if (this.pollenAcc < POLLEN_INTERVAL_MS) return;
    this.pollenAcc = 0;

    // ponto aleatório na tela; só emite se cair em floresta/grama alta intacta
    const view = this.scene.cameras.main.worldView;
    const x = view.x + Math.random() * view.width;
    const y = view.y + Math.random() * view.height;
    const t = this.scene.terrain;
    const { cx, cy } = t.worldToCell(x, y);
    const cell = t.getCell(cx, cy);
    if (
      cell && cell.state === TerrainState.NORMAL
      && (cell.material === TerrainMaterial.TREE || cell.material === TerrainMaterial.TALL_GRASS)
    ) {
      this.pollen.emitParticleAt(x, y);
    }
  }

  // ============================================================ shake em camadas
  private addTrauma(v: number): void {
    this.trauma = Math.min(1, this.trauma + v);
  }

  private onPlayerDamaged(dmg: number): void {
    this.addTrauma(Math.min(dmg * TRAUMA_PER_PLAYER_DMG, TRAUMA_PLAYER_DMG_MAX));
  }

  private updateShake(delta: number): void {
    const cam = this.scene.cameras.main;
    if (this.trauma > 0) {
      this.trauma = Math.max(0, this.trauma - TRAUMA_DECAY_PER_S * (delta / 1000));
      const amp = this.trauma * this.trauma * SHAKE_MAX_FRAC;
      if (amp > 0.0002) cam.shake(SHAKE_PULSE_MS, amp, true);
    }
    if (this.kickX !== 0 || this.kickY !== 0) {
      const f = Math.max(0, 1 - delta / KICK_DECAY_MS);
      this.kickX *= f;
      this.kickY *= f;
      if (Math.abs(this.kickX) < 0.1 && Math.abs(this.kickY) < 0.1) {
        this.kickX = 0;
        this.kickY = 0;
      }
      cam.setFollowOffset(this.kickX, this.kickY);
    }
  }

  // ================================================================== hitstop
  /**
   * Hitstop clássico: congela o frame inteiro por 40–60ms via sleep do game
   * loop. É apresentação pura — no wake o Phaser retoma sem delta acumulado,
   * então a simulação apenas "desliza" alguns ms, imperceptível e coerente
   * (nenhum timer anda dilatado). Cooldown evita cascata em combate denso.
   * ponytail: sleep é global (pausa render de tudo); se algum fluxo de pausa
   * futuro conflitar, trocar por punch de zoom da câmera.
   */
  private hitstop(ms: number): void {
    const now = performance.now();
    if (this.hitstopTimer !== undefined || now - this.lastHitstopAt < HITSTOP_COOLDOWN_MS) return;
    this.lastHitstopAt = now;
    const loop = this.scene.game.loop;
    loop.sleep();
    this.hitstopTimer = window.setTimeout(() => {
      this.hitstopTimer = undefined;
      loop.wake();
    }, ms);
  }

  // ============================================================= pool de FX
  /** pega uma imagem livre do pool (ou cria até o teto); null = orçamento cheio */
  private grabFx(texKey: string): Phaser.GameObjects.Image | null {
    for (const img of this.fxPool) {
      if (!img.visible) {
        this.scene.tweens.killTweensOf(img);
        img.setTexture(texKey).setVisible(true);
        return img;
      }
    }
    if (this.fxPool.length >= FX_POOL_CAP) return null;
    const img = this.scene.add.image(0, 0, texKey)
      .setDepth(DEPTH.VFX)
      .setBlendMode(Phaser.BlendModes.ADD);
    this.fxPool.push(img);
    return img;
  }

  private ringAt(x: number, y: number, tint: number, endScale: number, ms: number, alpha: number): void {
    const img = this.grabFx(VFX_TEX.RING);
    if (!img) return;
    img.setPosition(x, y).setTint(tint).setAlpha(alpha).setScale(RING_START_SCALE);
    this.scene.tweens.add({
      targets: img,
      scale: endScale,
      alpha: 0,
      duration: ms,
      ease: 'Cubic.easeOut',
      onComplete: () => img.setVisible(false),
    });
  }

  /** flash branco no alvo atingido */
  private flashAt(x: number, y: number): void {
    const img = this.grabFx(VFX_TEX.SOFT);
    if (!img) return;
    img.setPosition(x, y).setTint(0xffffff).setAlpha(0.95).setScale(FLASH_SCALE);
    this.scene.tweens.add({
      targets: img,
      scale: FLASH_SCALE * 1.6,
      alpha: 0,
      duration: FLASH_MS,
      ease: 'Quad.easeOut',
      onComplete: () => img.setVisible(false),
    });
  }
}

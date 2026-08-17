// ============================================================================
// ARKANA v0.1 — pool de projéteis (raia C: gameplay)
// Movimento linear com sub-passos (anti-túnel), colisão contra terreno e
// entidades, efeito elemental no grid ao impactar e partículas moderadas
// (respeita Settings.video.quality). Zero alocação por frame: slots reusados.
// ============================================================================
import Phaser from 'phaser';
import { Element, EntityRef, ProjectileSpawn } from '../core/types';
import { DEPTH, ELEMENT_COLORS, PROJ_TEX, TEX } from '../core/config';
import { BAL } from '../core/balance';
import { Audio, type SfxName } from '../core/audio';
import { Settings } from '../core/settings';
import type { ArenaScene } from '../scenes/ArenaScene';

// --- constantes de sensação/física da raia C (briefing) ---------------------
// Números de BALANCEAMENTO (dano, velocidade, raios de terreno) vêm SEMPRE
// de BAL — aqui ficam apenas parâmetros de game feel e varredura.
/** alcance máximo de qualquer projétil (~720px, briefing da raia) */
const MAX_RANGE_PX = 720;
/** folga de corpo das entidades no teste centro-a-centro (briefing: 14px) */
const HIT_PAD_PX = 14;
/** sub-passo da varredura de colisão — menor que meio tile evita túnel */
const SUBSTEP_PX = 10;
/** projéteis pré-alocados no create (evita hitch no primeiro tiroteio) */
const PREWARM = 16;
/** tática levemente maior para leitura visual imediata */
const STRONG_SCALE = 1.3;
/** amplitude do "wobble" do vento (deslocamento perpendicular, só visual) */
const WOBBLE_AMP_PX = 5;
/** frequência do wobble em radianos por pixel percorrido */
const WOBBLE_FREQ = 0.045;
/** fogo perde até 40% da escala ao longo do alcance (chama "morrendo") */
const FIRE_FALLOFF = 0.4;
/** clamp de delta p/ aba inativa — nunca simular mais que isso de uma vez */
const MAX_STEP_MS = 100;

/** estado de um projétil do pool (a colisão usa a posição "na linha";
 *  o wobble do vento é aplicado apenas ao sprite) */
interface Slot {
  img: Phaser.GameObjects.Image;
  active: boolean;
  element: Element;
  fromPlayer: boolean;
  strong: boolean;
  damage: number;
  speed: number;
  radius: number;
  dirX: number;
  dirY: number;
  lineX: number;
  lineY: number;
  traveled: number;
  wobblePhase: number;
}

export class ProjectilePool {
  private readonly scene: ArenaScene;
  private slots: Slot[] = [];
  /** um emitter por elemento (tint fixo no config — barato e compatível) */
  private emitters = new Map<Element, Phaser.GameObjects.Particles.ParticleEmitter>();

  constructor(scene: ArenaScene) {
    this.scene = scene;
    for (let i = 0; i < PREWARM; i++) this.makeSlot();
  }

  // ------------------------------------------------------------------ spawn
  spawn(opts: ProjectileSpawn): void {
    const s = this.obtain();
    s.active = true;
    s.element = opts.element;
    s.fromPlayer = opts.fromPlayer;
    s.strong = opts.strong ?? false;
    s.damage = opts.damage;
    s.speed = opts.speed;
    s.radius = opts.radius;
    s.dirX = Math.cos(opts.angle);
    s.dirY = Math.sin(opts.angle);
    s.lineX = opts.x;
    s.lineY = opts.y;
    s.traveled = 0;
    s.wobblePhase = Math.random() * Math.PI * 2;

    s.img.setTexture(PROJ_TEX[opts.element]);
    // as texturas de projétil "apontam para cima" → alinhar = ângulo + 90°
    s.img.setRotation(opts.angle + Math.PI / 2);
    s.img.setPosition(opts.x, opts.y);
    s.img.setScale(s.strong ? STRONG_SCALE : 1);
    s.img.setAlpha(1);
    s.img.setActive(true).setVisible(true);

    // som de conjuração apenas para o jogador (bots cuidam do próprio áudio)
    if (opts.fromPlayer) Audio.playSfx(('cast_' + opts.element) as SfxName);
  }

  // ----------------------------------------------------------------- update
  update(_time: number, delta: number): void {
    const dt = Math.min(delta, MAX_STEP_MS) / 1000;

    // listas de alvos por lado, montadas uma vez por frame (refs são vivas)
    const all = this.scene.getEntities();
    const vsPlayer: EntityRef[] = [];
    const vsHostiles: EntityRef[] = [];
    for (const r of all) {
      if (!r.alive) continue;
      if (r.isPlayer) vsPlayer.push(r);
      else vsHostiles.push(r);
    }

    for (const s of this.slots) {
      if (!s.active) continue;

      // varredura em sub-passos: terreno primeiro, depois entidades
      let remaining = s.speed * dt;
      let impacted = false;
      const targets = s.fromPlayer ? vsHostiles : vsPlayer;
      const hitR2 = (s.radius + HIT_PAD_PX) * (s.radius + HIT_PAD_PX);

      while (remaining > 0) {
        const step = Math.min(SUBSTEP_PX, remaining);
        remaining -= step;
        s.lineX += s.dirX * step;
        s.lineY += s.dirY * step;
        s.traveled += step;

        // 1) terreno bloqueante (árvore viva, rocha, muro)
        const cell = this.scene.terrain.worldToCell(s.lineX, s.lineY);
        if (this.scene.terrain.blocksProjectile(cell.cx, cell.cy)) {
          this.impact(s, null);
          impacted = true;
          break;
        }

        // 2) entidades do lado oposto (distância centro-a-centro)
        let hit: EntityRef | null = null;
        for (const t of targets) {
          const dx = t.x - s.lineX;
          const dy = t.y - s.lineY;
          if (dx * dx + dy * dy < hitR2) { hit = t; break; }
        }
        if (hit) {
          this.impact(s, hit);
          impacted = true;
          break;
        }

        // 3) alcance esgotado — dissipa com um sopro discreto
        if (s.traveled >= MAX_RANGE_PX) {
          this.emitterFor(s.element).explode(2, s.lineX, s.lineY);
          this.release(s);
          impacted = true;
          break;
        }
      }
      if (impacted) continue;

      // ---- visual: wobble do vento e chama do fogo diminuindo -------------
      let vx = s.lineX;
      let vy = s.lineY;
      if (s.element === Element.WIND) {
        const off = Math.sin(s.traveled * WOBBLE_FREQ + s.wobblePhase) * WOBBLE_AMP_PX;
        vx += -s.dirY * off;
        vy += s.dirX * off;
      }
      s.img.setPosition(vx, vy);
      if (s.element === Element.FIRE) {
        const base = s.strong ? STRONG_SCALE : 1;
        s.img.setScale(base * (1 - FIRE_FALLOFF * (s.traveled / MAX_RANGE_PX)));
      }
    }
  }

  // ---------------------------------------------------------------- impacto
  /** hitRef=null → impacto em célula bloqueante do terreno */
  private impact(s: Slot, hitRef: EntityRef | null): void {
    const spec = s.strong
      ? BAL.elements[s.element].tactic
      : BAL.elements[s.element].basic;

    if (hitRef) {
      // dano roteado pela cena (também alimenta a evolução do escudo)
      this.scene.hitEntity(hitRef, s.damage, s.fromPlayer);
    } else {
      const { cx, cy } = this.scene.terrain.worldToCell(s.lineX, s.lineY);
      this.scene.terrain.damageCell(cx, cy, BAL.terrain.projectileCellDmg);
    }

    // reação elemental do terreno no ponto de impacto (GDD seção 14)
    this.scene.terrain.applyElement(s.lineX, s.lineY, s.element, spec.terrainRadius, s.strong);
    this.burst(s.lineX, s.lineY, s.element, s.strong);
    this.release(s);
  }

  /** rajada de partículas de impacto — contagem sensível à qualidade */
  private burst(x: number, y: number, el: Element, strong: boolean): void {
    const q = Settings.get().video.quality;
    const base = q === 'low' ? 3 : q === 'medium' ? 6 : 9;
    const n = strong ? Math.round(base * 1.6) : base;
    this.emitterFor(el).explode(n, x, y);
  }

  private emitterFor(el: Element): Phaser.GameObjects.Particles.ParticleEmitter {
    let e = this.emitters.get(el);
    if (!e) {
      e = this.scene.add.particles(0, 0, TEX.PARTICLE, {
        speed: { min: 40, max: 170 },
        angle: { min: 0, max: 360 },
        scale: { start: 1.1, end: 0 },
        alpha: { start: 1, end: 0 },
        lifespan: { min: 160, max: 320 },
        tint: ELEMENT_COLORS[el],
        emitting: false,
      }).setDepth(DEPTH.VFX);
      this.emitters.set(el, e);
    }
    return e;
  }

  // ------------------------------------------------------------------- pool
  private obtain(): Slot {
    for (const s of this.slots) if (!s.active) return s;
    return this.makeSlot();
  }

  private makeSlot(): Slot {
    const img = this.scene.add.image(0, 0, PROJ_TEX[Element.FIRE])
      .setDepth(DEPTH.PROJECTILE)
      .setActive(false)
      .setVisible(false);
    const s: Slot = {
      img,
      active: false,
      element: Element.FIRE,
      fromPlayer: true,
      strong: false,
      damage: 0,
      speed: 0,
      radius: 0,
      dirX: 1,
      dirY: 0,
      lineX: 0,
      lineY: 0,
      traveled: 0,
      wobblePhase: 0,
    };
    this.slots.push(s);
    return s;
  }

  private release(s: Slot): void {
    s.active = false;
    s.img.setActive(false).setVisible(false);
  }

  destroy(): void {
    for (const s of this.slots) s.img.destroy();
    this.slots = [];
    for (const e of this.emitters.values()) e.destroy();
    this.emitters.clear();
  }
}

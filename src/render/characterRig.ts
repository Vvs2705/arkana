// ============================================================================
// PRISMA-1 · raia 3 — rig de animação procedural dos personagens.
// Idle "respirando", bob de caminhada, tilt na direção do movimento,
// squash & stretch no dash, manto em 2 segmentos com inércia e sombra
// elíptica projetada. Deriva o movimento do delta de posição — a entidade
// só chama update(dtMs, x, y, opts). Nenhuma lógica de gameplay aqui.
// ============================================================================
import Phaser from 'phaser';
import { Settings } from '../core/settings';
import { CHAR_TEX, CHAR_BASE_SCALE, CLOAK_BASE_SCALE } from './characterTextures';

// --- números visuais (raia 3) ----------------------------------------------
/** respiração: amplitude de escala Y e frequência (rad/ms) */
const BREATH_AMP = 0.035;
const BREATH_HZ = 0.0026;
/** bob de caminhada: amplitude (px) e frequência do passo (rad/ms) */
const BOB_AMP = 2.4;
const BOB_HZ = 0.014;
/** squash sutil no apoio do passo */
const STEP_SQUASH = 0.05;
/** inclinação máxima na direção do movimento (rad) e velocidade do lerp */
const TILT_RAD = 0.12;
const TILT_LERP = 0.012;
/** squash & stretch do dash */
const DASH_STRETCH = 0.30;
const DASH_SQUASH = 0.20;
/** decaimento do pulso de escala (punch) por ms */
const PULSE_DECAY = 0.006;
/** limiar de velocidade (px/ms) p/ considerar "andando" */
const MOVE_EPS = 0.03;
/** sombra: offset vertical (px), alpha e encolhimento por px de bob */
const SHADOW_OFF_Y = 12;
const SHADOW_ALPHA = 0.55;
const SHADOW_BOB_SHRINK = 0.05;
/** manto: offset atrás do movimento (px), queda (px) e lerps dos 2 segmentos */
const CLOAK_TRAIL = 5;
const CLOAK_DROP = 5;
const CLOAK_LERP_1 = 0.020;
const CLOAK_LERP_2 = 0.010;
const CLOAK_ALPHA = 0.9;

export interface RigFrameOpts {
  /** true durante o dash/esquiva → squash & stretch */
  dashing?: boolean;
  /** alpha do corpo (pisca de i-frames) — espelhado no manto */
  alpha?: number;
}

export class CharacterRig {
  private readonly scene: Phaser.Scene;
  private readonly sprite: Phaser.GameObjects.Image;
  private readonly shadow: Phaser.GameObjects.Image;
  /** vazio quando quality === 'low' (gate de qualidade) */
  private readonly cloaks: Phaser.GameObjects.Image[] = [];

  /** metade da textura (px) — o bob é aplicado via displayOrigin */
  private readonly halfW: number;
  private readonly halfH: number;

  /** fase dessincronizada p/ os personagens não respirarem em uníssono */
  private t = Math.random() * 10000;
  private lastX: number;
  private lastY: number;
  private dirX = 1;
  private dirY = 0;
  private tilt = 0;
  private pulse = 1;

  constructor(
    scene: Phaser.Scene,
    sprite: Phaser.GameObjects.Image,
    cloakTint: number,
  ) {
    this.scene = scene;
    this.sprite = sprite;
    this.lastX = sprite.x;
    this.lastY = sprite.y;
    this.halfW = sprite.width / 2;
    this.halfH = sprite.height / 2;
    const d = sprite.depth;

    this.shadow = scene.add.image(sprite.x, sprite.y + SHADOW_OFF_Y, CHAR_TEX.SHADOW)
      .setDepth(d - 2)
      .setAlpha(SHADOW_ALPHA);

    // ponytail: gate simples — em 'low' o manto some, o resto é barato
    if (Settings.get().video.quality !== 'low') {
      for (let i = 0; i < 2; i++) {
        this.cloaks.push(
          scene.add.image(sprite.x, sprite.y + CLOAK_DROP, CHAR_TEX.CLOAK)
            .setDepth(d - 0.5 - i * 0.1)
            .setScale(CLOAK_BASE_SCALE * (1 - i * 0.25))
            .setTint(cloakTint)
            .setAlpha(CLOAK_ALPHA),
        );
      }
    }
  }

  /** pulso de escala (ex.: evolução do escudo) — decai sozinho no update */
  punch(scale = 1.2): void {
    this.pulse = Math.max(this.pulse, scale);
  }

  /** sombra + manto — p/ entrar nos tweens de morte/respawn da entidade */
  parts(): Phaser.GameObjects.Image[] {
    return [this.shadow, ...this.cloaks];
  }

  /** teleporte (respawn): manto/sombra não atravessam o mapa voando */
  snap(x: number, y: number): void {
    this.lastX = x;
    this.lastY = y;
    this.shadow.setPosition(x, y + SHADOW_OFF_Y);
    for (const c of this.cloaks) c.setPosition(x, y + CLOAK_DROP);
  }

  /** morte: esconde o manto e apaga a sombra devagar (corpo é da entidade) */
  setDead(): void {
    for (const c of this.cloaks) c.setVisible(false);
    this.scene.tweens.add({ targets: this.shadow, alpha: 0.15, duration: 600 });
  }

  /** aplica posição/escala/rotação do frame; (x, y) é a posição lógica */
  update(dtMs: number, x: number, y: number, opts?: RigFrameOpts): void {
    if (dtMs <= 0) return;
    this.t += dtMs;

    // movimento derivado do delta de posição
    const dx = x - this.lastX;
    const dy = y - this.lastY;
    this.lastX = x;
    this.lastY = y;
    const dist = Math.hypot(dx, dy);
    const speed = dist / dtMs;
    const moving = speed > MOVE_EPS;
    if (moving) {
      this.dirX = dx / dist;
      this.dirY = dy / dist;
    }

    // pose do frame
    let sx = 1;
    let sy = 1;
    let bobY = 0;
    if (opts?.dashing) {
      // estica no eixo dominante do deslocamento, achata o outro
      if (Math.abs(this.dirX) >= Math.abs(this.dirY)) {
        sx = 1 + DASH_STRETCH;
        sy = 1 - DASH_SQUASH;
      } else {
        sx = 1 - DASH_SQUASH;
        sy = 1 + DASH_STRETCH;
      }
    } else if (moving) {
      const ph = Math.abs(Math.sin(this.t * BOB_HZ));
      bobY = -ph * BOB_AMP;
      sy = 1 - (1 - ph) * STEP_SQUASH; // achata no apoio do passo
      sx = 1 + (1 - ph) * STEP_SQUASH * 0.6;
    } else {
      const br = Math.sin(this.t * BREATH_HZ);
      sy = 1 + br * BREATH_AMP;
      sx = 1 - br * BREATH_AMP * 0.4;
      bobY = -br * 0.6;
    }

    // tilt na direção (horizontal) do movimento
    const tiltTarget = (moving || opts?.dashing) ? this.dirX * TILT_RAD : 0;
    this.tilt += (tiltTarget - this.tilt) * Math.min(1, dtMs * TILT_LERP);

    // pulso (punch) decai até 1
    this.pulse = Math.max(1, this.pulse - dtMs * PULSE_DECAY);

    // bob via displayOrigin: desloca o DESENHO sem mover sprite.y — a câmera
    // que segue o sprite do player não balança junto com o passo
    const scaleY = CHAR_BASE_SCALE * sy * this.pulse;
    this.sprite.setPosition(x, y);
    this.sprite.setRotation(this.tilt);
    this.sprite.setScale(CHAR_BASE_SCALE * sx * this.pulse, scaleY);
    this.sprite.setDisplayOrigin(this.halfW, this.halfH - bobY / scaleY);

    // sombra fica no chão e encolhe quando o corpo sobe no bob
    const shrink = 1 + bobY * SHADOW_BOB_SHRINK;
    this.shadow.setPosition(x, y + SHADOW_OFF_Y);
    this.shadow.setScale(shrink);
    this.shadow.setAlpha(SHADOW_ALPHA); // tweens de morte/respawn não deixam resíduo

    // manto: 2 segmentos com inércia — o alvo fica atrás do movimento
    const alpha = opts?.alpha ?? 1;
    let tx = x - this.dirX * (moving ? CLOAK_TRAIL : 0);
    let ty = y - this.dirY * (moving ? CLOAK_TRAIL : 0) + CLOAK_DROP + bobY * 0.5;
    for (let i = 0; i < this.cloaks.length; i++) {
      const c = this.cloaks[i];
      const k = Math.min(1, dtMs * (i === 0 ? CLOAK_LERP_1 : CLOAK_LERP_2));
      c.x += (tx - c.x) * k;
      c.y += (ty - c.y) * k;
      c.setAlpha(CLOAK_ALPHA * alpha);
      // o 2º segmento persegue o 1º, um pouco mais atrás
      tx = c.x - this.dirX * 2;
      ty = c.y + 1;
    }
  }

  destroy(): void {
    this.scene.tweens.killTweensOf(this.shadow);
    this.shadow.destroy();
    for (const c of this.cloaks) c.destroy();
  }
}

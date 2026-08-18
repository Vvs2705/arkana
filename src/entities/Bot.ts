// ============================================================================
// ARKANA v0.1 — Bot de treino (raia D — Programador de Gameplay / IA)
// FSM de 4 estados: WANDER → CHASE → ATTACK, com FLEE_FIRE em prioridade
// máxima (DoD seção 15: "perseguem, atacam, reagem ao fogo").
// Todo número de balanceamento vem de BAL.bots; os números abaixo em TUNE
// são parâmetros de comportamento fixados pela spec da raia D.
// ============================================================================
import Phaser from 'phaser';
import { type ArenaApi, type EntityRef, Element, EVT } from '../core/types';
import { DEPTH, ELEMENT_COLORS, TILE, ARENA_COLS, ARENA_ROWS } from '../core/config';
import { BAL } from '../core/balance';
import { Audio } from '../core/audio';
import { ensureCharacterTextures, CHAR_TEX, CHAR_BASE_SCALE } from '../render/characterTextures';
import { CharacterRig } from '../render/characterRig';

type BotState = 'WANDER' | 'CHASE' | 'ATTACK' | 'FLEE_FIRE';

/** parâmetros de comportamento (spec da raia D — não são balanceamento) */
const TUNE = {
  /** raio do corpo p/ colisão contra células bloqueantes (~12px) */
  bodyRadius: 12,
  /** fração da velocidade em WANDER ("anda devagar") */
  wanderSpeedMult: 0.4,
  /** troca de direção aleatória a cada 1–2s */
  wanderDirMinMs: 1000,
  wanderDirMaxMs: 2000,
  /** CHASE → WANDER após 3s sem ver o player */
  loseSightMs: 3000,
  /** erro de mira de ±6° no ATTACK */
  aimErrorDeg: 6,
  /** fração da velocidade no ATTACK ("para/anda pouco" — strafe leve) */
  attackStrafeMult: 0.25,
  /** corre na direção oposta ao fogo por ~1s antes de reavaliar */
  fleeMs: 1000,
  /** intervalo do scan de fogo (prioridade máxima da FSM) */
  fireScanMs: 200,
  /** período de aplicação do hazard da própria célula (~250ms) */
  hazardTickMs: 250,
  /** respawn a ≥12 células do player */
  respawnMinCells: 12,
  /** projétil nasce à frente do corpo (evita autoacerto) */
  muzzleOffsetPx: 16,
  /** barrinha de vida flutuante */
  barW: 26,
  barH: 4,
  barOffsetY: 22,
} as const;

/** interpola dois 0xRRGGBB (p/ cor da barrinha de vida) */
function lerpColor(a: number, b: number, t: number): number {
  const k = Phaser.Math.Clamp(t, 0, 1);
  const ar = (a >> 16) & 0xff, ag = (a >> 8) & 0xff, ab = a & 0xff;
  const br = (b >> 16) & 0xff, bg = (b >> 8) & 0xff, bb = b & 0xff;
  const r = Math.round(ar + (br - ar) * k);
  const g = Math.round(ag + (bg - ag) * k);
  const bl = Math.round(ab + (bb - ab) * k);
  return (r << 16) | (g << 8) | bl;
}

export class Bot {
  readonly ref: EntityRef;

  private api: ArenaApi;
  private element: Element;
  private shield: number = BAL.bots.shield;
  private state: BotState = 'WANDER';

  private sprite: Phaser.GameObjects.Image;
  /** animação procedural (PRISMA-1 raia 3): respiração, bob, tilt, manto, sombra */
  private rig: CharacterRig;
  private barBg: Phaser.GameObjects.Rectangle;
  private barFill: Phaser.GameObjects.Rectangle;
  /** tint sutil (elemento lavado com branco) — restaurado após flash de dano */
  private baseTint: number;

  // WANDER
  private wanderDirX = 0;
  private wanderDirY = 0;
  private nextWanderChangeAt = 0;
  // CHASE
  private lastSeenAt = 0;
  // ATTACK
  private lastShotAt = 0;
  private strafePhase: number;
  // FLEE_FIRE
  private fleeDirX = 0;
  private fleeDirY = 0;
  private fleeUntil = 0;
  private fireScanAccum = 0;
  // hazard da célula
  private hazardAccum = 0;

  private respawnTimer: Phaser.Time.TimerEvent | null = null;
  private flashTimer: Phaser.Time.TimerEvent | null = null;
  private destroyed = false;

  constructor(api: ArenaApi, x: number, y: number, id: string, element: Element) {
    this.api = api;
    this.element = element;
    this.strafePhase = Math.random() * Math.PI * 2;

    this.ref = { x, y, hp: BAL.bots.hp, alive: true, isPlayer: false, id };

    // sprite 64px exibido no tamanho lógico (28px) — hitbox intacta.
    // Tint sutil da cor do elemento (mistura 40% elemento / 60% branco)
    // sobre base neutra: a identidade de elemento continua vindo do tint.
    ensureCharacterTextures(api.scene);
    const c = ELEMENT_COLORS[element];
    this.baseTint = lerpColor(0xffffff, c, 0.4);
    this.sprite = api.scene.add.image(x, y, CHAR_TEX.BOT)
      .setScale(CHAR_BASE_SCALE)
      .setDepth(DEPTH.ENTITY)
      .setTint(this.baseTint);
    // manto na cor cheia do elemento (escurecida) — reforça a identidade
    this.rig = new CharacterRig(api.scene, this.sprite, lerpColor(c, 0x0b1026, 0.45));

    // barrinha de vida flutuante: fundo escuro + preenchimento colorido
    this.barBg = api.scene.add.rectangle(
      x, y - TUNE.barOffsetY, TUNE.barW, TUNE.barH, 0x05070f, 0.75,
    ).setDepth(DEPTH.ENTITY + 1);
    this.barFill = api.scene.add.rectangle(
      x - TUNE.barW / 2 + 1, y - TUNE.barOffsetY, TUNE.barW - 2, TUNE.barH - 2, 0x35c94a, 1,
    ).setOrigin(0, 0.5).setDepth(DEPTH.ENTITY + 1);
  }

  // ------------------------------------------------------------------ update
  update(time: number, delta: number): void {
    if (this.destroyed || !this.ref.alive) return;

    const player = this.api.getPlayerRef();
    const dt = delta / 1000;

    // PRIORIDADE MÁXIMA: fogo por perto? (DoD: "reagem ao fogo")
    this.fireScanAccum += delta;
    if (this.state !== 'FLEE_FIRE' && this.fireScanAccum >= TUNE.fireScanMs) {
      this.fireScanAccum = 0;
      if (this.scanFireAndSetFleeDir()) {
        this.state = 'FLEE_FIRE';
        this.fleeUntil = time + TUNE.fleeMs;
      }
    }

    switch (this.state) {
      case 'FLEE_FIRE': this.doFlee(time, dt); break;
      case 'WANDER': this.doWander(time, dt, player); break;
      case 'CHASE': this.doChase(time, dt, player); break;
      case 'ATTACK': this.doAttack(time, dt, player); break;
    }

    // hazard do terreno na própria célula (bots morrem no fogo que pisam)
    this.hazardAccum += delta;
    if (this.hazardAccum >= TUNE.hazardTickMs) {
      this.hazardAccum -= TUNE.hazardTickMs;
      const cell = this.api.terrain.worldToCell(this.ref.x, this.ref.y);
      const dps = this.api.terrain.getHazard(cell.cx, cell.cy).dps;
      if (dps > 0) this.takeDamage(dps * (TUNE.hazardTickMs / 1000));
    }

    this.syncVisuals(delta);
  }

  // ------------------------------------------------------------------ estados
  private doWander(time: number, dt: number, player: EntityRef): void {
    if (time >= this.nextWanderChangeAt) {
      this.nextWanderChangeAt = time
        + Phaser.Math.Between(TUNE.wanderDirMinMs, TUNE.wanderDirMaxMs);
      if (Math.random() < 0.15) {
        // pequena pausa: fica parado até a próxima troca
        this.wanderDirX = 0;
        this.wanderDirY = 0;
      } else {
        const a = Math.random() * Math.PI * 2;
        this.wanderDirX = Math.cos(a);
        this.wanderDirY = Math.sin(a);
      }
    }
    const speed = BAL.bots.speed * TUNE.wanderSpeedMult * this.mySpeedMult();
    this.tryMove(this.wanderDirX * speed * dt, this.wanderDirY * speed * dt);

    if (player.alive && this.canSeePlayer(player)) {
      this.state = 'CHASE';
      this.lastSeenAt = time;
    }
  }

  private doChase(time: number, dt: number, player: EntityRef): void {
    if (!player.alive) { this.state = 'WANDER'; return; }

    if (this.canSeePlayer(player)) this.lastSeenAt = time;
    if (time - this.lastSeenAt > TUNE.loseSightMs) { this.state = 'WANDER'; return; }

    const dist = Phaser.Math.Distance.Between(this.ref.x, this.ref.y, player.x, player.y);
    if (dist <= BAL.bots.attackRange) { this.state = 'ATTACK'; return; }

    const a = Phaser.Math.Angle.Between(this.ref.x, this.ref.y, player.x, player.y);
    const speed = BAL.bots.speed * this.mySpeedMult();
    this.tryMove(Math.cos(a) * speed * dt, Math.sin(a) * speed * dt);
  }

  private doAttack(time: number, dt: number, player: EntityRef): void {
    if (!player.alive) { this.state = 'WANDER'; return; }

    const dist = Phaser.Math.Distance.Between(this.ref.x, this.ref.y, player.x, player.y);
    if (dist > BAL.bots.attackRange) {
      this.state = 'CHASE';
      this.lastSeenAt = time;
      return;
    }

    // "para/anda pouco": strafe perpendicular suave em torno do alvo
    const toPlayer = Phaser.Math.Angle.Between(this.ref.x, this.ref.y, player.x, player.y);
    const strafe = toPlayer + Math.PI / 2;
    const wobble = Math.sin(time / 400 + this.strafePhase);
    const speed = BAL.bots.speed * TUNE.attackStrafeMult * this.mySpeedMult();
    this.tryMove(Math.cos(strafe) * wobble * speed * dt, Math.sin(strafe) * wobble * speed * dt);

    // dispara a cada fireRateMs com erro de ±6°
    if (time - this.lastShotAt >= BAL.bots.fireRateMs) {
      this.lastShotAt = time;
      this.shootAt(player);
    }
  }

  private doFlee(time: number, dt: number): void {
    const speed = BAL.bots.speed * this.mySpeedMult();
    this.tryMove(this.fleeDirX * speed * dt, this.fleeDirY * speed * dt);
    if (time >= this.fleeUntil) {
      // reavalia: se ainda houver fogo perto, o scan re-dispara FLEE_FIRE;
      // senão a FSM retoma pelo WANDER (que enxerga o player e vira CHASE).
      this.state = 'WANDER';
      this.nextWanderChangeAt = 0;
      this.fireScanAccum = TUNE.fireScanMs;
    }
  }

  // ------------------------------------------------------------------ percepção
  /** raio de visão reduzido quando o player está escondido em grama alta */
  private canSeePlayer(player: EntityRef): boolean {
    const cell = this.api.terrain.worldToCell(player.x, player.y);
    const radius = this.api.terrain.isConcealing(cell.cx, cell.cy)
      ? BAL.bots.sightConcealed
      : BAL.bots.sightRadius;
    return Phaser.Math.Distance.Between(this.ref.x, this.ref.y, player.x, player.y) <= radius;
  }

  /**
   * Varre células a até fleeRadiusCells; se houver hazard (fogo/eletricidade),
   * define a direção de fuga oposta ao centro de massa do perigo.
   */
  private scanFireAndSetFleeDir(): boolean {
    const t = this.api.terrain;
    const me = t.worldToCell(this.ref.x, this.ref.y);
    const r = BAL.bots.fleeRadiusCells;
    let sumX = 0, sumY = 0, found = 0;
    for (let dy = -r; dy <= r; dy++) {
      for (let dx = -r; dx <= r; dx++) {
        const cx = me.cx + dx, cy = me.cy + dy;
        if (cx < 0 || cy < 0 || cx >= t.cols || cy >= t.rows) continue;
        if (t.getHazard(cx, cy).dps > 0) {
          const w = t.cellToWorld(cx, cy);
          sumX += w.x - this.ref.x;
          sumY += w.y - this.ref.y;
          found++;
        }
      }
    }
    if (found === 0) return false;
    const len = Math.hypot(sumX, sumY);
    if (len < 1) {
      // fogo debaixo dos pés: foge numa direção aleatória
      const a = Math.random() * Math.PI * 2;
      this.fleeDirX = Math.cos(a);
      this.fleeDirY = Math.sin(a);
    } else {
      this.fleeDirX = -sumX / len;
      this.fleeDirY = -sumY / len;
    }
    return true;
  }

  // ------------------------------------------------------------------ movimento
  private mySpeedMult(): number {
    const c = this.api.terrain.worldToCell(this.ref.x, this.ref.y);
    return this.api.terrain.getSpeedMult(c.cx, c.cy);
  }

  /**
   * Passo por eixo testando isBlocking na borda do corpo (~12px) —
   * sem arcade collider, conforme o contrato do terreno (types.ts).
   */
  private tryMove(dx: number, dy: number): void {
    const t = this.api.terrain;
    const r = TUNE.bodyRadius;
    const maxX = ARENA_COLS * TILE - r;
    const maxY = ARENA_ROWS * TILE - r;

    if (dx !== 0) {
      const nx = Phaser.Math.Clamp(this.ref.x + dx, r, maxX);
      const edge = nx + Math.sign(dx) * r;
      const c1 = t.worldToCell(edge, this.ref.y - r * 0.6);
      const c2 = t.worldToCell(edge, this.ref.y + r * 0.6);
      if (!t.isBlocking(c1.cx, c1.cy) && !t.isBlocking(c2.cx, c2.cy)) this.ref.x = nx;
    }
    if (dy !== 0) {
      const ny = Phaser.Math.Clamp(this.ref.y + dy, r, maxY);
      const edge = ny + Math.sign(dy) * r;
      const c1 = t.worldToCell(this.ref.x - r * 0.6, edge);
      const c2 = t.worldToCell(this.ref.x + r * 0.6, edge);
      if (!t.isBlocking(c1.cx, c1.cy) && !t.isBlocking(c2.cx, c2.cy)) this.ref.y = ny;
    }
  }

  // ------------------------------------------------------------------ combate
  private shootAt(player: EntityRef): void {
    const base = Phaser.Math.Angle.Between(this.ref.x, this.ref.y, player.x, player.y);
    const err = Phaser.Math.DegToRad(
      Phaser.Math.FloatBetween(-TUNE.aimErrorDeg, TUNE.aimErrorDeg),
    );
    const angle = base + err;
    this.api.spawnProjectile({
      x: this.ref.x + Math.cos(angle) * TUNE.muzzleOffsetPx,
      y: this.ref.y + Math.sin(angle) * TUNE.muzzleOffsetPx,
      angle,
      element: this.element,
      fromPlayer: false,
      damage: BAL.bots.dmg,
      speed: BAL.bots.projSpeed,
      radius: BAL.bots.projRadius,
    });
  }

  /** dano recebido (chamado pela ArenaScene e pelo hazard do terreno) */
  takeDamage(amount: number): void {
    if (this.destroyed || !this.ref.alive || amount <= 0) return;

    // escudo absorve primeiro
    const absorbed = Math.min(this.shield, amount);
    this.shield -= absorbed;
    const rest = amount - absorbed;
    this.ref.hp = Math.max(0, this.ref.hp - rest);

    this.api.scene.events.emit(EVT.DAMAGE_NUMBER, {
      x: this.ref.x,
      y: this.ref.y - TUNE.barOffsetY,
      amount,
    });

    // flash branco curto de feedback
    this.sprite.setTintFill(0xffffff);
    this.flashTimer?.remove(false);
    this.flashTimer = this.api.scene.time.delayedCall(70, () => {
      if (!this.destroyed && this.ref.alive) this.sprite.setTint(this.baseTint);
    });

    if (this.ref.hp <= 0) this.die();
  }

  private die(): void {
    this.ref.alive = false;
    this.ref.hp = 0;
    Audio.playSfx('bot_die');
    this.api.scene.events.emit(EVT.BOT_DIED, this.ref.id);

    this.api.scene.tweens.add({
      targets: [this.sprite, this.barBg, this.barFill, ...this.rig.parts()],
      alpha: 0,
      duration: 400,
      ease: 'Cubic.easeOut',
    });

    // respawn sozinho após respawnMs, longe do player
    this.respawnTimer?.remove(false);
    this.respawnTimer = this.api.scene.time.delayedCall(
      BAL.bots.respawnMs,
      () => this.respawn(),
    );
  }

  /** renasce em célula andável a ≥12 células do player (busca aleatória) */
  private respawn(): void {
    if (this.destroyed) return;
    const t = this.api.terrain;
    const player = this.api.getPlayerRef();
    const pc = t.worldToCell(player.x, player.y);

    let cx = -1, cy = -1;
    let fallbackX = -1, fallbackY = -1;
    for (let i = 0; i < 400; i++) {
      const rx = Phaser.Math.Between(1, t.cols - 2);
      const ry = Phaser.Math.Between(1, t.rows - 2);
      if (t.isBlocking(rx, ry)) continue;
      if (fallbackX < 0) { fallbackX = rx; fallbackY = ry; }
      const distCells = Phaser.Math.Distance.Between(rx, ry, pc.cx, pc.cy);
      if (distCells >= TUNE.respawnMinCells) { cx = rx; cy = ry; break; }
    }
    if (cx < 0) { cx = fallbackX; cy = fallbackY; }
    if (cx < 0) { cx = Math.floor(t.cols / 2); cy = Math.floor(t.rows / 2); }

    const w = t.cellToWorld(cx, cy);
    this.ref.x = w.x;
    this.ref.y = w.y;
    this.ref.hp = BAL.bots.hp;
    this.ref.alive = true;
    this.shield = BAL.bots.shield;
    this.state = 'WANDER';
    this.nextWanderChangeAt = 0;
    this.hazardAccum = 0;
    this.fireScanAccum = 0;

    this.sprite.setTint(this.baseTint);
    this.rig.snap(this.ref.x, this.ref.y); // manto/sombra não voam pelo mapa
    this.syncVisuals(16);
    this.api.scene.tweens.add({
      targets: [this.sprite, this.barBg, this.barFill, ...this.rig.parts()],
      alpha: 1,
      duration: 300,
    });
  }

  // ------------------------------------------------------------------ visual
  private syncVisuals(delta: number): void {
    // rig cuida de posição + respiração/bob/tilt/manto/sombra (deriva o
    // movimento do delta de posição — a FSM não precisa informar nada)
    this.rig.update(delta, this.ref.x, this.ref.y);
    const frac = Phaser.Math.Clamp(this.ref.hp / BAL.bots.hp, 0, 1);
    this.barBg.setPosition(this.ref.x, this.ref.y - TUNE.barOffsetY);
    this.barFill.setPosition(this.ref.x - TUNE.barW / 2 + 1, this.ref.y - TUNE.barOffsetY);
    // origem (0, 0.5): scaleX encolhe a barra a partir da esquerda
    this.barFill.scaleX = frac;
    // verde → vermelho conforme a vida cai
    this.barFill.setFillStyle(lerpColor(0xe04040, 0x35c94a, frac), 1);
  }

  destroy(): void {
    if (this.destroyed) return;
    this.destroyed = true;
    this.respawnTimer?.remove(false);
    this.respawnTimer = null;
    this.flashTimer?.remove(false);
    this.flashTimer = null;
    this.api.scene.tweens.killTweensOf([this.sprite, this.barBg, this.barFill, ...this.rig.parts()]);
    this.rig.destroy();
    this.sprite.destroy();
    this.barBg.destroy();
    this.barFill.destroy();
  }
}

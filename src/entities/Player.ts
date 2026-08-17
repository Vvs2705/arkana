// ============================================================================
// ARKANA v0.1 — mago jogável (raia C: gameplay)
// Movimento 8 direções com colisão manual por eixo (sem arcade), mira no
// mouse via "cajado", ataque básico/tática, esquiva com i-frames, troca de
// elemento, mana, hazards do terreno e o Escudo de Magia Evolutivo (GDD §5).
// ============================================================================
import Phaser from 'phaser';
import { Element, ELEMENTS, EntityRef, EVT, PlayerStats } from '../core/types';
import { DEPTH, ELEMENT_COLORS, TEX } from '../core/config';
import { BAL } from '../core/balance';
import { Settings, type KeyAction } from '../core/settings';
import { Audio } from '../core/audio';
import type { ArenaScene } from '../scenes/ArenaScene';

// --- constantes de sensação da raia C (briefing) ----------------------------
// Números de BALANCEAMENTO vêm SEMPRE de BAL — aqui só game feel/varredura.
/** raio do corpo p/ colisão com terreno (briefing: ~12px) */
const BODY_RADIUS_PX = 12;
/** cadência de aplicação do hazard de terreno (briefing: ~250ms) */
const HAZARD_TICK_MS = 250;
/** comprimento do cajado (linha de mira) e ponto de saída dos projéteis */
const STAFF_LEN_PX = 20;
/** duração do flash vermelho ao tomar dano no HP */
const FLASH_MS = 90;
/** screen shake sutil ao tomar dano (briefing: ~80ms) */
const SHAKE_MS = 80;
const SHAKE_INTENSITY = 0.004;
/** período do pisca-pisca de alpha durante i-frames */
const BLINK_MS = 70;
/** intervalo mínimo entre "cliques secos" (sem mana) */
const DRY_CLICK_MS = 250;
/** passo da varredura do dash contra células bloqueantes */
const DASH_STEP_PX = 6;
/** recuo do sprite ao disparar (px) e decaimento (px/ms) */
const RECOIL_BASIC_PX = 3;
const RECOIL_TACTIC_PX = 5;
const RECOIL_DECAY = 0.05;
/** clamp de delta p/ aba inativa */
const MAX_STEP_MS = 100;

export class Player {
  /** referência leve consumida por bots/HUD/pool (contrato EntityRef) */
  readonly ref: EntityRef;
  /** exposto p/ a câmera da ArenaScene seguir */
  readonly sprite: Phaser.GameObjects.Image;

  private readonly scene: ArenaScene;
  private readonly staff: Phaser.GameObjects.Image;

  private x: number;
  private y: number;
  private alive = true;
  private element: Element = Element.FIRE;

  // recursos
  private hp: number = BAL.player.hp;
  private mana: number = BAL.player.manaMax;

  // Escudo de Magia Evolutivo (GDD §5): nível 1..4, evolui com dano CAUSADO
  private shieldLevel = 1;
  private shield: number = BAL.shield.caps[0];
  private evoTotal = 0;

  // timers em ms (decrementados no update — imunes a pausa por construção)
  private fireCd = 0;
  private tacticCd = 0;
  private dodgeCd = 0;
  private dashLeft = 0;
  private iframesLeft = 0;
  private flashLeft = 0;
  private dryClickCd = 0;
  private hazardAcc = 0;
  private recoil = 0;

  // direções
  private aimAngle = 0;
  private lastMoveX = 1;
  private lastMoveY = 0;
  private dashDirX = 0;
  private dashDirY = 0;

  /**
   * Teclas: Settings guarda KeyboardEvent.code ('KeyW', 'Space'...), que NÃO
   * casa com Phaser.KeyCodes ('W', 'SPACE') — addKey('KeyW') falharia mudo.
   * Por isso lemos eventos crus e comparamos ev.code com Settings.key(...):
   * o remapeamento vale imediatamente; no onChange só limpamos estado preso.
   */
  private keysDown = new Set<string>();
  private readonly unsubSettings: () => void;

  private readonly onKeyDown = (ev: KeyboardEvent): void => {
    this.keysDown.add(ev.code);
    if (ev.repeat || !this.alive) return;
    if (ev.code === Settings.key('dodge')) this.tryDodge();
    else if (ev.code === Settings.key('element')) this.cycleElement();
  };

  private readonly onKeyUp = (ev: KeyboardEvent): void => {
    this.keysDown.delete(ev.code);
  };

  /** janela perdeu foco → nenhum keyup chega; solta tudo */
  private readonly onBlur = (): void => {
    this.keysDown.clear();
  };

  constructor(scene: ArenaScene, x: number, y: number) {
    this.scene = scene;
    this.x = x;
    this.y = y;

    this.sprite = scene.add.image(x, y, TEX.PLAYER).setDepth(DEPTH.PLAYER);
    // cajado: linha curta que aponta p/ o mouse; o tint dobra como
    // indicador do elemento atual (acessibilidade: cor + HUD)
    this.staff = scene.add.image(x, y, TEX.PIXEL)
      .setOrigin(0, 0.5)
      .setDisplaySize(STAFF_LEN_PX, 3)
      .setDepth(DEPTH.PLAYER)
      .setTint(ELEMENT_COLORS[this.element]);

    this.ref = { x, y, hp: this.hp, alive: true, isPlayer: true, id: 'player' };

    const kb = scene.input.keyboard;
    kb?.on('keydown', this.onKeyDown);
    kb?.on('keyup', this.onKeyUp);
    scene.game.events.on(Phaser.Core.Events.BLUR, this.onBlur);
    this.unsubSettings = Settings.onChange(() => this.keysDown.clear());
  }

  // ----------------------------------------------------------------- update
  update(_time: number, delta: number): void {
    if (!this.alive) {
      this.syncRef();
      return;
    }
    const dtMs = Math.min(delta, MAX_STEP_MS);
    const dt = dtMs / 1000;
    this.tickTimers(dtMs);

    // mira: worldX/Y só atualiza no movimento do mouse — com câmera seguindo
    // o player é preciso reprojetar todo frame
    const p = this.scene.input.activePointer;
    p.updateWorldPoint(this.scene.cameras.main);
    this.aimAngle = Math.atan2(p.worldY - this.y, p.worldX - this.x);

    // movimento: dash tem prioridade sobre o andar
    if (this.dashLeft > 0) this.dashStep(dtMs);
    else this.walk(dt);

    // hazard do terreno em ticks fixos (fogo queima, água eletrizada choca);
    // passa pelo takeDamage normal → escudo absorve e i-frames protegem
    this.hazardAcc += dtMs;
    while (this.hazardAcc >= HAZARD_TICK_MS) {
      this.hazardAcc -= HAZARD_TICK_MS;
      const cell = this.scene.terrain.worldToCell(this.x, this.y);
      const hz = this.scene.terrain.getHazard(cell.cx, cell.cy);
      if (hz.dps > 0) this.takeDamage(hz.dps * (HAZARD_TICK_MS / 1000));
      if (!this.alive) break;
    }
    if (!this.alive) {
      this.syncRef();
      return;
    }

    // mana regenera devagar (GDD regra global 2)
    this.mana = Math.min(BAL.player.manaMax, this.mana + BAL.player.manaRegen * dt);

    // ataques (botões seguráveis)
    if (p.leftButtonDown()) this.tryBasic();
    if (p.rightButtonDown()) this.tryTactic();

    this.updateVisuals();
    this.syncRef();
  }

  private tickTimers(dtMs: number): void {
    this.fireCd = Math.max(0, this.fireCd - dtMs);
    this.tacticCd = Math.max(0, this.tacticCd - dtMs);
    this.dodgeCd = Math.max(0, this.dodgeCd - dtMs);
    this.iframesLeft = Math.max(0, this.iframesLeft - dtMs);
    this.flashLeft = Math.max(0, this.flashLeft - dtMs);
    this.dryClickCd = Math.max(0, this.dryClickCd - dtMs);
    this.recoil = Math.max(0, this.recoil - dtMs * RECOIL_DECAY);
  }

  // -------------------------------------------------------------- movimento
  private walk(dt: number): void {
    let mx = 0;
    let my = 0;
    const held = (a: KeyAction): boolean => this.keysDown.has(Settings.key(a));
    if (held('left')) mx -= 1;
    if (held('right')) mx += 1;
    if (held('up')) my -= 1;
    if (held('down')) my += 1;
    if (mx === 0 && my === 0) return;

    const inv = 1 / Math.hypot(mx, my);
    mx *= inv;
    my *= inv;
    this.lastMoveX = mx;
    this.lastMoveY = my;

    const cell = this.scene.terrain.worldToCell(this.x, this.y);
    const v = BAL.player.speed * this.scene.terrain.getSpeedMult(cell.cx, cell.cy);
    this.tryMove(mx * v * dt, my * v * dt);
  }

  /**
   * Colisão manual por eixo (sem arcade collider): tenta X, depois Y,
   * testando os 2 cantos da borda de avanço contra terrain.isBlocking.
   * Mover por eixo separado dá o "deslizar" natural ao raspar paredes.
   */
  private tryMove(dx: number, dy: number): void {
    const t = this.scene.terrain;
    const r = BODY_RADIUS_PX;
    const corner = r * 0.7;
    const maxX = t.cols * t.tileSize - r;
    const maxY = t.rows * t.tileSize - r;

    if (dx !== 0) {
      const nx = Phaser.Math.Clamp(this.x + dx, r, maxX);
      const edge = nx + Math.sign(dx) * r;
      if (!this.blockedAt(edge, this.y - corner) && !this.blockedAt(edge, this.y + corner)) {
        this.x = nx;
      }
    }
    if (dy !== 0) {
      const ny = Phaser.Math.Clamp(this.y + dy, r, maxY);
      const edge = ny + Math.sign(dy) * r;
      if (!this.blockedAt(this.x - corner, edge) && !this.blockedAt(this.x + corner, edge)) {
        this.y = ny;
      }
    }
  }

  private blockedAt(wx: number, wy: number): boolean {
    const { cx, cy } = this.scene.terrain.worldToCell(wx, wy);
    return this.scene.terrain.isBlocking(cx, cy);
  }

  /** dash em passos curtos — NÃO atravessa células bloqueantes */
  private dashStep(dtMs: number): void {
    const speed = BAL.player.dodge.dist / BAL.player.dodge.durationMs; // px/ms
    const useMs = Math.min(dtMs, this.dashLeft);
    this.dashLeft = Math.max(0, this.dashLeft - dtMs);

    let dist = speed * useMs;
    while (dist > 0) {
      const step = Math.min(DASH_STEP_PX, dist);
      const px = this.x;
      const py = this.y;
      this.tryMove(this.dashDirX * step, this.dashDirY * step);
      if (this.x === px && this.y === py) {
        // parede: interrompe o dash no último ponto válido
        this.dashLeft = 0;
        break;
      }
      dist -= step;
    }
  }

  // ---------------------------------------------------------------- ataques
  private tryBasic(): void {
    if (this.fireCd > 0) return;
    const spec = BAL.elements[this.element].basic;
    if (this.mana < spec.manaCost) {
      // clique "seco": feedback discreto, nada dispara
      if (this.dryClickCd <= 0) {
        Audio.playSfx('ui_back');
        this.dryClickCd = DRY_CLICK_MS;
      }
      return;
    }
    this.mana -= spec.manaCost;
    this.fireCd = spec.fireRateMs;
    this.shoot(spec.dmg, spec.speed, spec.radius, false);
  }

  private tryTactic(): void {
    if (this.tacticCd > 0) return;
    const spec = BAL.elements[this.element].tactic;
    this.tacticCd = spec.cooldownMs;
    this.shoot(spec.dmg, spec.speed, spec.radius, true);
  }

  private shoot(damage: number, speed: number, radius: number, strong: boolean): void {
    // sai da ponta do cajado (o sfx de conjuração toca no pool, fromPlayer)
    const tx = this.x + Math.cos(this.aimAngle) * STAFF_LEN_PX;
    const ty = this.y + Math.sin(this.aimAngle) * STAFF_LEN_PX;
    this.scene.spawnProjectile({
      x: tx,
      y: ty,
      angle: this.aimAngle,
      element: this.element,
      fromPlayer: true,
      damage,
      speed,
      radius,
      strong,
    });
    this.recoil = strong ? RECOIL_TACTIC_PX : RECOIL_BASIC_PX;
  }

  // ---------------------------------------------------------------- esquiva
  private tryDodge(): void {
    if (!this.alive || this.dodgeCd > 0 || this.dashLeft > 0) return;
    // direção do movimento; parado, esquiva na direção da mira
    const moving = this.keysDownHasMovement();
    if (moving) {
      this.dashDirX = this.lastMoveX;
      this.dashDirY = this.lastMoveY;
    } else {
      this.dashDirX = Math.cos(this.aimAngle);
      this.dashDirY = Math.sin(this.aimAngle);
    }
    this.dashLeft = BAL.player.dodge.durationMs;
    this.iframesLeft = BAL.player.dodge.iframesMs;
    this.dodgeCd = BAL.player.dodge.cooldownMs;
    Audio.playSfx('dodge');
  }

  private keysDownHasMovement(): boolean {
    return (['up', 'down', 'left', 'right'] as KeyAction[])
      .some((a) => this.keysDown.has(Settings.key(a)));
  }

  // --------------------------------------------------------------- elemento
  private cycleElement(): void {
    const i = ELEMENTS.indexOf(this.element);
    this.element = ELEMENTS[(i + 1) % ELEMENTS.length];
    this.staff.setTint(ELEMENT_COLORS[this.element]);
    Audio.playSfx('element_switch');
    this.scene.events.emit(EVT.ELEMENT_CHANGED, this.element);
  }

  // ------------------------------------------------ Escudo de Magia Evolutivo
  /** acumula dano CAUSADO pelo jogador; cruzar o limiar evolui o escudo */
  addEvoProgress(dmg: number): void {
    if (!this.alive) return;
    this.evoTotal += dmg;
    // thresholds[nível-1] = dano acumulado p/ ESTAR no nível → o próximo
    // nível exige thresholds[shieldLevel]; while cobre saltos de dano altos
    while (
      this.shieldLevel < BAL.shield.caps.length &&
      this.evoTotal >= BAL.shield.thresholds[this.shieldLevel]
    ) {
      this.shieldLevel++;
      this.shield = BAL.shield.caps[this.shieldLevel - 1]; // ENCHE no novo cap
      Audio.playSfx('shield_up');
      this.scene.events.emit(EVT.SHIELD_EVOLVED, this.shieldLevel);
      // pulso visual curto — o momento precisa ser sentido (GDD §5)
      this.scene.tweens.add({
        targets: this.sprite,
        scale: 1.18,
        duration: 120,
        yoyo: true,
        ease: 'Quad.easeOut',
      });
    }
  }

  // ------------------------------------------------------------------- dano
  takeDamage(amount: number): void {
    if (!this.alive || amount <= 0) return;
    if (this.iframesLeft > 0) return; // esquiva perfeita

    let rest = amount;
    if (this.shield > 0) {
      const absorbed = Math.min(this.shield, rest);
      this.shield -= absorbed;
      rest -= absorbed;
      Audio.playSfx('hit_shield');
    }
    if (rest > 0) {
      this.hp = Math.max(0, this.hp - rest);
      Audio.playSfx('player_hurt');
      this.flashLeft = FLASH_MS;
      // shake sutil: dano no HP precisa doer mais que no escudo
      this.scene.cameras.main.shake(SHAKE_MS, SHAKE_INTENSITY);
    }
    this.scene.events.emit(EVT.PLAYER_DAMAGED, amount);
    if (this.hp <= 0) this.die();
  }

  private die(): void {
    this.alive = false;
    this.hp = 0;
    this.dashLeft = 0;
    this.keysDown.clear();
    this.staff.setVisible(false);
    this.sprite.clearTint();
    // tomba: gira e apaga — o overlay de morte é responsabilidade do HUD
    this.scene.tweens.add({
      targets: this.sprite,
      angle: 90,
      alpha: 0.25,
      duration: 600,
      ease: 'Quad.easeOut',
    });
    this.syncRef();
    this.scene.events.emit(EVT.PLAYER_DIED);
  }

  // ------------------------------------------------------------------ visual
  private updateVisuals(): void {
    // recuo sutil oposto à mira ao disparar
    const ox = -Math.cos(this.aimAngle) * this.recoil;
    const oy = -Math.sin(this.aimAngle) * this.recoil;
    this.sprite.setPosition(this.x + ox, this.y + oy);
    this.staff.setPosition(this.x, this.y);
    this.staff.setRotation(this.aimAngle);

    // flash de dano tem prioridade visual sobre tudo
    if (this.flashLeft > 0) this.sprite.setTintFill(0xff5050);
    else this.sprite.clearTint();

    // pisca de alpha durante i-frames
    if (this.iframesLeft > 0) {
      this.sprite.setAlpha(Math.floor(this.iframesLeft / BLINK_MS) % 2 === 0 ? 0.35 : 0.9);
    } else {
      this.sprite.setAlpha(1);
    }
  }

  private syncRef(): void {
    this.ref.x = this.x;
    this.ref.y = this.y;
    this.ref.hp = this.hp;
    this.ref.alive = this.alive;
  }

  // ------------------------------------------------------------------- stats
  getStats(): PlayerStats {
    return {
      hp: this.hp,
      maxHp: BAL.player.hp,
      shield: this.shield,
      shieldMax: BAL.shield.caps[this.shieldLevel - 1],
      shieldLevel: this.shieldLevel,
      evoProgress: this.evoTotal,
      evoNext: this.shieldLevel < BAL.shield.caps.length
        ? BAL.shield.thresholds[this.shieldLevel]
        : -1,
      mana: this.mana,
      manaMax: BAL.player.manaMax,
      element: this.element,
      tacticCdRemaining: this.tacticCd,
      tacticCdTotal: BAL.elements[this.element].tactic.cooldownMs,
      dodgeCdRemaining: this.dodgeCd,
      dodgeCdTotal: BAL.player.dodge.cooldownMs,
    };
  }

  /** chamado pela ArenaScene ao acordar da pausa — solta teclas presas */
  clearInputState(): void {
    this.keysDown.clear();
  }

  destroy(): void {
    const kb = this.scene.input.keyboard;
    kb?.off('keydown', this.onKeyDown);
    kb?.off('keyup', this.onKeyUp);
    this.scene.game.events.off(Phaser.Core.Events.BLUR, this.onBlur);
    this.unsubSettings();
    this.scene.tweens.killTweensOf(this.sprite);
    this.sprite.destroy();
    this.staff.destroy();
  }
}

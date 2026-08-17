// ============================================================================
// ARKANA v0.1 — Dummy de treino (raia D)
// Imóvel, hp altíssimo (BAL.dummy.hp), mostra números de dano via EVT e
// exibe uma janelinha de DPS que zera após resetAfterIdleMs sem dano.
// ============================================================================
import Phaser from 'phaser';
import { type ArenaApi, type EntityRef, EVT } from '../core/types';
import { TEX, DEPTH, COLORS, FONTS } from '../core/config';
import { BAL } from '../core/balance';
import { t } from '../core/strings';

/** parâmetros visuais fixos da spec da raia D */
const TUNE = {
  labelOffsetY: 32,
  dpsOffsetX: 30,
  dpsOffsetY: -10,
  wobbleDeg: 10,
  wobbleMs: 70,
} as const;

export class Dummy {
  readonly ref: EntityRef;

  private api: ArenaApi;
  private sprite: Phaser.GameObjects.Image;
  private label: Phaser.GameObjects.Text;
  private dpsText: Phaser.GameObjects.Text;

  /** janela de DPS: dano acumulado desde o primeiro hit da janela */
  private accum = 0;
  private windowStartMs = 0;
  private lastHitAtMs = -Infinity;
  private wobbleTween: Phaser.Tweens.Tween | null = null;
  private destroyed = false;

  constructor(api: ArenaApi, x: number, y: number) {
    this.api = api;
    this.ref = { x, y, hp: BAL.dummy.hp, alive: true, isPlayer: false, id: 'dummy' };

    this.sprite = api.scene.add.image(x, y, TEX.DUMMY).setDepth(DEPTH.ENTITY);

    // rótulo pequeno acima
    this.label = api.scene.add.text(x, y - TUNE.labelOffsetY, t('hud.dummy'), {
      fontFamily: FONTS.ui,
      fontSize: '12px',
      color: '#e8e6f0',
    }).setOrigin(0.5).setAlpha(0.7).setDepth(DEPTH.ENTITY + 1);

    // janelinha de DPS ao lado (aparece só enquanto recebe dano)
    this.dpsText = api.scene.add.text(
      x + TUNE.dpsOffsetX, y + TUNE.dpsOffsetY, '', {
        fontFamily: FONTS.ui,
        fontSize: '13px',
        color: '#' + COLORS.gold.toString(16).padStart(6, '0'),
      },
    ).setOrigin(0, 0.5).setDepth(DEPTH.ENTITY + 1).setVisible(false);
  }

  update(time: number, _delta: number): void {
    if (this.destroyed) return;
    // sem dano por resetAfterIdleMs → zera a janela e esconde o texto
    if (this.accum > 0 && time - this.lastHitAtMs > BAL.dummy.resetAfterIdleMs) {
      this.accum = 0;
      this.ref.hp = BAL.dummy.hp;
      this.dpsText.setVisible(false);
    }
  }

  takeDamage(amount: number): void {
    if (this.destroyed || amount <= 0) return;

    // hp altíssimo: nunca "morre" — clampa em 1 por segurança
    this.ref.hp = Math.max(1, this.ref.hp - amount);

    this.api.scene.events.emit(EVT.DAMAGE_NUMBER, {
      x: this.ref.x,
      y: this.ref.y - TUNE.labelOffsetY + 10,
      amount,
    });

    // janela de DPS
    const now = this.api.scene.time.now;
    if (this.accum === 0) this.windowStartMs = now;
    this.accum += amount;
    this.lastHitAtMs = now;
    const secs = Math.max((now - this.windowStartMs) / 1000, 1);
    this.dpsText.setText('DPS: ' + Math.round(this.accum / secs));
    this.dpsText.setVisible(true);

    // balança: tween de angle ±10°
    this.wobbleTween?.stop();
    this.sprite.setAngle(-TUNE.wobbleDeg);
    this.wobbleTween = this.api.scene.tweens.add({
      targets: this.sprite,
      angle: TUNE.wobbleDeg,
      duration: TUNE.wobbleMs,
      yoyo: true,
      repeat: 1,
      ease: 'Sine.easeInOut',
      onComplete: () => { if (!this.destroyed) this.sprite.setAngle(0); },
    });
  }

  destroy(): void {
    if (this.destroyed) return;
    this.destroyed = true;
    this.wobbleTween?.stop();
    this.wobbleTween = null;
    this.api.scene.tweens.killTweensOf(this.sprite);
    this.sprite.destroy();
    this.label.destroy();
    this.dpsText.destroy();
  }
}

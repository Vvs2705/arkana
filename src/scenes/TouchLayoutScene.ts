// ============================================================================
// ARKANA Fase 2 — "Editar layout" dos controles de toque (GDD 19.3)
// Aberta de Configurações › Controles. Mostra os controles na escala atual;
// arrastar reposiciona (o TouchControls em modo edição salva em Settings a
// cada dragend). "Restaurar padrão" volta às posições originais.
// ============================================================================
import Phaser from 'phaser';
import { GAME_WIDTH, GAME_HEIGHT, SCENE, FONTS, COLORS, defaultTouchLayout } from '../core/config';
import { t } from '../core/strings';
import { Settings } from '../core/settings';
import { Audio } from '../core/audio';
import { TouchControls } from '../ui/TouchControls';

const css = (n: number): string => `#${n.toString(16).padStart(6, '0')}`;

export class TouchLayoutScene extends Phaser.Scene {
  /** cena que abriu as Configurações (Menu ou Pause) — repassada no retorno */
  private from: string = SCENE.MENU;
  private touch: TouchControls | null = null;

  constructor() { super(SCENE.TOUCH_LAYOUT); }

  init(data: { from?: string } = {}): void {
    this.from = data.from ?? SCENE.MENU;
    this.touch = null;
  }

  create(): void {
    const cx = GAME_WIDTH / 2;
    this.cameras.main.setBackgroundColor(css(COLORS.night));

    this.add.text(cx, 44, t('touch.edit.title'), {
      fontFamily: FONTS.title, fontSize: '32px', color: css(COLORS.gold), letterSpacing: 8,
    }).setOrigin(0.5);
    this.add.text(cx, 82, t('touch.edit.hint'), {
      fontFamily: FONTS.body, fontSize: '15px', color: css(COLORS.textDim),
    }).setOrigin(0.5);

    // botões no centro — as bordas/cantos pertencem aos controles editáveis
    this.makeButton(cx, GAME_HEIGHT / 2 - 20, t('settings.restore'), css(COLORS.textDim), () => {
      const cur = Settings.get().controls.touch;
      Settings.update({
        controls: { ...Settings.get().controls, touch: { ...cur, layout: defaultTouchLayout() } },
      });
      this.buildControls(); // re-renderiza nas posições padrão
    });
    this.makeButton(cx, GAME_HEIGHT / 2 + 36, t('touch.edit.done'), css(COLORS.gold), () => this.goBack());

    this.input.keyboard?.on('keydown-ESC', () => this.goBack());
    this.events.once(Phaser.Scenes.Events.SHUTDOWN, () => {
      this.touch?.destroy();
      this.touch = null;
    });

    this.buildControls();
  }

  private buildControls(): void {
    this.touch?.destroy();
    this.touch = new TouchControls(this, null, { edit: true });
  }

  private goBack(): void {
    Audio.playSfx('ui_back');
    this.scene.start(SCENE.SETTINGS, { from: this.from, tab: 2 });
  }

  private makeButton(x: number, y: number, label: string, base: string, onClick: () => void): void {
    const txt = this.add.text(x, y, label, {
      fontFamily: FONTS.ui, fontSize: '20px', color: base, letterSpacing: 3,
    }).setOrigin(0.5).setInteractive({ useHandCursor: true });
    txt.on('pointerover', () => { txt.setColor('#ffffff'); Audio.playSfx('ui_hover'); });
    txt.on('pointerout', () => txt.setColor(base));
    txt.on('pointerdown', () => { Audio.playSfx('ui_click'); onClick(); });
  }
}

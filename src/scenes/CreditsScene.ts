// ============================================================================
// Créditos (GDD seção 11). Dono: COORDENADOR.
// ============================================================================
import Phaser from 'phaser';
import { GAME_WIDTH, GAME_HEIGHT, SCENE, FONTS, TEX } from '../core/config';
import { t } from '../core/strings';
import { Audio } from '../core/audio';

export class CreditsScene extends Phaser.Scene {
  constructor() { super(SCENE.CREDITS); }

  create(): void {
    const cx = GAME_WIDTH / 2;
    this.cameras.main.setBackgroundColor('#0b1026');

    const seal = this.add.image(cx, GAME_HEIGHT / 2, TEX.SEAL).setScale(4.2).setAlpha(0.08);
    this.tweens.add({ targets: seal, angle: -360, duration: 90000, repeat: -1 });

    this.add.text(cx, 80, t('credits.title'), {
      fontFamily: FONTS.title, fontSize: '52px', color: '#f0c75e', letterSpacing: 10,
    }).setOrigin(0.5);

    const rows: [string, string][] = [
      [t('credits.design'), t('credits.design.name')],
      [t('credits.dev'), t('credits.dev.name')],
      [t('credits.tech'), t('credits.tech.name')],
    ];
    rows.forEach(([label, name], i) => {
      const y = 190 + i * 110;
      this.add.text(cx, y, label.toUpperCase(), {
        fontFamily: FONTS.ui, fontSize: '16px', color: '#9a97ad', letterSpacing: 6,
      }).setOrigin(0.5);
      this.add.text(cx, y + 32, name, {
        fontFamily: FONTS.body, fontSize: '24px', color: '#e8e6f0',
      }).setOrigin(0.5);
    });

    this.add.text(cx, GAME_HEIGHT - 110, t('credits.assets'), {
      fontFamily: FONTS.body, fontSize: '15px', color: '#7a7790',
    }).setOrigin(0.5);

    const back = this.add.text(cx, GAME_HEIGHT - 56, `‹ ${t('settings.back')}`, {
      fontFamily: FONTS.ui, fontSize: '22px', color: '#f0c75e', letterSpacing: 4,
    }).setOrigin(0.5).setInteractive({ useHandCursor: true });
    back.on('pointerover', () => { back.setColor('#ffffff'); Audio.playSfx('ui_hover'); });
    back.on('pointerout', () => back.setColor('#f0c75e'));
    const goBack = () => { Audio.playSfx('ui_back'); this.scene.start(SCENE.MENU); };
    back.on('pointerdown', goBack);
    this.input.keyboard?.once('keydown-ESC', goBack);
  }
}

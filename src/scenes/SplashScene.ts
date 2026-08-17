// ============================================================================
// Splash do estúdio (GDD seção 11): 2s, fundo preto, selo do estúdio.
// Dono: COORDENADOR.
// ============================================================================
import Phaser from 'phaser';
import { GAME_WIDTH, GAME_HEIGHT, SCENE, COLORS, FONTS, STUDIO_NAME } from '../core/config';
import { t } from '../core/strings';

export class SplashScene extends Phaser.Scene {
  constructor() { super(SCENE.SPLASH); }

  create(): void {
    this.cameras.main.setBackgroundColor('#000000');
    const cx = GAME_WIDTH / 2, cy = GAME_HEIGHT / 2;

    const name = this.add.text(cx, cy - 10, STUDIO_NAME, {
      fontFamily: FONTS.title,
      fontSize: '54px',
      color: '#f0c75e',
      letterSpacing: 10,
    }).setOrigin(0.5).setAlpha(0);

    const sub = this.add.text(cx, cy + 44, t('splash.presents'), {
      fontFamily: FONTS.body,
      fontSize: '18px',
      color: '#9a97ad',
    }).setOrigin(0.5).setAlpha(0);

    this.tweens.add({ targets: [name, sub], alpha: 1, duration: 600, ease: 'Sine.easeIn' });
    this.tweens.add({
      targets: [name, sub],
      alpha: 0,
      delay: 1500,
      duration: 400,
      onComplete: () => this.scene.start(SCENE.LOADING),
    });

    // pular com clique/tecla
    this.input.once('pointerdown', () => this.scene.start(SCENE.LOADING));
    this.input.keyboard?.once('keydown', () => this.scene.start(SCENE.LOADING));
    void COLORS;
  }
}

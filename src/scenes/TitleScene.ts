// ============================================================================
// Tela de título (GDD seção 11): logo + "PRESSIONE ENTER" pulsando + tema.
// Dono: COORDENADOR.
// ============================================================================
import Phaser from 'phaser';
import { GAME_WIDTH, GAME_HEIGHT, SCENE, FONTS, TEX, GAME_TITLE, GAME_VERSION } from '../core/config';
import { t } from '../core/strings';
import { Audio } from '../core/audio';

export class TitleScene extends Phaser.Scene {
  constructor() { super(SCENE.TITLE); }

  create(): void {
    const cx = GAME_WIDTH / 2;
    this.cameras.main.setBackgroundColor('#0b1026');

    // selo girando sutil ao fundo
    const seal = this.add.image(cx, GAME_HEIGHT * 0.42, TEX.SEAL).setScale(3.4).setAlpha(0.16);
    this.tweens.add({ targets: seal, angle: 360, duration: 60000, repeat: -1 });

    // partículas elementais subindo
    this.add.particles(0, 0, TEX.PARTICLE, {
      x: { min: 0, max: GAME_WIDTH },
      y: GAME_HEIGHT + 10,
      lifespan: 9000,
      speedY: { min: -30, max: -12 },
      scale: { start: 0.8, end: 0 },
      alpha: { start: 0.35, end: 0 },
      quantity: 1,
      frequency: 320,
      tint: [0xff5a2a, 0x2aa7ff, 0xa8763e, 0x8fe8c9, 0xf5d90a],
    });

    this.add.text(cx, GAME_HEIGHT * 0.40, GAME_TITLE, {
      fontFamily: FONTS.title, fontSize: '120px', color: '#f0c75e', fontStyle: 'bold', letterSpacing: 22,
    }).setOrigin(0.5).setShadow(0, 5, '#000000', 16);

    this.add.text(cx, GAME_HEIGHT * 0.53, GAME_VERSION, {
      fontFamily: FONTS.ui, fontSize: '18px', color: '#9a97ad', letterSpacing: 4,
    }).setOrigin(0.5);

    const press = this.add.text(cx, GAME_HEIGHT * 0.72, t('title.press'), {
      fontFamily: FONTS.ui, fontSize: '26px', color: '#e8e6f0', letterSpacing: 8,
    }).setOrigin(0.5);
    this.tweens.add({ targets: press, alpha: 0.25, duration: 750, yoyo: true, repeat: -1, ease: 'Sine.easeInOut' });

    const go = () => {
      Audio.playSfx('title_confirm');
      Audio.playMusic('menu');
      this.scene.start(SCENE.MENU);
    };
    this.input.keyboard?.once('keydown-ENTER', go);
    this.input.once('pointerdown', go);
  }
}

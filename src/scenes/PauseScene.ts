// ============================================================================
// Pausa (GDD seção 11): lançada POR CIMA da ArenaScene pausada (a Arena faz
// scene.launch(SCENE.PAUSE) + scene.pause()). Retomar / Configurações /
// Abandonar partida. ESC também retoma.
// init({ silent: true }) evita repetir o sfx de pausa ao voltar das Configurações.
// Dono: WORKER A (UX/UI + gameplay).
// ============================================================================
import Phaser from 'phaser';
import { GAME_WIDTH, GAME_HEIGHT, SCENE, FONTS, TEX, COLORS } from '../core/config';
import { t } from '../core/strings';
import { Audio } from '../core/audio';

/** converte 0xRRGGBB para string CSS '#rrggbb' */
const css = (n: number): string => `#${n.toString(16).padStart(6, '0')}`;

export class PauseScene extends Phaser.Scene {
  private silent = false;

  constructor() { super(SCENE.PAUSE); }

  init(data: { silent?: boolean } = {}): void {
    this.silent = data.silent ?? false;
  }

  create(): void {
    const cx = GAME_WIDTH / 2;
    const cy = GAME_HEIGHT / 2;
    if (!this.silent) Audio.playSfx('pause');

    // véu escuro sobre a arena congelada (interativo p/ engolir cliques)
    this.add.rectangle(cx, cy, GAME_WIDTH, GAME_HEIGHT, 0x000000, 0.6).setInteractive();

    // selo sutil + painel central
    const seal = this.add.image(cx, cy, TEX.SEAL).setScale(2.6).setAlpha(0.06);
    this.tweens.add({ targets: seal, angle: 360, duration: 80000, repeat: -1 });

    this.add.rectangle(cx, cy + 10, 400, 320, COLORS.panel, 0.96)
      .setStrokeStyle(1, COLORS.gold, 0.8);

    this.add.text(cx, cy - 108, t('pause.title'), {
      fontFamily: FONTS.title, fontSize: '36px', color: css(COLORS.gold), letterSpacing: 10,
    }).setOrigin(0.5);

    this.makeButton(cx, cy - 24, t('pause.resume'), css(COLORS.text), '#ffffff',
      () => this.resumeArena());

    this.makeButton(cx, cy + 40, t('pause.settings'), css(COLORS.text), '#ffffff',
      () => this.scene.start(SCENE.SETTINGS, { from: SCENE.PAUSE }));

    this.makeButton(cx, cy + 104, t('pause.quit'), css(COLORS.textDim), css(COLORS.danger),
      () => {
        this.scene.stop(SCENE.ARENA);
        Audio.playMusic('menu');
        this.scene.start(SCENE.MENU);
      });

    // ESC também retoma (igual ao botão RETOMAR)
    this.input.keyboard?.on('keydown-ESC', () => {
      Audio.playSfx('ui_back');
      this.resumeArena();
    });
  }

  private resumeArena(): void {
    this.scene.stop();
    this.scene.resume(SCENE.ARENA);
  }

  private makeButton(
    x: number, y: number, label: string,
    base: string, hover: string, onClick: () => void,
  ): void {
    const txt = this.add.text(x, y, label, {
      fontFamily: FONTS.ui, fontSize: '22px', color: base, letterSpacing: 4,
    }).setOrigin(0.5).setInteractive({ useHandCursor: true });
    txt.on('pointerover', () => { txt.setColor(hover); Audio.playSfx('ui_hover'); });
    txt.on('pointerout', () => txt.setColor(base));
    txt.on('pointerdown', () => { Audio.playSfx('ui_click'); onClick(); });
  }
}

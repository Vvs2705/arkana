// ============================================================================
// Menu principal (GDD seção 11): fundo animado (partículas elementais + Selo de
// Arkana girando), botões verticais com hover sonoro, galeria de MAGOS em
// overlay e toasts para itens ainda não disponíveis.
// Dono: WORKER A (UX/UI + gameplay).
// ============================================================================
import Phaser from 'phaser';
import {
  GAME_WIDTH, GAME_HEIGHT, SCENE, FONTS, TEX, COLORS, ELEMENT_COLORS,
  GAME_TITLE, GAME_VERSION,
} from '../core/config';
import { ELEMENTS } from '../core/types';
import { t } from '../core/strings';
import { Audio } from '../core/audio';

/** converte 0xRRGGBB para string CSS '#rrggbb' */
const css = (n: number): string => `#${n.toString(16).padStart(6, '0')}`;

interface ItemDef {
  label: string;
  sub?: string;
  size: number;
  base: string;
  action: () => void;
}

export class MenuScene extends Phaser.Scene {
  private labels: Phaser.GameObjects.Text[] = [];
  private actions: (() => void)[] = [];
  private baseColors: string[] = [];
  private selected = -1;
  private overlay: Phaser.GameObjects.Container | null = null;
  private toast: Phaser.GameObjects.Container | null = null;
  private toastTimer: Phaser.Time.TimerEvent | null = null;

  constructor() { super(SCENE.MENU); }

  create(): void {
    // estado limpo a cada entrada (a instância da cena é reutilizada pelo Phaser)
    this.labels = [];
    this.actions = [];
    this.baseColors = [];
    this.selected = -1;
    this.overlay = null;
    this.toast = null;
    this.toastTimer = null;

    const cx = GAME_WIDTH / 2;
    this.cameras.main.setBackgroundColor(css(COLORS.night));
    Audio.playMusic('menu');

    // ---- fundo: Selo de Arkana girando lentamente + partículas elementais
    const seal = this.add.image(cx, GAME_HEIGHT * 0.52, TEX.SEAL).setScale(4.6).setAlpha(0.1);
    this.tweens.add({ targets: seal, angle: 360, duration: 80000, repeat: -1 });

    this.add.particles(0, 0, TEX.PARTICLE, {
      x: { min: 0, max: GAME_WIDTH },
      y: { min: GAME_HEIGHT * 0.1, max: GAME_HEIGHT + 10 },
      lifespan: 11000,
      speedX: { min: -9, max: 9 },
      speedY: { min: -20, max: -7 },
      scale: { start: 0.9, end: 0 },
      alpha: { start: 0.32, end: 0 },
      quantity: 1,
      frequency: 240,
      tint: ELEMENTS.map((el) => ELEMENT_COLORS[el]),
    });

    // ---- cabeçalho
    this.add.text(cx, 92, GAME_TITLE, {
      fontFamily: FONTS.title, fontSize: '64px', color: css(COLORS.gold),
      fontStyle: 'bold', letterSpacing: 14,
    }).setOrigin(0.5).setShadow(0, 4, '#000000', 12);
    this.add.text(cx, 148, GAME_VERSION, {
      fontFamily: FONTS.ui, fontSize: '14px', color: css(COLORS.textDim), letterSpacing: 4,
    }).setOrigin(0.5);

    // ---- botões verticais (hierarquia: JOGAR em destaque dourado)
    const defs: ItemDef[] = [
      {
        label: t('menu.play'), sub: t('menu.play.sub'), size: 38, base: css(COLORS.gold),
        action: () => this.scene.start(SCENE.ARENA),
      },
      { label: t('menu.mages'), size: 26, base: css(COLORS.text), action: () => this.openMages() },
      { label: t('menu.arsenal'), size: 26, base: css(COLORS.text), action: () => this.showToast(t('menu.soon')) },
      {
        label: t('menu.settings'), size: 26, base: css(COLORS.text),
        action: () => this.scene.start(SCENE.SETTINGS, { from: SCENE.MENU }),
      },
      { label: t('menu.credits'), size: 26, base: css(COLORS.text), action: () => this.scene.start(SCENE.CREDITS) },
      { label: t('menu.quit'), size: 26, base: css(COLORS.textDim), action: () => this.showToast(t('menu.quit.web')) },
    ];
    const ys = [268, 372, 428, 484, 540, 596];

    defs.forEach((def, i) => {
      const label = this.add.text(cx, ys[i], def.label, {
        fontFamily: FONTS.ui, fontSize: `${def.size}px`, color: def.base,
        fontStyle: i === 0 ? 'bold' : 'normal', letterSpacing: i === 0 ? 8 : 4,
      }).setOrigin(0.5).setInteractive({ useHandCursor: true });
      if (def.sub !== undefined) {
        this.add.text(cx, ys[i] + 34, def.sub, {
          fontFamily: FONTS.body, fontSize: '15px', color: css(COLORS.textDim),
        }).setOrigin(0.5);
      }
      label.on('pointerover', () => this.setSelected(i));
      label.on('pointerout', () => { if (this.selected === i) this.setSelected(-1); });
      label.on('pointerdown', () => this.activate(i));
      this.labels.push(label);
      this.actions.push(def.action);
      this.baseColors.push(def.base);
    });

    // ---- navegação por teclado (setas + ENTER); ESC fecha a galeria de magos
    const kb = this.input.keyboard;
    kb?.on('keydown-UP', () => this.moveSelection(-1));
    kb?.on('keydown-DOWN', () => this.moveSelection(1));
    kb?.on('keydown-ENTER', () => {
      if (this.overlay !== null) return;
      if (this.selected >= 0) this.activate(this.selected);
    });
    kb?.on('keydown-ESC', () => this.closeMages());
  }

  // ------------------------------------------------------------ seleção/ação
  private moveSelection(dir: number): void {
    if (this.overlay !== null) return;
    const n = this.labels.length;
    const next = this.selected < 0
      ? (dir > 0 ? 0 : n - 1)
      : (this.selected + dir + n) % n;
    this.setSelected(next);
  }

  private setSelected(i: number): void {
    if (this.selected === i) return;
    if (this.selected >= 0) this.applyStyle(this.selected, false);
    this.selected = i;
    if (i >= 0) {
      this.applyStyle(i, true);
      Audio.playSfx('ui_hover');
    }
  }

  private applyStyle(i: number, on: boolean): void {
    const label = this.labels[i];
    label.setColor(on ? '#ffffff' : this.baseColors[i]);
    this.tweens.add({ targets: label, scale: on ? 1.06 : 1, duration: 120, ease: 'Sine.easeOut' });
  }

  private activate(i: number): void {
    if (this.overlay !== null) return;
    Audio.playSfx('ui_click');
    this.actions[i]();
  }

  // ------------------------------------------------------------ galeria MAGOS
  private openMages(): void {
    if (this.overlay !== null) return;
    const cx = GAME_WIDTH / 2;
    const c = this.add.container(0, 0).setDepth(60);

    // véu escuro: clique fora do painel fecha
    const blocker = this.add.rectangle(cx, GAME_HEIGHT / 2, GAME_WIDTH, GAME_HEIGHT, 0x000000, 0.55)
      .setInteractive();
    blocker.on('pointerdown', () => this.closeMages());

    // painel central (engole cliques na área interna)
    const panel = this.add.rectangle(cx, 376, 680, 470, COLORS.panel, 0.97)
      .setStrokeStyle(1, COLORS.gold, 0.9)
      .setInteractive();

    const title = this.add.text(cx, 178, t('mages.title'), {
      fontFamily: FONTS.title, fontSize: '30px', color: css(COLORS.gold), letterSpacing: 8,
    }).setOrigin(0.5);

    // card: moldura dourada + retrato com animação de apresentação em loop
    const frameBg = this.add.rectangle(470, 392, 204, 204, COLORS.panelLight, 1)
      .setStrokeStyle(2, COLORS.gold, 1);
    const glow = this.add.circle(470, 392, 86, COLORS.gold, 0.07);
    const portrait = this.add.image(470, 388, TEX.PLAYER).setScale(6);
    this.tweens.add({
      targets: portrait, y: portrait.y - 10, duration: 1600,
      yoyo: true, repeat: -1, ease: 'Sine.easeInOut',
    });
    this.tweens.add({
      targets: portrait, angle: { from: -2.5, to: 2.5 }, duration: 2200,
      yoyo: true, repeat: -1, ease: 'Sine.easeInOut',
    });
    this.tweens.add({
      targets: glow, alpha: { from: 0.05, to: 0.13 }, duration: 1600,
      yoyo: true, repeat: -1, ease: 'Sine.easeInOut',
    });

    const name = this.add.text(600, 312, t('mages.evoker.name'), {
      fontFamily: FONTS.ui, fontSize: '26px', color: css(COLORS.gold),
      fontStyle: 'bold', letterSpacing: 3,
    }).setOrigin(0, 0.5);
    const subtitle = this.add.text(600, 344, t('mages.evoker.title'), {
      fontFamily: FONTS.body, fontSize: '16px', color: css(COLORS.textDim), fontStyle: 'italic',
    }).setOrigin(0, 0.5);
    const desc = this.add.text(600, 372, t('mages.evoker.desc'), {
      fontFamily: FONTS.body, fontSize: '15px', color: '#c9c6da',
      wordWrap: { width: 360 }, lineSpacing: 6,
    }).setOrigin(0, 0);

    // botão fechar (✕) + VOLTAR (ESC também fecha — listener registrado no create)
    const closeBtn = this.add.text(958, 178, '✕', {
      fontFamily: FONTS.ui, fontSize: '22px', color: css(COLORS.textDim),
    }).setOrigin(0.5).setInteractive({ useHandCursor: true });
    closeBtn.on('pointerover', () => { closeBtn.setColor('#ffffff'); Audio.playSfx('ui_hover'); });
    closeBtn.on('pointerout', () => closeBtn.setColor(css(COLORS.textDim)));
    closeBtn.on('pointerdown', () => this.closeMages());

    const back = this.add.text(cx, 576, `‹ ${t('settings.back')}`, {
      fontFamily: FONTS.ui, fontSize: '20px', color: css(COLORS.gold), letterSpacing: 4,
    }).setOrigin(0.5).setInteractive({ useHandCursor: true });
    back.on('pointerover', () => { back.setColor('#ffffff'); Audio.playSfx('ui_hover'); });
    back.on('pointerout', () => back.setColor(css(COLORS.gold)));
    back.on('pointerdown', () => this.closeMages());

    c.add([blocker, panel, title, frameBg, glow, portrait, name, subtitle, desc, closeBtn, back]);
    this.overlay = c;
  }

  private closeMages(): void {
    if (this.overlay === null) return;
    Audio.playSfx('ui_back');
    this.tweens.killTweensOf(this.overlay.list);
    this.overlay.destroy();
    this.overlay = null;
  }

  // ------------------------------------------------------------ toast
  /** aviso temporário no rodapé (some sozinho em 1,5s) */
  private showToast(msg: string): void {
    if (this.toast !== null) {
      this.toastTimer?.remove();
      this.tweens.killTweensOf(this.toast);
      this.toast.destroy();
      this.toast = null;
    }
    const txt = this.add.text(0, 0, msg, {
      fontFamily: FONTS.body, fontSize: '16px', color: css(COLORS.text),
    }).setOrigin(0.5);
    const bg = this.add.rectangle(0, 0, txt.width + 48, 44, COLORS.panelLight, 0.97)
      .setStrokeStyle(1, COLORS.goldDim, 1);
    const c = this.add.container(GAME_WIDTH / 2, GAME_HEIGHT - 46, [bg, txt])
      .setDepth(80).setAlpha(0);
    this.toast = c;
    this.tweens.add({ targets: c, alpha: 1, duration: 150 });
    this.toastTimer = this.time.delayedCall(1500, () => {
      this.tweens.add({
        targets: c, alpha: 0, duration: 250,
        onComplete: () => {
          if (this.toast === c) this.toast = null;
          c.destroy();
        },
      });
    });
  }
}

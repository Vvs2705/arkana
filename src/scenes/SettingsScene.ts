// ============================================================================
// Configurações (GDD seção 12): 4 abas (Vídeo/Áudio/Controles/Jogo).
// Toda mudança persiste imediatamente via Settings.update; remapeamento de
// teclas com captura de KeyboardEvent.code e swap de teclas duplicadas.
// init({ from, tab }): 'from' é a cena de retorno (Menu ou Pause).
// Dono: WORKER A (UX/UI + gameplay).
// ============================================================================
import Phaser from 'phaser';
import { GAME_WIDTH, GAME_HEIGHT, SCENE, FONTS, TEX, COLORS, TOUCH } from '../core/config';
import { t } from '../core/strings';
import { Settings, type GameSettings, type KeyAction } from '../core/settings';
import { Audio } from '../core/audio';

/** converte 0xRRGGBB para string CSS '#rrggbb' */
const css = (n: number): string => `#${n.toString(16).padStart(6, '0')}`;

type TabKey = keyof GameSettings; // 'video' | 'audio' | 'controls' | 'game'
const TAB_KEYS: TabKey[] = ['video', 'audio', 'controls', 'game'];

// ---- geometria do painel central
const P_CX = GAME_WIDTH / 2;
const P_W = 920;
const P_TOP = 132;
const P_H = 496;
const P_LEFT = P_CX - P_W / 2;
const LABEL_X = P_LEFT + 48;          // coluna de rótulos (esquerda)
const CTRL_R = P_CX + P_W / 2 - 48;   // borda direita da área de controles
const SEL_L = 756;                    // seta '‹' dos selects
const TRACK_L = 736;                  // início da trilha dos sliders
const TRACK_W = 240;
const RESTORE_Y = P_TOP + P_H - 28;

/** rótulos de exibição para KeyboardEvent.code (formatação, não é i18n) */
const KEY_LABELS: Record<string, string> = {
  Space: 'ESPAÇO', Tab: 'TAB', Enter: 'ENTER', Backspace: 'BACKSPACE',
  ShiftLeft: 'SHIFT', ShiftRight: 'SHIFT', ControlLeft: 'CTRL', ControlRight: 'CTRL',
  AltLeft: 'ALT', AltRight: 'ALT', CapsLock: 'CAPS LOCK',
  ArrowUp: '↑', ArrowDown: '↓', ArrowLeft: '←', ArrowRight: '→',
};

function formatKey(code: string): string {
  if (KEY_LABELS[code] !== undefined) return KEY_LABELS[code];
  if (code.startsWith('Key')) return code.slice(3);
  if (code.startsWith('Digit')) return code.slice(5);
  if (code.startsWith('Numpad')) return `NUM ${code.slice(6)}`;
  return code.toUpperCase();
}

/** ordem das linhas de remapeamento (todas as ações de KeyAction) */
const KEY_ACTIONS: KeyAction[] = [
  'up', 'down', 'left', 'right', 'dodge', 'element', 'supreme', 'scoreboard',
];

export class SettingsScene extends Phaser.Scene {
  private from: string = SCENE.MENU;
  private tabIndex = 0;
  /** true enquanto aguarda a próxima tecla no remapeamento (bloqueia ESC/abas) */
  private capturing = false;
  private tabTexts: Phaser.GameObjects.Text[] = [];
  private tabUnderline!: Phaser.GameObjects.Rectangle;
  /** objetos da aba atual — destruídos a cada renderTab() */
  private tabObjects: Phaser.GameObjects.GameObject[] = [];

  constructor() { super(SCENE.SETTINGS); }

  init(data: { from?: string; tab?: number } = {}): void {
    this.from = data.from ?? SCENE.MENU;
    this.tabIndex = Phaser.Math.Clamp(data.tab ?? 0, 0, TAB_KEYS.length - 1);
    this.capturing = false;
    this.tabTexts = [];
    this.tabObjects = [];
  }

  create(): void {
    this.cameras.main.setBackgroundColor(css(COLORS.night));

    // selo sutil ao fundo
    const seal = this.add.image(P_CX, GAME_HEIGHT / 2, TEX.SEAL).setScale(4.2).setAlpha(0.06);
    this.tweens.add({ targets: seal, angle: 360, duration: 90000, repeat: -1 });

    // título
    this.add.text(P_CX, 52, t('settings.title'), {
      fontFamily: FONTS.title, fontSize: '40px', color: css(COLORS.gold), letterSpacing: 10,
    }).setOrigin(0.5);

    // ---- abas
    const tabLabels = [
      t('settings.tab.video'), t('settings.tab.audio'),
      t('settings.tab.controls'), t('settings.tab.game'),
    ];
    tabLabels.forEach((label, i) => {
      const x = P_CX + (i - (tabLabels.length - 1) / 2) * 200;
      const txt = this.add.text(x, 100, label, {
        fontFamily: FONTS.ui, fontSize: '18px', color: css(COLORS.textDim), letterSpacing: 3,
      }).setOrigin(0.5).setInteractive({ useHandCursor: true });
      txt.on('pointerover', () => {
        if (i !== this.tabIndex) { txt.setColor('#ffffff'); Audio.playSfx('ui_hover'); }
      });
      txt.on('pointerout', () => {
        if (i !== this.tabIndex) txt.setColor(css(COLORS.textDim));
      });
      txt.on('pointerdown', () => this.switchTab(i));
      this.tabTexts.push(txt);
    });
    this.tabUnderline = this.add.rectangle(P_CX, 118, 10, 2, COLORS.gold);
    this.refreshTabs();

    // ---- painel central com borda dourada fina
    this.add.rectangle(P_CX, P_TOP + P_H / 2, P_W, P_H, COLORS.panel, 0.92)
      .setStrokeStyle(1, COLORS.gold, 0.7);

    // ---- VOLTAR (botão + ESC)
    const back = this.add.text(P_CX, GAME_HEIGHT - 52, `‹ ${t('settings.back')}`, {
      fontFamily: FONTS.ui, fontSize: '22px', color: css(COLORS.gold), letterSpacing: 4,
    }).setOrigin(0.5).setInteractive({ useHandCursor: true });
    back.on('pointerover', () => { back.setColor('#ffffff'); Audio.playSfx('ui_hover'); });
    back.on('pointerout', () => back.setColor(css(COLORS.gold)));
    back.on('pointerdown', () => this.goBack());
    this.input.keyboard?.on('keydown-ESC', () => this.goBack());

    this.renderTab();
  }

  // ------------------------------------------------------------ navegação
  private goBack(): void {
    if (this.capturing) return; // ESC durante captura só cancela a captura
    Audio.playSfx('ui_back');
    if (this.from === SCENE.PAUSE) {
      // a Arena continua pausada por baixo — apenas devolve a tela de pausa
      this.scene.start(SCENE.PAUSE, { silent: true });
    } else {
      this.scene.start(this.from);
    }
  }

  private switchTab(i: number): void {
    if (this.capturing || i === this.tabIndex) return;
    this.tabIndex = i;
    Audio.playSfx('ui_click');
    this.refreshTabs();
    this.renderTab();
  }

  private refreshTabs(): void {
    this.tabTexts.forEach((txt, i) => {
      txt.setColor(i === this.tabIndex ? css(COLORS.gold) : css(COLORS.textDim));
    });
    const active = this.tabTexts[this.tabIndex];
    this.tabUnderline.setPosition(active.x, 118).setDisplaySize(active.width + 12, 2);
  }

  private renderTab(): void {
    this.tabObjects.forEach((o) => o.destroy());
    this.tabObjects = [];
    switch (TAB_KEYS[this.tabIndex]) {
      case 'video': this.buildVideo(); break;
      case 'audio': this.buildAudio(); break;
      case 'controls': this.buildControls(); break;
      case 'game': this.buildGame(); break;
    }
    this.buildRestore();
  }

  /** registra um objeto como pertencente à aba atual (limpo no re-render) */
  private keep<T extends Phaser.GameObjects.GameObject>(obj: T): T {
    this.tabObjects.push(obj);
    return obj;
  }

  // ------------------------------------------------------------ abas
  private buildVideo(): void {
    const v = Settings.get().video;

    let y = 176;
    this.rowLabel(y, t('settings.fullscreen'));
    this.makeToggle(y, v.fullscreen, (val) => {
      Settings.update({ video: { ...Settings.get().video, fullscreen: val } });
      if (val && !this.scale.isFullscreen) this.scale.startFullscreen();
      else if (!val && this.scale.isFullscreen) this.scale.stopFullscreen();
    });
    this.separator(y + 30);

    y = 236;
    const resolutions: GameSettings['video']['resolution'][] = ['1280x720', '1600x900', '1920x1080'];
    this.rowLabel(y, t('settings.resolution'));
    this.makeSelect(y, resolutions.map((r) => r.replace('x', ' × ')), resolutions.indexOf(v.resolution), (i) => {
      Settings.update({ video: { ...Settings.get().video, resolution: resolutions[i] } });
    });
    this.note(y + 20, t('settings.fps.note'));
    this.separator(y + 30);

    y = 296;
    const qualities: GameSettings['video']['quality'][] = ['low', 'medium', 'high'];
    this.rowLabel(y, t('settings.quality'));
    this.makeSelect(
      y,
      [t('settings.quality.low'), t('settings.quality.medium'), t('settings.quality.high')],
      qualities.indexOf(v.quality),
      (i) => { Settings.update({ video: { ...Settings.get().video, quality: qualities[i] } }); },
    );
    this.separator(y + 30);

    y = 356;
    const fpsValues: GameSettings['video']['fpsLimit'][] = [30, 60, 120, 0];
    this.rowLabel(y, t('settings.fps'));
    this.makeSelect(
      y,
      ['30', '60', '120', t('settings.fps.unlimited')],
      fpsValues.indexOf(v.fpsLimit),
      (i) => { Settings.update({ video: { ...Settings.get().video, fpsLimit: fpsValues[i] } }); },
    );
    this.note(y + 20, t('settings.fps.note'));
    this.separator(y + 30);

    y = 416;
    this.rowLabel(y, t('settings.vsync'));
    this.makeToggle(y, v.vsync, () => { /* desabilitado — o navegador controla */ }, true);
    this.note(y + 20, t('settings.vsync.note'));
    this.separator(y + 30);

    y = 476;
    this.rowLabel(y, t('settings.showfps'));
    this.makeToggle(y, v.showFps, (val) => {
      Settings.update({ video: { ...Settings.get().video, showFps: val } });
    });
  }

  private buildAudio(): void {
    const a = Settings.get().audio;
    const rows: { label: string; key: keyof GameSettings['audio'] }[] = [
      { label: t('settings.vol.master'), key: 'master' },
      { label: t('settings.vol.music'), key: 'music' },
      { label: t('settings.vol.sfx'), key: 'sfx' },
      { label: t('settings.vol.ui'), key: 'ui' },
    ];
    rows.forEach((row, i) => {
      const y = 196 + i * 62;
      this.rowLabel(y, row.label);
      // o preview sonoro (ui_slider) toca dentro do makeSlider a cada ajuste
      this.makeSlider(y, 0, 100, 1, a[row.key], 0, (val) => {
        const cur = { ...Settings.get().audio };
        cur[row.key] = val;
        Settings.update({ audio: cur });
      });
      if (i < rows.length - 1) this.separator(y + 31);
    });
  }

  private buildControls(): void {
    const c = Settings.get().controls;

    let y = 160;
    this.rowLabel(y, t('settings.sens.mouse'), 16);
    this.makeSlider(y, 0.1, 10, 0.1, c.mouseSens, 1, (val) => {
      Settings.update({ controls: { ...Settings.get().controls, mouseSens: val } });
    });

    y = 198;
    this.rowLabel(y, t('settings.sens.aim'), 16);
    this.makeSlider(y, 0.1, 3, 0.1, c.aimSens, 1, (val) => {
      Settings.update({ controls: { ...Settings.get().controls, aimSens: val } });
    });

    y = 234;
    this.rowLabel(y, t('settings.inverty'), 16);
    this.makeToggle(y, c.invertY, (val) => {
      Settings.update({ controls: { ...Settings.get().controls, invertY: val } });
    });
    this.separator(y + 22);

    // ---- controles de toque (Fase 2 — GDD 19.3)
    y = 278;
    const touchModes: GameSettings['controls']['touch']['mode'][] = ['auto', 'on', 'off'];
    this.rowLabel(y, t('settings.touch'), 16);
    this.makeSelect(
      y,
      [t('settings.touch.auto'), t('settings.on'), t('settings.off')],
      touchModes.indexOf(c.touch.mode),
      (i) => {
        Settings.update({
          controls: { ...Settings.get().controls, touch: { ...Settings.get().controls.touch, mode: touchModes[i] } },
        });
      },
    );
    this.note(y + 19, t('settings.touch.note'));

    // gesto único (R15 — GDD 19.3): padrão 'unified'; o clássico continua aqui
    y = 314;
    const gestures: GameSettings['controls']['touch']['gesture'][] = ['unified', 'legacy'];
    this.rowLabel(y, t('settings.touch.gesture'), 16);
    this.makeSelect(
      y,
      [t('settings.touch.gesture.unified'), t('settings.touch.gesture.legacy')],
      gestures.indexOf(c.touch.gesture),
      (i) => {
        Settings.update({
          controls: { ...Settings.get().controls, touch: { ...Settings.get().controls.touch, gesture: gestures[i] } },
        });
      },
    );
    this.note(y + 19, t('settings.touch.gesture.note'));

    y = 350;
    const schemes: GameSettings['controls']['touch']['scheme'][] = ['simple', 'advanced'];
    this.rowLabel(y, t('settings.touch.scheme'), 16);
    this.makeSelect(
      y,
      [t('settings.touch.scheme.simple'), t('settings.touch.scheme.adv')],
      schemes.indexOf(c.touch.scheme),
      (i) => {
        Settings.update({
          controls: { ...Settings.get().controls, touch: { ...Settings.get().controls.touch, scheme: schemes[i] } },
        });
      },
    );

    y = 384;
    this.rowLabel(y, t('settings.touch.scale'), 16);
    this.makeSlider(y, TOUCH.scaleMin, TOUCH.scaleMax, 0.05, c.touch.scale, 2, (val) => {
      Settings.update({
        controls: { ...Settings.get().controls, touch: { ...Settings.get().controls.touch, scale: val } },
      });
    });

    y = 416;
    const editBtn = this.keep(this.add.text(LABEL_X, y, t('settings.touch.edit'), {
      fontFamily: FONTS.ui, fontSize: '16px', color: css(COLORS.gold),
    }).setOrigin(0, 0.5).setInteractive({ useHandCursor: true }));
    editBtn.on('pointerover', () => { editBtn.setColor('#ffffff'); Audio.playSfx('ui_hover'); });
    editBtn.on('pointerout', () => editBtn.setColor(css(COLORS.gold)));
    editBtn.on('pointerdown', () => {
      if (this.capturing) return;
      Audio.playSfx('ui_click');
      this.scene.start(SCENE.TOUCH_LAYOUT, { from: this.from });
    });
    this.separator(y + 20);

    // remapeamento em DUAS colunas (4 ações por coluna); clicar captura a tecla
    this.keep(this.add.text(LABEL_X, 452, t('settings.keys').toUpperCase(), {
      fontFamily: FONTS.ui, fontSize: '13px', color: css(COLORS.textDim), letterSpacing: 3,
    }).setOrigin(0, 0.5));
    KEY_ACTIONS.forEach((action, i) => {
      const col = Math.floor(i / 4);
      const rowY = 478 + (i % 4) * 30;
      const labelX = col === 0 ? LABEL_X : P_CX + 16;
      const boxCx = col === 0 ? P_CX - 140 : CTRL_R - 75;
      this.makeKeyRow(rowY, action, labelX, boxCx);
    });
  }

  private buildGame(): void {
    const g = Settings.get().game;

    let y = 196;
    const langs: GameSettings['game']['language'][] = ['pt', 'en'];
    this.rowLabel(y, t('settings.language'));
    this.makeSelect(y, ['PT-BR', 'English'], langs.indexOf(g.language), (i) => {
      Settings.update({ game: { ...Settings.get().game, language: langs[i] } });
      // re-renderiza a cena inteira já no novo idioma, mantendo aba e retorno
      this.scene.restart({ from: this.from, tab: this.tabIndex });
    });
    this.separator(y + 31);

    y = 258;
    const cbs: GameSettings['game']['colorblind'][] = ['off', 'protanopia', 'deuteranopia', 'tritanopia'];
    this.rowLabel(y, t('settings.colorblind'));
    this.makeSelect(
      y,
      [
        t('settings.colorblind.off'), t('settings.colorblind.prot'),
        t('settings.colorblind.deut'), t('settings.colorblind.trit'),
      ],
      cbs.indexOf(g.colorblind),
      (i) => { Settings.update({ game: { ...Settings.get().game, colorblind: cbs[i] } }); },
    );
    this.separator(y + 31);

    y = 320;
    this.rowLabel(y, t('settings.dmgnumbers'));
    this.makeToggle(y, g.damageNumbers, (val) => {
      Settings.update({ game: { ...Settings.get().game, damageNumbers: val } });
    });
    this.separator(y + 31);

    y = 382;
    this.rowLabel(y, t('settings.combotips'));
    this.makeToggle(y, g.comboTips, (val) => {
      Settings.update({ game: { ...Settings.get().game, comboTips: val } });
    });
  }

  /** botão "Restaurar padrão" da aba atual */
  private buildRestore(): void {
    const btn = this.keep(this.add.text(LABEL_X, RESTORE_Y, t('settings.restore'), {
      fontFamily: FONTS.ui, fontSize: '15px', color: css(COLORS.textDim),
    }).setOrigin(0, 0.5).setInteractive({ useHandCursor: true }));
    btn.on('pointerover', () => { btn.setColor('#ffffff'); Audio.playSfx('ui_hover'); });
    btn.on('pointerout', () => btn.setColor(css(COLORS.textDim)));
    btn.on('pointerdown', () => {
      if (this.capturing) return;
      Audio.playSfx('ui_click');
      const tab = TAB_KEYS[this.tabIndex];
      Settings.resetTab(tab);
      if (tab === 'video' && !Settings.get().video.fullscreen && this.scale.isFullscreen) {
        this.scale.stopFullscreen();
      }
      if (tab === 'game') {
        // o idioma pode ter voltado ao padrão — re-renderiza a cena inteira
        this.scene.restart({ from: this.from, tab: this.tabIndex });
      } else {
        this.renderTab();
      }
    });
  }

  // ------------------------------------------------------------ componentes
  private rowLabel(y: number, text: string, size = 17): void {
    this.keep(this.add.text(LABEL_X, y, text, {
      fontFamily: FONTS.ui, fontSize: `${size}px`, color: css(COLORS.text),
    }).setOrigin(0, 0.5));
  }

  private note(y: number, text: string): void {
    this.keep(this.add.text(TRACK_L, y, text, {
      fontFamily: FONTS.body, fontSize: '12px', color: css(COLORS.textDim),
    }).setOrigin(0, 0.5));
  }

  private separator(y: number): void {
    this.keep(this.add.rectangle(P_CX, y, P_W - 96, 1, 0xffffff, 0.05));
  }

  /** interruptor Ligado/Desligado (disabled = só exibição, ex.: VSync) */
  private makeToggle(y: number, value: boolean, onChange: (v: boolean) => void, disabled = false): void {
    let val = value;
    const trackX = CTRL_R - 28;
    const stateText = this.keep(this.add.text(trackX - 40, y, t(val ? 'settings.on' : 'settings.off'), {
      fontFamily: FONTS.ui, fontSize: '15px', color: css(val ? COLORS.gold : COLORS.textDim),
    }).setOrigin(1, 0.5));
    const track = this.keep(this.add.rectangle(trackX, y, 52, 22, val ? COLORS.gold : COLORS.panelLight, 1)
      .setStrokeStyle(1, COLORS.goldDim, 1));
    const knob = this.keep(this.add.circle(trackX + (val ? 13 : -13), y, 8, 0xffffff));

    if (disabled) {
      track.setAlpha(0.3);
      knob.setAlpha(0.3);
      stateText.setAlpha(0.45);
      return;
    }
    const refresh = (): void => {
      stateText.setText(t(val ? 'settings.on' : 'settings.off'))
        .setColor(css(val ? COLORS.gold : COLORS.textDim));
      track.setFillStyle(val ? COLORS.gold : COLORS.panelLight, 1);
      knob.x = trackX + (val ? 13 : -13);
    };
    track.setInteractive({ useHandCursor: true });
    track.on('pointerover', () => Audio.playSfx('ui_hover'));
    track.on('pointerdown', () => {
      val = !val;
      Audio.playSfx('ui_click');
      refresh();
      onChange(val);
    });
  }

  /** seletor cíclico ‹ valor › */
  private makeSelect(y: number, labels: string[], index: number, onChange: (i: number) => void): void {
    let idx = Math.max(0, index);
    const value = this.keep(this.add.text((SEL_L + CTRL_R) / 2, y, labels[idx], {
      fontFamily: FONTS.ui, fontSize: '16px', color: css(COLORS.text),
    }).setOrigin(0.5));
    const arrow = (x: number, ch: string, dir: number): void => {
      const a = this.keep(this.add.text(x, y, ch, {
        fontFamily: FONTS.ui, fontSize: '22px', color: css(COLORS.gold),
      }).setOrigin(0.5).setInteractive({ useHandCursor: true }));
      a.on('pointerover', () => { a.setColor('#ffffff'); Audio.playSfx('ui_hover'); });
      a.on('pointerout', () => a.setColor(css(COLORS.gold)));
      a.on('pointerdown', () => {
        idx = (idx + dir + labels.length) % labels.length;
        value.setText(labels[idx]);
        Audio.playSfx('ui_click');
        onChange(idx);
      });
    };
    arrow(SEL_L, '‹', -1);
    arrow(CTRL_R, '›', 1);
  }

  /** slider clicável/arrastável: trilha + preenchimento dourado + valor */
  private makeSlider(
    y: number, min: number, max: number, step: number, value: number,
    decimals: number, onChange: (v: number) => void,
  ): void {
    let val = Phaser.Math.Clamp(value, min, max);
    const fmt = (n: number): string => n.toFixed(decimals);
    const frac0 = (val - min) / (max - min);

    this.keep(this.add.rectangle(TRACK_L + TRACK_W / 2, y, TRACK_W, 6, 0xffffff, 0.1));
    const fill = this.keep(this.add.rectangle(TRACK_L, y, Math.max(1, frac0 * TRACK_W), 6, COLORS.gold)
      .setOrigin(0, 0.5));
    const handle = this.keep(this.add.circle(TRACK_L + frac0 * TRACK_W, y, 9, 0xffffff)
      .setStrokeStyle(2, COLORS.gold, 1));
    const valText = this.keep(this.add.text(CTRL_R, y, fmt(val), {
      fontFamily: FONTS.ui, fontSize: '16px', color: css(COLORS.gold),
    }).setOrigin(1, 0.5));

    const apply = (px: number): void => {
      const frac = Phaser.Math.Clamp((px - TRACK_L) / TRACK_W, 0, 1);
      let nv = min + frac * (max - min);
      nv = Math.round(nv / step) * step;
      nv = Number(Phaser.Math.Clamp(nv, min, max).toFixed(decimals));
      if (nv === val) return;
      val = nv;
      const f = (val - min) / (max - min);
      fill.width = Math.max(1, f * TRACK_W);
      handle.x = TRACK_L + f * TRACK_W;
      valText.setText(fmt(val));
      Audio.playSfx('ui_slider'); // preview sonoro do ajuste
      onChange(val);
    };
    const zone = this.keep(this.add.zone(TRACK_L + TRACK_W / 2, y, TRACK_W + 26, 30)
      .setInteractive({ useHandCursor: true, draggable: true }));
    zone.on('pointerdown', (p: Phaser.Input.Pointer) => apply(p.x));
    zone.on('drag', (p: Phaser.Input.Pointer) => apply(p.x));
  }

  /** linha de remapeamento: rótulo + tecla atual (clique = capturar nova) */
  private makeKeyRow(y: number, action: KeyAction, labelX: number = LABEL_X, boxCx: number = CTRL_R - 100): void {
    this.keep(this.add.text(labelX, y, t(`settings.key.${action}`), {
      fontFamily: FONTS.ui, fontSize: '15px', color: css(COLORS.text),
    }).setOrigin(0, 0.5));
    const bg = this.keep(this.add.rectangle(boxCx, y, 150, 26, COLORS.panelLight, 1)
      .setStrokeStyle(1, COLORS.goldDim, 0.8)
      .setInteractive({ useHandCursor: true }));
    const keyText = this.keep(this.add.text(boxCx, y, formatKey(Settings.key(action)), {
      fontFamily: FONTS.ui, fontSize: '15px', color: css(COLORS.text),
    }).setOrigin(0.5));
    bg.on('pointerover', () => {
      if (!this.capturing) { bg.setStrokeStyle(1, COLORS.gold, 1); Audio.playSfx('ui_hover'); }
    });
    bg.on('pointerout', () => {
      if (!this.capturing) bg.setStrokeStyle(1, COLORS.goldDim, 0.8);
    });
    bg.on('pointerdown', () => this.beginCapture(action, keyText, bg));
  }

  /** captura a próxima keydown para a ação (ESC cancela; duplicada = swap) */
  private beginCapture(
    action: KeyAction,
    keyText: Phaser.GameObjects.Text,
    bg: Phaser.GameObjects.Rectangle,
  ): void {
    if (this.capturing) return;
    this.capturing = true;
    Audio.playSfx('ui_click');
    bg.setStrokeStyle(1, COLORS.gold, 1);
    keyText.setText(t('settings.keys.press')).setColor(css(COLORS.gold));

    const kb = this.input.keyboard;
    const finish = (): void => {
      kb?.off('keydown', onKey);
      // o Phaser emite 'keydown' genérico ANTES de 'keydown-ESC': soltar o guard
      // só no próximo tick impede que o MESMO Esc da captura feche a cena
      this.time.delayedCall(0, () => { this.capturing = false; });
    };
    const onKey = (ev: KeyboardEvent): void => {
      ev.preventDefault();
      finish();
      if (ev.code === 'Escape') {
        Audio.playSfx('ui_back');
        keyText.setText(formatKey(Settings.key(action))).setColor(css(COLORS.text));
        bg.setStrokeStyle(1, COLORS.goldDim, 0.8);
        return;
      }
      // tecla já usada em outra ação → swap (nenhuma ação fica sem tecla)
      const keys: Record<KeyAction, string> = { ...Settings.get().controls.keys };
      const prev = keys[action];
      (Object.keys(keys) as KeyAction[]).forEach((a) => {
        if (a !== action && keys[a] === ev.code) keys[a] = prev;
      });
      keys[action] = ev.code;
      Settings.update({ controls: { ...Settings.get().controls, keys } });
      Audio.playSfx('ui_click');
      this.renderTab(); // reflete o swap em todas as linhas
    };
    kb?.on('keydown', onKey);
  }
}

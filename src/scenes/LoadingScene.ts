// ============================================================================
// Tela de carregamento (GDD seção 11): key art (gradiente + castelo voador em
// silhueta), logo ARKANA, barra dourada fina + %, dica rotativa no rodapé.
// Gera as texturas procedurais (barra de progresso REAL) e inicializa o áudio.
// Dono: COORDENADOR.
// ============================================================================
import Phaser from 'phaser';
import { GAME_WIDTH, GAME_HEIGHT, SCENE, FONTS, GAME_TITLE } from '../core/config';
import { t, tips } from '../core/strings';
import { generateAllTextures } from '../core/textures';
import { Audio } from '../core/audio';

const MIN_DURATION_MS = 2600;

export class LoadingScene extends Phaser.Scene {
  private progress = 0;
  private shown = 0;
  private bar!: Phaser.GameObjects.Rectangle;
  private pct!: Phaser.GameObjects.Text;
  private startedAt = 0;

  constructor() { super(SCENE.LOADING); }

  create(): void {
    const cx = GAME_WIDTH / 2;
    this.startedAt = this.time.now;

    // ---- key art: céu ao entardecer (bandas de gradiente) + castelo voador
    const bands = 32;
    const top = Phaser.Display.Color.ValueToColor(0x0b1026);
    const bottom = Phaser.Display.Color.ValueToColor(0x7a3a4e);
    for (let i = 0; i < bands; i++) {
      const c = Phaser.Display.Color.Interpolate.ColorWithColor(top, bottom, bands - 1, i);
      this.add.rectangle(
        cx, (i + 0.5) * (GAME_HEIGHT / bands), GAME_WIDTH, GAME_HEIGHT / bands + 1,
        Phaser.Display.Color.GetColor(c.r, c.g, c.b),
      );
    }
    this.drawCastle(cx, GAME_HEIGHT * 0.36);

    // nuvens
    for (let i = 0; i < 6; i++) {
      const y = GAME_HEIGHT * (0.55 + 0.06 * i);
      const w = 240 + (i % 3) * 140;
      const cloud = this.add.ellipse(((i * 271) % GAME_WIDTH), y, w, 36, 0xffffff, 0.06 + 0.02 * (i % 2));
      this.tweens.add({ targets: cloud, x: cloud.x + 60, duration: 6000 + i * 900, yoyo: true, repeat: -1, ease: 'Sine.easeInOut' });
    }

    // ---- logo
    this.add.text(cx, GAME_HEIGHT * 0.56, GAME_TITLE, {
      fontFamily: FONTS.title, fontSize: '96px', color: '#f0c75e', fontStyle: 'bold', letterSpacing: 18,
    }).setOrigin(0.5).setShadow(0, 4, '#000000', 12);

    // ---- barra fina dourada + %
    const barW = GAME_WIDTH * 0.42, barY = GAME_HEIGHT * 0.70;
    this.add.rectangle(cx, barY, barW, 4, 0xffffff, 0.12);
    this.bar = this.add.rectangle(cx - barW / 2, barY, 1, 4, 0xf0c75e).setOrigin(0, 0.5);
    this.pct = this.add.text(cx, barY + 22, '0%', {
      fontFamily: FONTS.ui, fontSize: '16px', color: '#e8e6f0',
    }).setOrigin(0.5);
    this.add.text(cx, barY - 22, t('loading.label'), {
      fontFamily: FONTS.ui, fontSize: '14px', color: '#9a97ad', letterSpacing: 6,
    }).setOrigin(0.5);

    // ---- dica rotativa
    const tipList = Phaser.Utils.Array.Shuffle(tips().slice());
    let tipIdx = 0;
    const tip = this.add.text(cx, GAME_HEIGHT - 46, tipList[0], {
      fontFamily: FONTS.body, fontSize: '17px', color: '#c9c6da', align: 'center',
      wordWrap: { width: GAME_WIDTH * 0.8 },
    }).setOrigin(0.5);
    this.time.addEvent({
      delay: 2100, loop: true, callback: () => {
        tipIdx = (tipIdx + 1) % tipList.length;
        this.tweens.add({
          targets: tip, alpha: 0, duration: 250, yoyo: true,
          onYoyo: () => tip.setText(tipList[tipIdx]),
        });
      },
    });

    // ---- trabalho real: texturas + áudio (70% do peso visual da barra)
    Audio.init();
    generateAllTextures(this, (p) => { this.progress = p * 0.7; });
    this.progress = 0.7;
  }

  private drawCastle(cx: number, cy: number): void {
    const g = this.add.graphics();
    const c = 0x121230;
    // ilha flutuante
    g.fillStyle(c, 1);
    g.fillTriangle(cx - 150, cy + 30, cx + 150, cy + 30, cx, cy + 110);
    g.fillRect(cx - 150, cy + 6, 300, 26);
    // torres
    const tower = (x: number, w: number, h: number) => {
      g.fillRect(x - w / 2, cy + 8 - h, w, h);
      g.fillTriangle(x - w / 2 - 4, cy + 8 - h, x + w / 2 + 4, cy + 8 - h, x, cy - h - w * 0.9);
    };
    tower(cx - 95, 34, 60);
    tower(cx + 95, 34, 60);
    tower(cx - 40, 26, 92);
    tower(cx + 40, 26, 92);
    tower(cx, 40, 130);
    // janelas douradas
    g.fillStyle(0xf0c75e, 0.85);
    [[-95, 40], [95, 40], [-40, 70], [40, 70], [0, 100], [0, 60]].forEach(([dx, dy]) => {
      g.fillRect(cx + dx - 2, cy - dy, 4, 7);
    });
  }

  update(): void {
    // barra suavizada + tempo mínimo (respiro pra ler a dica)
    const elapsed = this.time.now - this.startedAt;
    const timeFrac = Math.min(1, elapsed / MIN_DURATION_MS);
    const target = Math.min(this.progress + timeFrac * 0.3, timeFrac);
    this.shown = Phaser.Math.Linear(this.shown, target, 0.08);
    const barW = GAME_WIDTH * 0.42;
    this.bar.width = Math.max(1, barW * this.shown);
    this.pct.setText(`${Math.round(this.shown * 100)}%`);
    if (this.shown > 0.995 && timeFrac >= 1) {
      this.scene.start(SCENE.TITLE);
    }
  }
}
